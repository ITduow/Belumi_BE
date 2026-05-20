using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Core.Interfaces;
using Belumi.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace Belumi.Infrastructure.Services;

public sealed class SkinAnalysisService(BelumiDbContext db, HttpClient httpClient, IConfiguration configuration) : ISkinAnalysisService
{
    public async Task<SkinAnalysis> AnalyzeAsync(Guid userId, SkinAnalysisRequest request, CancellationToken cancellationToken)
    {
        var skinType = Normalize(request.SkinType, "Combination");
        var concerns = request.Concerns?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? [];
        var goal = Normalize(request.Goal, "Build a simple, healthy barrier-first skincare routine.");
        var plan = Normalize(request.PlanCode, "free").ToLowerInvariant();
        var imageUrl = request.ImageUrl?.Trim() ?? string.Empty;
        var imageData = TryParseDataUrl(imageUrl);
        var canUseImage = plan == "pro" && (!string.IsNullOrWhiteSpace(imageUrl) || imageData is not null);
        var detailLevel = plan switch
        {
            "pro" => "Pro image-aware Gemini consultation",
            "plus" => "Plus detailed Gemini consultation",
            _ => "Free basic consultation"
        };

        var concernText = concerns.Length == 0 ? "general dullness and routine consistency" : string.Join(", ", concerns);
        var score = Math.Clamp(72 + skinType.Length + concerns.Length * 2 + (canUseImage ? 5 : 0), 70, 96);
        var analysisSummary = BuildAnalysisSummary(skinType, concerns, goal, canUseImage, detailLevel);
        var fallbackRecommendations = BuildFallbackRecommendations(plan, skinType, concerns, goal, analysisSummary);
        var recommendations = await TryGenerateGeminiRecommendationsAsync(
            plan,
            skinType,
            concerns,
            goal,
            imageUrl,
            imageData,
            canUseImage,
            fallbackRecommendations,
            cancellationToken);

        var analysis = new SkinAnalysis
        {
            UserId = userId,
            ImageUrl = imageData is null ? imageUrl : $"uploaded:{imageData.MimeType};bytes={imageData.Base64Data.Length}",
            SkinType = skinType,
            Concerns = concernText,
            AgeRange = request.Goal?.Contains("18") == true ? request.Goal : null,
            SensitivityLevel = concerns.FirstOrDefault(x => x.Contains("sensitive", StringComparison.OrdinalIgnoreCase) || x.Contains("nhay", StringComparison.OrdinalIgnoreCase)),
            UserNote = goal,
            AiResult = recommendations,
            MorningRoutine = ExtractSection(recommendations, "Morning routine") ?? BuildMorningRoutine(plan, skinType),
            NightRoutine = ExtractSection(recommendations, "Evening routine") ?? BuildEveningRoutine(plan, skinType, concerns),
            RecommendedIngredients = ExtractSection(recommendations, "Ingredients to use") ?? BuildUseIngredients(skinType, concerns),
            AvoidIngredients = ExtractSection(recommendations, "Ingredients to avoid") ?? BuildAvoidIngredients(skinType, concerns),
            Recommendations = recommendations,
            Score = score
        };

        db.SkinAnalyses.Add(analysis);
        db.AiUsageLogs.Add(new AiUsageLog
        {
            UserId = userId,
            FeatureName = "skincare",
            TokenUsed = Math.Max(1, recommendations.Length / 4),
            RequestData = JsonSerializer.Serialize(request),
            ResponseData = recommendations
        });
        await db.SaveChangesAsync(cancellationToken);
        return analysis;
    }

    private static string? ExtractSection(string value, string section)
    {
        var prefix = section + ":";
        var part = value.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return part is null ? null : part[prefix.Length..].Trim();
    }

