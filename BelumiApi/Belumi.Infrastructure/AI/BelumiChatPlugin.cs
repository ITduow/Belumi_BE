using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Belumi.Infrastructure.AI;

public sealed class BelumiChatPlugin(
    BelumiDbContext db,
    IInciApiClient inciApiClient,
    IOptions<InciApiOptions> inciOptions,
    ChatbotRequestContext requestContext,
    ChatToolCallTracker tracker,
    ILogger<BelumiChatPlugin> logger)
{
    [KernelFunction("search_local_ingredient")]
    [Description("Search Belumi's local ingredient database by INCI name, common name, or query text.")]
    public async Task<string> SearchLocalIngredientAsync(
        [Description("Ingredient name, INCI name, or user query text.")] string query,
        CancellationToken cancellationToken = default)
    {
        tracker.AddTool("Belumi.search_local_ingredient");

        if (string.IsNullOrWhiteSpace(query))
        {
            return "[]";
        }

        var normalizedQuery = Normalize(query);
        var ingredients = await db.Ingredients.AsNoTracking()
            .Where(x =>
                normalizedQuery.Contains(x.NameInc.ToLower()) ||
                normalizedQuery.Contains(x.Name.ToLower()) ||
                x.NameInc.ToLower().Contains(normalizedQuery) ||
                x.Name.ToLower().Contains(normalizedQuery))
            .OrderByDescending(x => x.NameInc.ToLower() == normalizedQuery || x.Name.ToLower() == normalizedQuery)
            .ThenBy(x => x.NameInc)
            .Take(8)
            .Select(x => new
            {
                x.Id,
                inciName = x.NameInc,
                name = x.Name,
                x.Category,
                x.Description,
                url = x.Links
            })
            .ToListAsync(cancellationToken);

        foreach (var ingredient in ingredients)
        {
            tracker.AddSource("local_ingredient", ingredient.inciName, ingredient.url);
        }

        return JsonSerializer.Serialize(ingredients);
    }

    [KernelFunction("get_latest_skin_analysis")]
    [Description("Get the latest saved skin analysis for the currently logged-in user. Never exposes another user's data.")]
    public async Task<string> GetLatestSkinAnalysisAsync(CancellationToken cancellationToken = default)
    {
        tracker.AddTool("Belumi.get_latest_skin_analysis");

        if (requestContext.UserId == Guid.Empty)
        {
            return """{"status":"not_logged_in","message":"No authenticated user is available for skin analysis lookup."}""";
        }

        var latestSkin = await db.SkinAnalyses.AsNoTracking()
            .Where(x => x.UserId == requestContext.UserId)
            .OrderByDescending(x => x.AnalyzedAt)
            .Select(x => new
            {
                x.SkinType,
                x.Concerns,
                x.SensitivityLevel,
                x.Score,
                x.Recommendations,
                x.RecommendedIngredients,
                x.AvoidIngredients,
                x.AnalyzedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSkin is null)
        {
            return """{"status":"not_found","message":"This user does not have a saved skin analysis yet."}""";
        }

        tracker.AddSource("skin_analysis", "Latest skin analysis");
        return JsonSerializer.Serialize(latestSkin);
    }

    [KernelFunction("get_inci_ingredient")]
    [Description("Get ingredient details from INCI API when local data is missing or the user needs broader ingredient knowledge.")]
    public Task<string> GetInciIngredientAsync(
        [Description("Single INCI ingredient name, for example Niacinamide.")] string inciName,
        CancellationToken cancellationToken = default) =>
        GetCachedInciAsync(
            "ingredient",
            Normalize(inciName),
            inciName,
            "Belumi.get_inci_ingredient",
            $"GET /v1/ingredients/{inciName}",
            () => inciApiClient.GetIngredientAsync(inciName, cancellationToken),
            cancellationToken);

    [KernelFunction("get_inci_skin_type_profiles")]
    [Description("Get INCI API skin type suitability profiles for one ingredient, such as acne-prone, sensitive, oily, or dry skin.")]
    public Task<string> GetInciSkinTypeProfilesAsync(
        [Description("Single INCI ingredient name, for example Niacinamide.")] string inciName,
        CancellationToken cancellationToken = default) =>
        GetCachedInciAsync(
            "skin-type-profiles",
            Normalize(inciName),
            inciName,
            "Belumi.get_inci_skin_type_profiles",
            $"GET /v1/ingredients/{inciName}/skin-type-profiles",
            () => inciApiClient.GetIngredientSkinTypeProfilesAsync(inciName, cancellationToken),
            cancellationToken);

    [KernelFunction("get_inci_efficacy")]
    [Description("Get INCI API efficacy or evidence information for one ingredient.")]
    public Task<string> GetInciEfficacyAsync(
        [Description("Single INCI ingredient name, for example Niacinamide.")] string inciName,
        CancellationToken cancellationToken = default) =>
        GetCachedInciAsync(
            "efficacy",
            Normalize(inciName),
            inciName,
            "Belumi.get_inci_efficacy",
            $"GET /v1/ingredients/{inciName}/efficacy",
            () => inciApiClient.GetIngredientEfficacyAsync(inciName, cancellationToken),
            cancellationToken);

    [KernelFunction("get_inci_incompatibilities")]
    [Description("Get INCI API incompatibilities and mixing guidance for one ingredient.")]
    public Task<string> GetInciIncompatibilitiesAsync(
        [Description("Single INCI ingredient name, for example Retinol.")] string inciName,
        CancellationToken cancellationToken = default) =>
        GetCachedInciAsync(
            "incompatibilities",
            Normalize(inciName),
            inciName,
            "Belumi.get_inci_incompatibilities",
            $"GET /v1/ingredients/{inciName}/incompatibilities",
            () => inciApiClient.GetIngredientIncompatibilitiesAsync(inciName, cancellationToken),
            cancellationToken);

    [KernelFunction("analyze_inci_list")]
    [Description("Analyze a comma-separated INCI ingredient list through INCI API.")]
    public Task<string> AnalyzeInciListAsync(
        [Description("Comma-separated ingredient list copied from a cosmetic label.")] string inciList,
        CancellationToken cancellationToken = default)
    {
        var ingredients = inciList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length is > 1 and < 80)
            .Take(80)
            .ToArray();

        return GetCachedInciAsync(
            "analyze",
            HashKey(Normalize(string.Join(",", ingredients))),
            inciList,
            "Belumi.analyze_inci_list",
            "POST /v1/analyze",
            () => inciApiClient.AnalyzeIngredientsAsync(ingredients, cancellationToken),
            cancellationToken);
    }

    private async Task<string> GetCachedInciAsync(
        string endpoint,
        string normalizedName,
        string query,
        string toolName,
        string sourceLabel,
        Func<Task<string?>> fetch,
        CancellationToken cancellationToken)
    {
        tracker.AddTool(toolName);
        tracker.AddSource("inciapi", sourceLabel);

        if (string.IsNullOrWhiteSpace(query))
        {
            return """{"status":"invalid_request","message":"Ingredient query is required."}""";
        }

        var now = DateTime.UtcNow;
        var cached = await db.IngredientExternalCaches
            .FirstOrDefaultAsync(x =>
                x.Source == "inciapi" &&
                x.Endpoint == endpoint &&
                x.NormalizedName == normalizedName &&
                x.ExpiresAt > now,
                cancellationToken);

        if (cached is not null)
        {
            return cached.RawJson;
        }

        var rawJson = await fetch();
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return """{"status":"not_found","message":"INCI API returned no data for this query."}""";
        }

        var stale = await db.IngredientExternalCaches
            .FirstOrDefaultAsync(x =>
                x.Source == "inciapi" &&
                x.Endpoint == endpoint &&
                x.NormalizedName == normalizedName,
                cancellationToken);

        if (stale is null)
        {
            db.IngredientExternalCaches.Add(new IngredientExternalCache
            {
                Query = query,
                NormalizedName = normalizedName,
                Source = "inciapi",
                Endpoint = endpoint,
                RawJson = rawJson,
                LastFetchedAt = now,
                ExpiresAt = now.AddDays(Math.Max(1, inciOptions.Value.CacheDays))
            });
        }
        else
        {
            stale.Query = query;
            stale.RawJson = rawJson;
            stale.LastFetchedAt = now;
            stale.ExpiresAt = now.AddDays(Math.Max(1, inciOptions.Value.CacheDays));
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Could not save INCI API cache for {Endpoint}:{Name}", endpoint, normalizedName);
        }

        return rawJson;
    }

    private static string Normalize(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).Replace("đ", "d");
    }

    private static string HashKey(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
