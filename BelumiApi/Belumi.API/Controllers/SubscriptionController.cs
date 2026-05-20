using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/subscription")]
public sealed class SubscriptionController(BelumiDbContext db) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<IActionResult> Plans(CancellationToken cancellationToken) =>
        Ok(await db.SubscriptionPlans.AsNoTracking().OrderBy(x => x.Price).ToListAsync(cancellationToken));

    [HttpGet("user/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> UserSubscription(Guid userId, CancellationToken cancellationToken)
    {
        var subscription = await db.UserSubscriptions.AsNoTracking()
            .Include(x => x.Plan)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return Ok(subscription);
    }

    [HttpPost("upgrade")]
    [Authorize]
    public async Task<IActionResult> Upgrade(SubscriptionUpgradeRequest request, CancellationToken cancellationToken)
    {
        var userId = request.UserId == Guid.Empty ? User.GetUserId() : request.UserId;
        var user = await db.Users.FindAsync([userId], cancellationToken);
        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Name.ToLower() == request.PlanName.Trim().ToLower(), cancellationToken);
        if (user is null || plan is null)
        {
            return NotFound(new { message = "User or plan not found." });
        }

        user.SubscriptionPlan = plan.Name;
        var subscription = new UserSubscription
        {
            UserId = user.Id,
            PlanId = plan.Id,
            Status = "Active",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            PaymentStatus = "MockPaid"
        };
        db.UserSubscriptions.Add(subscription);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(subscription);
    }
}

public sealed record SubscriptionUpgradeRequest(Guid UserId, string PlanName);
