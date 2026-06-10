using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/subscription")]
public sealed class RevenueCatWebhookController(
    BelumiDbContext db,
    ILogger<RevenueCatWebhookController> logger,
    IConfiguration configuration) : ControllerBase
{
    private readonly string _webhookSecret = configuration["RevenueCat:WebhookSecret"] ?? "";

    [HttpPost("revenuecat-webhook")]
    public async Task<IActionResult> HandleWebhook(
        [FromBody] RevenueCatWebhookRequest request,
        [FromHeader(Name = "Authorization")] string? authorizationHeader,
        CancellationToken cancellationToken)
    {
        // 1. Verify Secret Token if configured
        if (!string.IsNullOrEmpty(_webhookSecret) && authorizationHeader != _webhookSecret)
        {
            logger.LogWarning("Unauthorized RevenueCat webhook request received.");
            return Unauthorized("Invalid webhook secret.");
        }

        if (request?.Event == null)
        {
            logger.LogWarning("RevenueCat Webhook received with null event body.");
            return BadRequest("Event payload is required.");
        }

        var ev = request.Event;
        logger.LogInformation("Processing RevenueCat event: Type={Type}, AppUserId={AppUserId}, ProductId={ProductId}", 
            ev.Type, ev.AppUserId, ev.ProductId);

        if (!Guid.TryParse(ev.AppUserId, out var userId))
        {
            logger.LogWarning("Invalid AppUserId format: {AppUserId}. Must be a valid Guid.", ev.AppUserId);
            return BadRequest("Invalid AppUserId format.");
        }

        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found in database.", userId);
            // Return 200 OK so RevenueCat does not retry indefinitely for non-existent users
            return Ok(new { message = "User not found, event skipped." });
        }

        var planName = ev.ProductId.ToLower().Contains("pro") ? "Pro" : "Plus";
        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Name.ToLower() == planName.ToLower(), cancellationToken);
        if (plan == null)
        {
            logger.LogWarning("Subscription plan '{PlanName}' not found in database.", planName);
            return BadRequest($"Plan '{planName}' not found.");
        }

        DateTime? expiryDate = ev.ExpirationAtMs.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(ev.ExpirationAtMs.Value).UtcDateTime
            : null;

        DateTime startDate = ev.PurchasedAtMs.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(ev.PurchasedAtMs.Value).UtcDateTime
            : DateTime.UtcNow;

        switch (ev.Type)
        {
            case "INITIAL_PURCHASE":
            case "RENEWAL":
            case "PRODUCT_CHANGE":
            case "UNCANCELLATION":
                // Upgrade/Renew user status
                user.SubscriptionPlan = planName;
                db.Users.Update(user);

                var existingSub = await db.UserSubscriptions
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.PlanId == plan.Id, cancellationToken);

                if (existingSub == null)
                {
                    db.UserSubscriptions.Add(new UserSubscription
                    {
                        UserId = user.Id,
                        PlanId = plan.Id,
                        Status = "Active",
                        StartDate = startDate,
                        EndDate = expiryDate,
                        PaymentStatus = "Paid"
                    });
                }
                else
                {
                    existingSub.Status = "Active";
                    existingSub.StartDate = startDate;
                    existingSub.EndDate = expiryDate;
                    existingSub.PaymentStatus = "Paid";
                    db.UserSubscriptions.Update(existingSub);
                }

                logger.LogInformation("Successfully updated subscription to Active (Plan={PlanName}) for user {UserId}. Expiry={Expiry}", 
                    planName, userId, expiryDate);
                break;

            case "CANCELLATION":
                // If it is a cancellation, it might just mean auto-renew is off but subscription is active until expiry
                if (expiryDate.HasValue && expiryDate.Value > DateTime.UtcNow)
                {
                    logger.LogInformation("User {UserId} cancelled auto-renew. Subscription remains active until {Expiry}", 
                        userId, expiryDate);
                }
                else
                {
                    user.SubscriptionPlan = "Free";
                    db.Users.Update(user);

                    var activeSub = await db.UserSubscriptions
                        .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == "Active", cancellationToken);
                    if (activeSub != null)
                    {
                        activeSub.Status = "Cancelled";
                        db.UserSubscriptions.Update(activeSub);
                    }
                    logger.LogInformation("Subscription expired/cancelled immediately for user {UserId}", userId);
                }
                break;

            case "EXPIRATION":
            case "BILLING_ISSUE":
                // Revoke premium access
                user.SubscriptionPlan = "Free";
                db.Users.Update(user);

                var expiredSub = await db.UserSubscriptions
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == "Active", cancellationToken);
                if (expiredSub != null)
                {
                    expiredSub.Status = "Expired";
                    db.UserSubscriptions.Update(expiredSub);
                }

                logger.LogInformation("Subscription expired/revoked for user {UserId}", userId);
                break;

            default:
                logger.LogInformation("Skipped processing for unhandled RevenueCat event type: {Type}", ev.Type);
                break;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Event processed successfully." });
    }
}

public sealed class RevenueCatWebhookRequest
{
    [JsonPropertyName("event")]
    public RevenueCatEvent? Event { get; set; }
}

public sealed class RevenueCatEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("app_user_id")]
    public string AppUserId { get; set; } = string.Empty;

    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("entitlement_ids")]
    public List<string>? EntitlementIds { get; set; }

    [JsonPropertyName("expiration_at_ms")]
    public long? ExpirationAtMs { get; set; }

    [JsonPropertyName("purchased_at_ms")]
    public long? PurchasedAtMs { get; set; }
}