    private async Task<string> TryGenerateGeminiRecommendationsAsync(
        string plan,
        string skinType,
        IReadOnlyCollection<string> concerns,
        string goal,
        string imageUrl,
        GeminiImageData? imageData,
        bool canUseImage,
        string fallback,
        CancellationToken cancellationToken)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return fallback;
        }

        try
        {
            var model = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            var endpointTemplate = configuration["Gemini:Endpoint"]
                ?? "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
            var endpoint = endpointTemplate.Replace("{model}", Uri.EscapeDataString(model), StringComparison.Ordinal);

            using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
            message.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
            var parts = new List<object>
            {
                new
                {
                    text = BuildGeminiPrompt(plan, skinType, concerns, goal, imageUrl, imageData is not null, canUseImage)
                }
            };
            if (canUseImage && imageData is not null)
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = imageData.MimeType,
                        data = imageData.Base64Data
                    }
                });
            }

            message.Content = JsonContent.Create(new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts
                    }
                },
                generationConfig = new
                {
                    temperature = 0.35,
                    topP = 0.9,
                    maxOutputTokens = plan == "free" ? 700 : 1400
                }
            });

            using var response = await httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return fallback;
            }

            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var text = ExtractGeminiText(payload);
            return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        }
        catch
        {
            return fallback;
        }
    }

    private static string BuildGeminiPrompt(
        string plan,
        string skinType,
        IReadOnlyCollection<string> concerns,
        string goal,
        string imageUrl,
        bool hasInlineImage,
        bool canUseImage)
    {
        var detailInstruction = plan switch
        {
            "pro" => "Give a detailed, professional but safe skincare consultation. If image context is present, mention visible-signal limitations and do not diagnose disease.",
            "plus" => "Give a detailed skincare consultation with clear routines and ingredient logic.",
            _ => "Give a concise basic skincare consultation."
        };
        var imageInstruction = hasInlineImage
            ? "The user uploaded a face skin image. The image is attached as inline_data. Use visible cues such as shine, redness, texture, dryness and uneven tone, but do not diagnose disease."
            : canUseImage
            ? $"The user supplied this skin image URL for Pro analysis: {imageUrl}. If you cannot fetch URLs, treat it as contextual metadata and rely on the questionnaire."
            : "No image analysis is available for this plan.";

        return $"""
You are Belumi Beauty's skincare AI assistant. Respond in Vietnamese without accents for app compatibility.
{detailInstruction}
Skin type: {skinType}
Concerns: {(concerns.Count == 0 ? "general routine support" : string.Join(", ", concerns))}
Goal: {goal}
Plan: {plan}
Image context: {imageInstruction}

Return exactly these sections and keep each section practical:
Analysis:
Morning routine:
Evening routine:
Ingredients to use:
Ingredients to avoid:
Product suggestions:
""";
    }

    private static string ExtractGeminiText(JsonElement payload)
    {
        if (!payload.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts))
        {
            return string.Empty;
        }

        var chunks = new List<string>();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text))
            {
                chunks.Add(text.GetString() ?? string.Empty);
            }
        }

        return string.Join("\n", chunks);
    }

    private static GeminiImageData? TryParseDataUrl(string value)
    {
        const string marker = ";base64,";
        if (!value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var mimeType = value[5..markerIndex];
        var base64Data = value[(markerIndex + marker.Length)..];
        if (string.IsNullOrWhiteSpace(mimeType) || string.IsNullOrWhiteSpace(base64Data))
        {
            return null;
        }

        return new GeminiImageData(mimeType, base64Data);
    }

    private static string Normalize(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string BuildAnalysisSummary(string skinType, IReadOnlyCollection<string> concerns, string goal, bool canUseImage, string detailLevel)
    {
        var imageNote = canUseImage
            ? "Face image signal is included for visible texture, redness, shine and uneven tone cues."
            : "Image analysis is locked to Pro, so this result is based on the questionnaire.";
        var concernText = concerns.Count == 0 ? "no severe concern selected" : string.Join(", ", concerns);
        return $"{detailLevel}: {skinType} skin with {concernText}. Goal: {goal}. {imageNote}";
    }

    private static string BuildFallbackRecommendations(string plan, string skinType, IReadOnlyCollection<string> concerns, string goal, string analysisSummary) =>
        string.Join("\n\n", new[]
        {
            $"Analysis: {analysisSummary}",
            $"Morning routine: {BuildMorningRoutine(plan, skinType)}",
            $"Evening routine: {BuildEveningRoutine(plan, skinType, concerns)}",
            $"Ingredients to use: {BuildUseIngredients(skinType, concerns)}",
            $"Ingredients to avoid: {BuildAvoidIngredients(skinType, concerns)}",
            $"Product suggestions: {BuildProductSuggestions(plan, skinType, concerns)}"
        });

    private static string BuildMorningRoutine(string plan, string skinType)
    {
        var moisturizer = skinType.Contains("dry", StringComparison.OrdinalIgnoreCase) || skinType.Contains("kho", StringComparison.OrdinalIgnoreCase)
            ? "ceramide cream"
            : "light gel moisturizer";
        var baseRoutine = $"Gentle cleanser -> hydrating toner -> vitamin C or niacinamide -> {moisturizer} -> broad-spectrum SPF 50.";
        return plan == "free" ? baseRoutine : $"{baseRoutine} Reapply sunscreen every 2-3 hours when outdoors.";
    }

    private static string BuildEveningRoutine(string plan, string skinType, IReadOnlyCollection<string> concerns)
    {
        var active = concerns.Any(x => x.Contains("mun", StringComparison.OrdinalIgnoreCase) || x.Contains("acne", StringComparison.OrdinalIgnoreCase))
            ? "BHA 1-2 times/week on acne-prone areas"
            : "low-strength retinol 1-2 nights/week";
        if (skinType.Contains("sensitive", StringComparison.OrdinalIgnoreCase) || skinType.Contains("nhay", StringComparison.OrdinalIgnoreCase))
        {
            active = "azelaic acid or panthenol serum, introduced slowly";
        }

        var routine = $"Cleanser -> {active} -> barrier serum -> moisturizer.";
        return plan == "free" ? routine : $"{routine} Keep one recovery night between active nights.";
    }

    private static string BuildUseIngredients(string skinType, IReadOnlyCollection<string> concerns)
    {
        var ingredients = new List<string> { "Niacinamide", "Hyaluronic Acid", "Ceramide", "Panthenol" };
        if (concerns.Any(x => x.Contains("mun", StringComparison.OrdinalIgnoreCase) || x.Contains("acne", StringComparison.OrdinalIgnoreCase)))
        {
            ingredients.Add("Salicylic Acid");
        }
        if (concerns.Any(x => x.Contains("tham", StringComparison.OrdinalIgnoreCase) || x.Contains("nam", StringComparison.OrdinalIgnoreCase) || x.Contains("spot", StringComparison.OrdinalIgnoreCase)))
        {
            ingredients.Add("Vitamin C");
            ingredients.Add("Tranexamic Acid");
        }
        if (skinType.Contains("dry", StringComparison.OrdinalIgnoreCase) || skinType.Contains("kho", StringComparison.OrdinalIgnoreCase))
        {
            ingredients.Add("Squalane");
        }

        return string.Join(", ", ingredients.Distinct());
    }

    private static string BuildAvoidIngredients(string skinType, IReadOnlyCollection<string> concerns)
    {
        var avoid = new List<string> { "Harsh physical scrubs", "Over-layering acids", "Unprotected daytime retinoids" };
        if (skinType.Contains("sensitive", StringComparison.OrdinalIgnoreCase) || skinType.Contains("nhay", StringComparison.OrdinalIgnoreCase))
        {
            avoid.Add("High fragrance formulas");
            avoid.Add("Alcohol denat-heavy toners");
        }
        if (concerns.Any(x => x.Contains("kho", StringComparison.OrdinalIgnoreCase)))
        {
            avoid.Add("Foaming cleansers that leave skin tight");
        }

        return string.Join(", ", avoid.Distinct());
    }

    private static string BuildProductSuggestions(string plan, string skinType, IReadOnlyCollection<string> concerns)
    {
        var suggestions = new List<string> { "Belumi Barrier Cream", "Belumi Daily SPF 50" };
        suggestions.Add(skinType.Contains("oily", StringComparison.OrdinalIgnoreCase) || skinType.Contains("dau", StringComparison.OrdinalIgnoreCase)
            ? "Belumi Clarifying Gel Cleanser"
            : "Belumi Gentle Milk Cleanser");
        if (concerns.Any(x => x.Contains("tham", StringComparison.OrdinalIgnoreCase) || x.Contains("nam", StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add("Belumi Glow Serum");
        }
        if (plan == "pro")
        {
            suggestions.Add("Belumi AI Custom Routine Set");
        }

        return string.Join(", ", suggestions.Distinct());
    }

    private sealed record GeminiImageData(string MimeType, string Base64Data);
}
