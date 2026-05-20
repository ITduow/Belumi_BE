using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/ingredients")]
public sealed class IngredientController(IAiBeautyService aiBeautyService, BelumiDbContext db) : ControllerBase
{
    [HttpPost("lookup")]
    public ActionResult<IngredientLookupResult> Lookup(IngredientLookupRequest request) =>
        Ok(aiBeautyService.LookupIngredients(request));

    [HttpPost("scan")]
    public ActionResult<IngredientScanResult> Scan(IngredientScanRequest request) =>
        Ok(aiBeautyService.AnalyzeIngredientLabel(request));

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
}

public sealed record IngredientAnalyzeTextRequest(Guid UserId, string InputText, string? SkinType, IReadOnlyCollection<string>? Allergies);
public sealed record IngredientAnalyzeImageRequest(Guid UserId, string ImageUrl, string? OcrText, string? SkinType, IReadOnlyCollection<string>? Allergies);
