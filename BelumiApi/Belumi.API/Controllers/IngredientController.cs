using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Belumi.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/ingredients")]
public sealed class IngredientController(IAiBeautyService aiBeautyService, BelumiDbContext db, CompatibilityEngine compatibilityEngine) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IngredientListResult>> Get(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Ingredients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.NameInc.ToLower().Contains(term) ||
                x.Name.ToLower().Contains(term) ||
                x.Category.ToLower().Contains(term) ||
                x.Description.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryTerm = category.Trim().ToLower();
            query = query.Where(x => x.Category.ToLower().Contains(categoryTerm));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.NameInc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return Ok(new IngredientListResult(items, total, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var ingredient = await db.Ingredients.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (ingredient is null) return NotFound();

        // General info: goodFor / avoidFor (always available, not personalized)
        var generalInfo = compatibilityEngine.GetGeneralInfo(ingredient.NameInc);

        // Personalized assessment (only when user is authenticated + has skin profile)
        PersonalizedAssessmentData? assessment = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.GetUserId();
            var profile = await compatibilityEngine.GetSkinProfileAsync(userId, cancellationToken);
            if (profile is not null)
            {
                var result = compatibilityEngine.EvaluateSingle(ingredient.NameInc, profile);
                assessment = new PersonalizedAssessmentData(result.Status, profile.IsStale, result.Reasons);
            }
        }

        return Ok(new EnhancedIngredientDto(
            ingredient.Id,
            ingredient.NameInc,
            ingredient.Name,
            ingredient.Category,
            ingredient.Description,
            ingredient.Links,
            ingredient.CreatedAt,
            ingredient.UpdatedAt,
            generalInfo?.GoodFor,
            generalInfo?.AvoidFor,
            assessment
        ));
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<IngredientDto>> Create(IngredientCreateRequest request, CancellationToken cancellationToken)
    {
        var nameInc = request.NameInc.Trim();
        if (await db.Ingredients.AnyAsync(x => x.NameInc.ToLower() == nameInc.ToLower(), cancellationToken))
        {
            return Conflict(new { message = "An ingredient with this INCI name already exists." });
        }

        var ingredient = new Ingredient
        {
            NameInc = nameInc,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            Description = request.Description.Trim(),
            Links = request.Links.Trim()
        };

        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync(cancellationToken);

        return Created($"/api/ingredients/{ingredient.Id}", ToDto(ingredient));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<IngredientDto>> Update(Guid id, IngredientUpdateRequest request, CancellationToken cancellationToken)
    {
        var ingredient = await db.Ingredients.FindAsync([id], cancellationToken);
        if (ingredient is null)
        {
            return NotFound();
        }

        var nameInc = request.NameInc.Trim();
        if (await db.Ingredients.AnyAsync(x => x.Id != id && x.NameInc.ToLower() == nameInc.ToLower(), cancellationToken))
        {
            return Conflict(new { message = "An ingredient with this INCI name already exists." });
        }

        ingredient.NameInc = nameInc;
        ingredient.Name = request.Name.Trim();
        ingredient.Category = request.Category.Trim();
        ingredient.Description = request.Description.Trim();
        ingredient.Links = request.Links.Trim();

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(ingredient));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ingredient = await db.Ingredients.FindAsync([id], cancellationToken);
        if (ingredient is null)
        {
            return NotFound();
        }

        db.Ingredients.Remove(ingredient);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("lookup")]
    public ActionResult<IngredientLookupResult> Lookup(IngredientLookupRequest request) =>
        Ok(aiBeautyService.LookupIngredients(request));

    /// <summary>
    /// Self-contained Scan endpoint:
    /// 1. AiBeautyService → Safety analysis (general)
    /// 2. CompatibilityEngine → Personalized compatibility (if user is authenticated + has skin profile)
    /// No coupling with AI Layer beyond safety scoring.
    /// </summary>
    [HttpPost("scan")]
    public async Task<ActionResult<EnhancedIngredientScanResult>> Scan(
        IngredientScanRequest request, CancellationToken cancellationToken)
    {
        // Step 1: Safety analysis (AI Layer — existing logic)
        var safetyResult = aiBeautyService.AnalyzeIngredientLabel(request);

        // Step 2: Compatibility (Decision Layer — Rule Engine via DB)
        CompatibilityData? compatibility = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.GetUserId();
            var profile = await compatibilityEngine.GetSkinProfileAsync(userId, cancellationToken);
            if (profile is not null)
            {
                var ingredientNames = (request.RawTextOrImageUrl ?? "")
                    .Split(['\n', ',', ';'])
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                var compResult = compatibilityEngine.Evaluate(ingredientNames, profile);
                compatibility = new CompatibilityData(
                    compResult.Status,
                    profile.IsStale,
                    compResult.Beneficial.Select(x => new CompatibilityIngredientItem(x.Name, x.Reason, x.PersonalReason)).ToList(),
                    compResult.Harmful.Select(x => new CompatibilityIngredientItem(x.Name, x.Reason, x.PersonalReason)).ToList(),
                    compResult.Neutral.Select(x => new CompatibilityIngredientItem(x.Name, x.Reason, x.PersonalReason)).ToList()
                );
            }
        }

        return Ok(new EnhancedIngredientScanResult(
            safetyResult.SafetyScore,
            safetyResult.Status,
            safetyResult.Summary,
            safetyResult.Beneficial,
            safetyResult.Neutral,
            safetyResult.Harmful,
            safetyResult.Recommendations,
            compatibility
        ));
    }

    [HttpPost("analyze-text")]
    [Authorize]
    public async Task<ActionResult<IngredientScanResult>> AnalyzeText(IngredientAnalyzeTextRequest request, CancellationToken cancellationToken)
    {
        var result = aiBeautyService.AnalyzeIngredientLabel(new IngredientScanRequest(request.InputText, request.SkinType, request.Allergies));
        await SaveLookupAsync(request.UserId == Guid.Empty ? User.GetUserId() : request.UserId, request.InputText, null, result, cancellationToken);
        return Ok(result);
    }

    [HttpPost("analyze-image")]
    [Authorize]
    public async Task<ActionResult<IngredientScanResult>> AnalyzeImage(IngredientAnalyzeImageRequest request, CancellationToken cancellationToken)
    {
        var result = aiBeautyService.AnalyzeIngredientLabel(new IngredientScanRequest(request.ImageUrl, request.SkinType, request.Allergies));
        await SaveLookupAsync(request.UserId == Guid.Empty ? User.GetUserId() : request.UserId, request.OcrText ?? request.ImageUrl, request.ImageUrl, result, cancellationToken);
        return Ok(result);
    }

    [HttpGet("history/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> History(Guid userId, CancellationToken cancellationToken)
    {
        if (userId != User.GetUserId() && !User.IsInRole(nameof(UserRole.Admin)))
        {
            return Forbid();
        }

        return Ok(await db.IngredientLookups.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("evaluation/dashboard")]
    public async Task<ActionResult> GetEvaluationDashboard(CancellationToken cancellationToken)
    {
        // 1. Static Metrics
        var dbIngredients = await db.Ingredients.AsNoTracking().Select(x => x.NameInc).ToListAsync(cancellationToken);
        var dbAnalysis = compatibilityEngine.AnalyzeList(dbIngredients);
        double ruleCoverageRate = dbIngredients.Count == 0 ? 0.0 : (double)dbAnalysis.MatchedIngredients / dbIngredients.Count * 100;

        // 2. Controlled Metrics (Golden Dataset)
        double goldenMatchRate = 0.0;
        int goldenProductCount = 0;
        int goldenTotal = 0;
        int goldenMatched = 0;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Regression", "TestData", "golden_dataset_v1.json");
            if (System.IO.File.Exists(path))
            {
                var json = await System.IO.File.ReadAllTextAsync(path, cancellationToken);
                var doc = JsonSerializer.Deserialize<LocalGoldenDataset>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (doc?.Products != null)
                {
                    goldenProductCount = doc.Products.Count;
                    foreach (var product in doc.Products)
                    {
                        var profile = new NormalizedSkinProfile(product.Profile.SkinType, product.Profile.Concerns ?? [], product.Profile.Sensitivity ?? "low", DateTime.UtcNow, false);
                        var result = compatibilityEngine.Evaluate(product.Ingredients, profile);
                        goldenTotal += product.Ingredients.Count;
                        goldenMatched += result.Beneficial.Count + result.Harmful.Count;
                    }
                    goldenMatchRate = goldenTotal == 0 ? 0.0 : (double)goldenMatched / goldenTotal * 100;
                }
            }
        }
        catch
        {
            // fallback if dataset v1 file not found
        }

        // 3. Behavioral Metrics
        var lookups = await db.IngredientLookups.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(100) // inspect last 100 searches
            .Select(x => x.InputText)
            .ToListAsync(cancellationToken);

        var missCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var text in lookups)
        {
            var parts = text.Split(new[] { '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x));
            
            var listAnalysis = compatibilityEngine.AnalyzeList(parts);
            foreach (var rawName in listAnalysis.MissedIngredients)
            {
                var key = rawName.ToLowerInvariant();
                missCounts[key] = missCounts.GetValueOrDefault(key) + 1;
            }
        }

        var topMissed = missCounts.OrderByDescending(x => x.Value)
            .Take(10)
            .Select(x => new { Ingredient = x.Key, Frequency = x.Value })
            .ToList();

        return Ok(new
        {
            staticMetrics = new
            {
                totalIngredientsInDb = dbIngredients.Count,
                matchedIngredientsInDb = dbAnalysis.MatchedIngredients,
                ruleCoverageRate = Math.Round(ruleCoverageRate, 2)
            },
            controlledMetrics = new
            {
                goldenDatasetProductCount = goldenProductCount,
                goldenDatasetMatchRate = Math.Round(goldenMatchRate, 2)
            },
            behavioralMetrics = new
            {
                topMissedIngredients = topMissed
            }
        });
    }

    private sealed class LocalGoldenDataset
    {
        public List<LocalGoldenProduct> Products { get; set; } = [];
    }

    private sealed class LocalGoldenProduct
    {
        public LocalGoldenProfile Profile { get; set; } = new();
        public List<string> Ingredients { get; set; } = [];
    }

    private sealed class LocalGoldenProfile
    {
        public string SkinType { get; set; } = "normal";
        public List<string>? Concerns { get; set; }
        public string? Sensitivity { get; set; }
    }

    private async Task SaveLookupAsync(Guid userId, string input, string? imageUrl, IngredientScanResult result, CancellationToken cancellationToken)
    {
        db.IngredientLookups.Add(new IngredientLookup
        {
            UserId = userId,
            InputText = input,
            ImageUrl = imageUrl,
            OcrText = imageUrl is null ? null : input,
            AiResult = JsonSerializer.Serialize(result),
            SafetyScore = result.SafetyScore,
            SuitableSkinTypes = string.Join(", ", result.Recommendations),
            WarningNotes = string.Join("; ", result.Harmful.Select(x => x.Reason))
        });
        db.AiUsageLogs.Add(new AiUsageLog
        {
            UserId = userId,
            FeatureName = "ingredient",
            TokenUsed = Math.Max(1, input.Length / 4),
            RequestData = input,
            ResponseData = JsonSerializer.Serialize(result)
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IngredientDto ToDto(Ingredient ingredient) =>
        new(
            ingredient.Id,
            ingredient.NameInc,
            ingredient.Name,
            ingredient.Category,
            ingredient.Description,
            ingredient.Links,
            ingredient.CreatedAt,
            ingredient.UpdatedAt);
}

public sealed record IngredientAnalyzeTextRequest(Guid UserId, string InputText, string? SkinType, IReadOnlyCollection<string>? Allergies);
public sealed record IngredientAnalyzeImageRequest(Guid UserId, string ImageUrl, string? OcrText, string? SkinType, IReadOnlyCollection<string>? Allergies);
