using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Belumi.Infrastructure.Data;

namespace Belumi.API.Controllers;

internal static class ControllerExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }

    public static async Task<bool> CheckDailyLimitAsync(this ClaimsPrincipal claimsUser, BelumiDbContext db, string featureName)
    {
        var userId = claimsUser.GetUserId();
        if (userId == Guid.Empty) return true;

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return true;

        var plan = user.SubscriptionPlan?.ToLowerInvariant() ?? "free";
        if (plan == "monthly" || plan == "yearly")
        {
            return true;
        }

        var todayUtc = DateTime.UtcNow.Date;

        if (featureName == "skin_analysis")
        {
            var count = await db.SkinAnalyses.CountAsync(x => x.UserId == userId && x.AnalyzedAt >= todayUtc);
            return count < 1;
        }
        else
        {
            var count = await db.AiUsageLogs.CountAsync(x => x.UserId == userId && x.FeatureName == featureName && x.CreatedAt >= todayUtc);
            return count < 1;
        }
    }
}
