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
[Route("api/makeup")]
public sealed class MakeupController(IAiBeautyService aiBeautyService, BelumiDbContext db) : ControllerBase
{
    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken) =>
        Ok(await aiBeautyService.GetMakeupCatalogAsync(cancellationToken));

    [HttpPost("consultation")]
    public ActionResult<MakeupConsultationResult> Consult(MakeupConsultationRequest request) =>
        Ok(aiBeautyService.ConsultMakeup(request));

    [HttpPost("consult")]
    [Authorize]
    public async Task<ActionResult<MakeupConsultationResult>> ConsultV2(MakeupConsultRequest request, CancellationToken cancellationToken)
    {
        if (!await User.CheckDailyLimitAsync(db, "makeup"))
        {
            return StatusCode(429, new { message = "Bạn đã dùng hết lượt tư vấn trang điểm miễn phí hôm nay. Vui lòng nâng cấp lên gói Paid để sử dụng không giới hạn!" });
        }

        var userId = request.UserId == Guid.Empty ? User.GetUserId() : request.UserId;
        var result = aiBeautyService.ConsultMakeup(new MakeupConsultationRequest(request.SkinTone, request.Occasion, request.StylePreference));
        db.MakeupConsultations.Add(new MakeupConsultation
        {
            UserId = userId,
            SkinTone = request.SkinTone,
            Occasion = request.Occasion,
            StylePreference = request.StylePreference,
            Note = request.Note,
            AiResult = JsonSerializer.Serialize(result),
            LipColorSuggestion = result.Lips,
            FoundationSuggestion = result.Base,
            EyeMakeupSuggestion = result.Eyes,
            BlushSuggestion = "Soft blush matched to your undertone"
        });
        db.AiUsageLogs.Add(new AiUsageLog
        {
            UserId = userId,
            FeatureName = "makeup",
            TokenUsed = 120,
            RequestData = JsonSerializer.Serialize(request),
            ResponseData = JsonSerializer.Serialize(result)
        });
        await db.SaveChangesAsync(cancellationToken);
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

        return Ok(await db.MakeupConsultations.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken));
    }

    [HttpPost("try-on")]
    [Authorize]
    public async Task<ActionResult<MakeupTryOnResult>> TryOn(MakeupTryOnRequest request, CancellationToken cancellationToken)
    {
        if (!await User.CheckDailyLimitAsync(db, "makeup_tryon"))
        {
            return StatusCode(429, new { message = "Bạn đã dùng hết lượt thử trang điểm ảo miễn phí hôm nay. Vui lòng nâng cấp lên gói Paid để sử dụng không giới hạn!" });
        }

        var userId = User.GetUserId();
        var result = aiBeautyService.TryOnMakeup(request);

        db.AiUsageLogs.Add(new AiUsageLog
        {
            UserId = userId,
            FeatureName = "makeup_tryon",
            TokenUsed = 50,
            RequestData = JsonSerializer.Serialize(request),
            ResponseData = JsonSerializer.Serialize(result)
        });
        await db.SaveChangesAsync(cancellationToken);

        return Ok(result);
    }
}

public sealed record MakeupConsultRequest(Guid UserId, string SkinTone, string Occasion, string StylePreference, string? Note);
