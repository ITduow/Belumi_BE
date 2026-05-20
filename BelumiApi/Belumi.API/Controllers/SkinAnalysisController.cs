using Belumi.Core.DTOs;
using Belumi.Core.Interfaces;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/skin-analysis")]
[Authorize]
public sealed class SkinAnalysisController(BelumiDbContext db, ISkinAnalysisService skinAnalysisService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SkinAnalysisResult>> Analyze(SkinAnalysisRequest request, CancellationToken cancellationToken)
    {
        var analysis = await skinAnalysisService.AnalyzeAsync(User.GetUserId(), request, cancellationToken);
        return Ok(new SkinAnalysisResult(analysis.Id, analysis.ImageUrl, analysis.SkinType, analysis.Concerns, analysis.Recommendations, analysis.Score, analysis.AnalyzedAt));
    }

    [HttpGet("my")]
    public async Task<IActionResult> My(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await db.SkinAnalyses.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.AnalyzedAt).ToListAsync(cancellationToken));
    }
}
