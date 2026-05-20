using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Core.Interfaces;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminAuthController(IAuthService authService, BelumiDbContext db) : ControllerBase
{
    [HttpPost("auth/login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return response.Role == UserRole.Admin ? Ok(response) : Unauthorized(new { message = "Admin permission required." });
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return Ok(new
        {
            totalUsers = await db.Users.CountAsync(cancellationToken),
            activeUsers = await db.Users.CountAsync(x => x.IsActive, cancellationToken),
            totalSkincareAnalyses = await db.SkinAnalyses.CountAsync(cancellationToken),
            totalIngredientLookups = await db.IngredientLookups.CountAsync(cancellationToken),
            totalMakeupConsultations = await db.MakeupConsultations.CountAsync(cancellationToken),
            totalSubscriptions = await db.UserSubscriptions.CountAsync(cancellationToken),
            totalPayments = await db.Payments.CountAsync(cancellationToken),
            totalNews = await db.BlogPosts.CountAsync(cancellationToken),
            aiUsageThisMonth = await db.AiUsageLogs.CountAsync(x => x.CreatedAt >= startOfMonth, cancellationToken)
        });
    }
}
