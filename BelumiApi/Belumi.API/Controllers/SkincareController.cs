using Belumi.Core.DTOs;
using Belumi.Core.Interfaces;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/skincare")]
[Authorize]
public sealed class SkincareController(BelumiDbContext db, ISkinAnalysisService skinAnalysisService) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(SkincareAnalyzeRequest request, CancellationToken cancellationToken)
    {
        var userId = request.UserId == Guid.Empty ? User.GetUserId() : request.UserId;
        var analysis = await skinAnalysisService.AnalyzeAsync(
            userId,
            new SkinAnalysisRequest(null, request.SkinType, request.SkinConcerns, request.UserNote, null),
            cancellationToken);

        analysis.AgeRange = request.AgeRange;
        analysis.SensitivityLevel = request.SensitivityLevel;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            analysis = analysis.AiResult ?? analysis.Recommendations,
            morningRoutine = analysis.MorningRoutine,
            nightRoutine = analysis.NightRoutine,
            recommendedIngredients = SplitCsv(analysis.RecommendedIngredients),
            avoidIngredients = SplitCsv(analysis.AvoidIngredients)
        });
    }

    [HttpGet("history/{userId:guid}")]
    public async Task<IActionResult> History(Guid userId, CancellationToken cancellationToken)
    {
        if (userId != User.GetUserId() && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return Ok(await db.SkinAnalyses.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AnalyzedAt)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.SkinAnalyses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    private static IReadOnlyCollection<string> SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed record SkincareAnalyzeRequest(Guid UserId, string SkinType, IReadOnlyCollection<string> SkinConcerns, string? AgeRange, string? SensitivityLevel, string? UserNote);
