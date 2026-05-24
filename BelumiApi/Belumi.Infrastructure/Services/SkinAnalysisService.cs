using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Belumi.Core.Interfaces;
using Belumi.Core.DTOs.Gemini;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Belumi.Infrastructure.Services;

public class SkinAnalysisService : ISkinAnalysisService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<SkinAnalysisService> _logger;
    private readonly IConfiguration _configuration;

    public SkinAnalysisService(
        IMemoryCache cache,
        ILogger<SkinAnalysisService> logger,
        IConfiguration configuration)
    {
        _cache         = cache;
        _logger        = logger;
        _configuration = configuration;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<AnalysisResponse> AnalyzeAsync(byte[] imageBytes, string skinType)
    {
        var validationError = ValidateImage(imageBytes);
        if (validationError != null)
        {
            _logger.LogWarning("Image validation failed: {Error}", validationError);
            return new AnalysisResponse { Status = "retake_required", Message = validationError };
        }

        byte[] processedImage;
        try
        {
            processedImage = PreprocessImage(imageBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image preprocessing failed");
            return new AnalysisResponse
            {
                Status  = "error",
                Message = $"Ảnh không hợp lệ hoặc bị lỗi: {ex.Message}"
            };
        }

        var imageHash = ComputeHash(processedImage);
        var cacheKey  = $"skin:{skinType}:{imageHash}";

        if (_cache.TryGetValue(cacheKey, out SkinAnalysisResult? cached) && cached != null)
        {
            _logger.LogInformation("Cache hit for key {CacheKey}", cacheKey);
            return new AnalysisResponse { Status = "success", Result = cached, FromCache = true };
        }

        SkinAnalysisResult? result;
        try
        {
            result = await CallOpenAiAsync(processedImage, skinType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API call failed");
            return new AnalysisResponse { Status = "error", Message = $"Lỗi AI: {ex.Message}" };
        }

        if (result == null)
        {
            _logger.LogWarning("OpenAI returned null result for skinType={SkinType}", skinType);
            return new AnalysisResponse { Status = "error", Message = "AI không trả về kết quả hợp lệ" };
        }

        _logger.LogInformation("OpenAI confidence={Confidence}, score={Score}, acne={Acne}",
            result.Confidence, result.OverallScore, result.AcneLevel);

        if (result.Confidence < 0.65)
        {
            return new AnalysisResponse
            {
                Status  = "retake_required",
                Message = "Ảnh chưa đủ rõ. Vui lòng chụp ở nơi có ánh sáng tốt hơn."
            };
        }

        result.SkinCondition = GenerateSkinCondition(result);
        result.Description   = GenerateDescription(result, skinType);
        result.Advice        = GenerateAdvice(result, skinType);
        result.Warnings      = GenerateWarnings(result, skinType);

        _cache.Set(cacheKey, result, TimeSpan.FromHours(24));

        return new AnalysisResponse { Status = "success", Result = result, FromCache = false };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SKIN CONDITION
    // ─────────────────────────────────────────────────────────────────────────

    private static string GenerateSkinCondition(SkinAnalysisResult result)
    {
        var concernCount = 0;
        if (result.AcneLevel != "none") concernCount++;
        if (result.DarkSpots)           concernCount++;
        if (result.EnlargedPores)       concernCount++;
        if (result.Redness)             concernCount++;
        if (result.UnevenTone)          concernCount++;

        return result.AcneLevel switch
        {
            "severe"   => "critical",
            "moderate" => "needs_care",
            "mild"     => "needs_attention",
            "none"     => concernCount >= 2 ? "needs_attention" : "good",
            _          => "good"
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DESCRIPTION — 2 câu ngắn gọn, tự nhiên
    // ─────────────────────────────────────────────────────────────────────────

    private static string GenerateDescription(SkinAnalysisResult result, string skinType)
    {
        var concerns = new List<string>();
        if (result.AcneLevel != "none") concerns.Add(result.AcneLevel switch
        {
            "mild"     => "mụn nhẹ",
            "moderate" => "mụn mức trung bình",
            "severe"   => "mụn nặng",
            _          => "mụn"
        });
        if (result.DarkSpots)     concerns.Add("thâm");
        if (result.EnlargedPores) concerns.Add("lỗ chân lông to");
        if (result.Redness)       concerns.Add("vùng đỏ");
        if (result.UnevenTone)    concerns.Add("da không đều màu");

        string sentence1;
        if (!concerns.Any())
        {
            sentence1 = result.OverallScore >= 80
                ? "Da bạn đang trong tình trạng tốt, không có dấu hiệu đáng lo ngại."
                : "Da bạn chưa có vấn đề rõ ràng nhưng vẫn có thể cải thiện thêm.";
        }
        else
        {
            var concernText = concerns.Count == 1
                ? concerns[0]
                : string.Join(", ", concerns[..^1]) + " và " + concerns[^1];

            sentence1 = $"Da bạn có {concernText}.";
        }

        var sentence2 = (result.AcneLevel, skinType, result.DarkSpots) switch
        {
            ("severe", _, _) =>
                "Tình trạng khá nghiêm trọng, nên tham khảo bác sĩ da liễu thay vì tự điều trị.",

            ("moderate", "sensitive", _) =>
                "Da nhạy cảm với mụn trung bình dễ để lại sẹo — cần ưu tiên làm dịu trước khi dùng active.",
            ("moderate", _, true) =>
                "Mụn trung bình kèm thâm cần được xử lý sớm để tránh thâm ăn sâu hơn.",
            ("moderate", _, _) =>
                "Tình trạng cần được chăm sóc tích cực hơn, nên bắt đầu routine ổn định sớm.",

            ("mild", _, true) =>
                "Tình trạng chưa nghiêm trọng nhưng cần xử lý sớm để tránh thâm kéo dài.",
            ("mild", "oily", _) =>
                "Mụn nhẹ trên da dầu thường cải thiện tốt nếu kiểm soát được bã nhờn và giữ routine ổn định.",
            ("mild", "dry", _) =>
                "Mụn nhẹ trên da khô thường do barrier yếu — ưu tiên dưỡng ẩm trước khi thêm treatment.",
            ("mild", _, _) =>
                "Tình trạng hiện tại nhẹ, có thể cải thiện rõ với routine đơn giản và kiên trì.",

            ("none", "sensitive", _) when result.Redness =>
                "Đỏ da trên da nhạy cảm cần theo dõi — ưu tiên sản phẩm dịu nhẹ và phục hồi barrier.",
            ("none", "dry", _) when result.DarkSpots =>
                "Da khô kèm thâm cần dưỡng ẩm tốt trước khi dùng active làm sáng để tránh kích ứng.",
            ("none", "oily", _) when result.EnlargedPores =>
                "Lỗ chân lông to trên da dầu thường do bã nhờn tích tụ — BHA định kỳ sẽ giúp cải thiện.",
            ("none", _, _) when concerns.Any() =>
                "Các dấu hiệu hiện tại chưa nghiêm trọng, duy trì routine đều đặn sẽ cải thiện tốt.",

            _ => "Duy trì routine chăm sóc da sáng và tối là đủ để giữ da ở trạng thái tốt."
        };

        var ingredients = new HashSet<string>();

        if (result.AcneLevel != "none")
        {
            ingredients.Add("azelaic acid");
            ingredients.Add("benzoyl peroxide");
            ingredients.Add("glycolic acid");
            ingredients.Add("retinoids");
            ingredients.Add("salicylic acid");
        }

        if (result.DarkSpots)
        {
            ingredients.Add("azelaic acid");
            ingredients.Add("glycolic acid");
            ingredients.Add("niacinamide");
            ingredients.Add("retinoids");
            ingredients.Add("vitamin C");
        }

        if (skinType == "oily")
        {
            ingredients.Add("benzoyl peroxide");
            ingredients.Add("retinoids");
            ingredients.Add("salicylic acid");
        }

        if (result.Redness)
        {
            ingredients.Add("mineral sunscreen");
            ingredients.Add("niacinamide");
        }

        if (result.EnlargedPores)
        {
            ingredients.Add("retinoids");
        }

        string sentence3 = ingredients.Any()
            ? $" Bạn có thể sử dụng sản phẩm chứa {string.Join(", ", ingredients.OrderBy(x => x))}."
            : "";

        return $"{sentence1} {sentence2}{sentence3}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ADVICE
    // ─────────────────────────────────────────────────────────────────────────

    private static List<string> GenerateAdvice(SkinAnalysisResult result, string skinType)
    {
        var advice = new List<string>();

        advice.Add("Ưu tiên routine đơn giản và ổn định trong ít nhất 4 tuần trước khi đánh giá hiệu quả");
        advice.Add("Dùng kem chống nắng SPF 30+ mỗi sáng, dù ở trong nhà");

        switch (skinType)
        {
            case "oily":
                advice.Add("Không bỏ bước dưỡng ẩm — da thiếu ẩm sẽ tiết dầu nhiều hơn để bù lại");
                advice.Add("Rửa mặt tối đa 2 lần/ngày, thêm 1 lần nếu vừa vận động ra nhiều mồ hôi");
                break;

            case "dry":
                advice.Add("Thoa moisturizer ngay khi da còn hơi ẩm sau rửa mặt để giữ nước tốt hơn");
                advice.Add("Chỉ dùng sản phẩm ghi rõ \"fragrance-free\", không phải \"unscented\"");
                break;

            case "combination":
                advice.Add("Có thể dùng cleanser nhẹ hơn ở vùng má và BHA nhẹ ở vùng T riêng biệt");
                advice.Add("Dưỡng ẩm tập trung vùng má, tránh thoa nhiều lên vùng trán và mũi");
                break;

            case "sensitive":
                advice.Add("Patch test sản phẩm mới ở cổ tay hoặc sau tai trước khi thoa lên mặt");
                advice.Add("Chỉ thêm tối đa 1 sản phẩm mới vào routine tại một thời điểm");
                break;

            case "normal":
                // Da thường cân bằng tự nhiên, không cần lời khuyên đặc thù theo loại da
                break;
        }

        if (result.AcneLevel != "none")
            advice.Add("Thoa acne treatment lên cả vùng hay nổi mụn, không chỉ spot treatment lên từng nốt");

        if (result.DarkSpots)
            advice.Add("Dùng SPF 50+ khi ra ngoài — thiếu SPF làm thâm đậm hơn và kéo dài thời gian trị");

        if (result.Redness)
            advice.Add("Rửa mặt bằng nước ấm (không nóng) để tránh kích ứng thêm vùng đỏ");

        if (result.EnlargedPores)
            advice.Add("Làm sạch da kỹ vào buổi tối quan trọng hơn buổi sáng vì bã nhờn tích tụ suốt ngày");

        return advice;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WARNINGS
    // ─────────────────────────────────────────────────────────────────────────

    private static List<string> GenerateWarnings(SkinAnalysisResult result, string skinType)
    {
        var warnings = new List<string>();

        if (result.AcneLevel is "mild" or "moderate")
        {
            warnings.Add("Không tự nặn mụn viêm — có thể đẩy vi khuẩn sâu hơn và gây thâm, sẹo");
            warnings.Add("Không đổi sản phẩm liên tục — cần ít nhất 4–6 tuần mới thấy hiệu quả rõ");
        }
        else if (result.AcneLevel == "severe")
        {
            warnings.Add("Không tự nặn hoặc can thiệp vào mụn viêm nặng — nguy cơ sẹo rất cao");
            warnings.Add("Không tự dùng retinoid hoặc acid nồng độ cao khi chưa có chỉ định của bác sĩ");
        }

        switch (skinType)
        {
            case "oily":
                warnings.Add("Không rửa mặt quá 2–3 lần/ngày — rửa nhiều gây kích ứng và da tiết dầu ngược");
                warnings.Add("Tránh sản phẩm chứa cồn nồng độ cao (alcohol denat.) — làm khô da và kích bùng dầu");
                break;

            case "dry":
                warnings.Add("Tránh sản phẩm chứa fragrance, cồn và AHA nồng độ cao — phá vỡ barrier da khô");
                warnings.Add("Không dùng nước nóng khi rửa mặt — làm mất dầu tự nhiên và tăng độ khô");
                break;

            case "combination":
                warnings.Add("Không dùng sản phẩm kiểm soát dầu mạnh lên vùng má — gây khô và bong tróc");
                break;

            case "sensitive":
                warnings.Add("Tránh layer nhiều active (AHA, BHA, Retinol, Vitamin C) cùng một buổi");
                warnings.Add("Không dùng sản phẩm có fragrance hoặc essential oil — hai thành phần kích ứng phổ biến nhất");
                break;

            case "normal":
                // Da thường khỏe mạnh, không cần cảnh báo đặc thù theo loại da
                break;
        }

        if (result.DarkSpots)
            warnings.Add("Không bỏ SPF khi đang dùng Vitamin C hoặc AHA — ánh nắng làm thâm tối và lâu mờ hơn");

        if (result.Redness && skinType == "sensitive")
            warnings.Add("Đỏ da không cải thiện sau 2–3 tuần nên được kiểm tra bởi bác sĩ da liễu");

        if (result.EnlargedPores && result.AcneLevel != "none")
            warnings.Add("Không dùng BHA và Retinol cùng một buổi tối — gây kích ứng và làm mỏng barrier");

        return warnings;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IMAGE UTILS
    // ─────────────────────────────────────────────────────────────────────────

    private string? ValidateImage(byte[] imageBytes)
    {
        if (imageBytes.Length > 5 * 1024 * 1024)
            return "Ảnh quá lớn. Vui lòng chọn ảnh nhỏ hơn 5MB.";

        if (!IsJpegOrPng(imageBytes))
            return "Định dạng ảnh không hợp lệ. Chỉ hỗ trợ JPEG và PNG.";

        return null;
    }

    private static bool IsJpegOrPng(byte[] bytes)
    {
        if (bytes.Length < 4) return false;

        bool isJpeg = bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        bool isPng  = bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;

        return isJpeg || isPng;
    }

    private static byte[] PreprocessImage(byte[] imageBytes)
    {
        using var input = new MemoryStream(imageBytes);
        using var image = Image.Load(input);

        image.Mutate(ctx => ctx
            .Resize(new ResizeOptions
            {
                Size     = new Size(512, 512),
                Mode     = ResizeMode.Pad,
                Position = AnchorPositionMode.Center
            })
            .Brightness(1.05f)
            .Contrast(1.05f)
        );

        using var output = new MemoryStream();
        image.Save(output, new JpegEncoder { Quality = 85 });
        return output.ToArray();
    }

    private static string ComputeHash(byte[] imageBytes)
    {
        var hash = SHA256.HashData(imageBytes);
        return Convert.ToHexString(hash).ToLower();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OPENAI CHAT CLIENT
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<SkinAnalysisResult?> CallOpenAiAsync(byte[] imageBytes, string skinType)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API Key is not configured.");
        }

        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        
        ChatClient client = new(model, apiKey);
        
        var prompt = BuildPrompt(skinType);
        var messages = new ChatMessage[]
        {
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(prompt),
                ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), "image/jpeg")
            )
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "skin_analysis",
                BinaryData.FromString(GetJsonSchemaForSkinAnalysisResult()),
                "Face skin analysis result schema",
                true
            ),
            Temperature = 0.1f
        };

        ChatCompletion completion = await client.CompleteChatAsync(messages, options);
        
        if (completion.Content == null || completion.Content.Count == 0)
        {
            _logger.LogWarning("OpenAI returned empty response");
            return null;
        }

        var resultText = completion.Content[0].Text;
        if (string.IsNullOrWhiteSpace(resultText))
        {
            _logger.LogWarning("OpenAI returned empty text in response");
            return null;
        }

        return JsonSerializer.Deserialize<SkinAnalysisResult>(resultText);
    }

    private static string GetJsonSchemaForSkinAnalysisResult() => """
    {
      "type": "object",
      "properties": {
        "acne_level": { "type": "string", "enum": ["none", "mild", "moderate", "severe"] },
        "dark_spots": { "type": "boolean" },
        "enlarged_pores": { "type": "boolean" },
        "redness": { "type": "boolean" },
        "uneven_tone": { "type": "boolean" },
        "top_concerns": {
          "type": "array",
          "items": { "type": "string", "enum": ["acne", "dark_spots", "pores", "dryness", "oiliness", "dullness", "redness"] }
        },
        "overall_score": { "type": "integer" },
        "confidence": { "type": "number" }
      },
      "required": ["acne_level", "dark_spots", "enlarged_pores", "redness", "uneven_tone", "top_concerns", "overall_score", "confidence"],
      "additionalProperties": false
    }
    """;

    private static string BuildPrompt(string skinType) => $"""
        You are a professional skincare analysis AI.

        The user has already been identified as having {skinType} skin.
        Do NOT re-classify skin type — it is already known.

        Your task: Analyze the facial image and identify VISIBLE skin concerns only.

        Look for:
        - Acne or pimples (none/mild/moderate/severe)
        - Dark spots or hyperpigmentation (yes/no)
        - Enlarged or visible pores (yes/no)
        - Redness or inflammation (yes/no)
        - Uneven skin tone or dullness (yes/no)

        overall_score: Rate overall skin health from 0 (severe issues) to 100 (perfect skin).
        confidence: How confident you are in this analysis based on image quality (0.0 to 1.0).

        Respond ONLY with valid JSON matching the schema. No explanations, no markdown.
        """;
}
