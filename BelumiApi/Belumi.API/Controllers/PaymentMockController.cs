using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/payment")]
[Authorize]
public sealed class PaymentMockController(BelumiDbContext db) : ControllerBase
{
    [HttpPost("mock-checkout")]
    public async Task<IActionResult> MockCheckout(MockCheckoutRequest request, CancellationToken cancellationToken)
    {
        var userId = request.UserId == Guid.Empty ? User.GetUserId() : request.UserId;
        var user = await db.Users.FindAsync([userId], cancellationToken);
        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Name.ToLower() == request.PlanName.Trim().ToLower(), cancellationToken);
        if (user is null || plan is null)
        {
            return NotFound(new { message = "User or plan not found." });
        }

        var payment = new Payment
        {
            UserId = user.Id,
            PlanId = plan.Id,
            Amount = plan.Price,
            Currency = "VND",
            PaymentMethod = request.PaymentMethod ?? "Mock",
            PaymentStatus = "Paid",
            TransactionCode = $"BELUMI-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}"
        };
        user.SubscriptionPlan = plan.Name;
        db.Payments.Add(payment);
        db.UserSubscriptions.Add(new UserSubscription
        {
            UserId = user.Id,
            PlanId = plan.Id,
            Status = "Active",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            PaymentStatus = "Paid"
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(payment);
    }
}

public sealed record MockCheckoutRequest(Guid UserId, string PlanName, string? PaymentMethod);
