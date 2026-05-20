using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class AiUsageController(BelumiDbContext db) : ControllerBase
{
    [HttpGet("ai-usage/{userId:guid}")]
    public async Task<IActionResult> UserUsage(Guid userId, CancellationToken cancellationToken)
    {
        if (User.GetUserId() != userId && !User.IsInRole(nameof(UserRole.Admin)))
        {
            return Forbid();
        }

        return Ok(await db.AiUsageLogs.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("admin/ai-usage")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> AdminUsage(CancellationToken cancellationToken) =>
        Ok(await db.AiUsageLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(300).ToListAsync(cancellationToken));
}
