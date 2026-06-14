using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Belumi.Infrastructure.Services;

public sealed class UserInteractionService(BelumiDbContext db) : IUserInteractionService
{
    public async Task<IReadOnlyCollection<WishlistItem>> GetWishlistAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.WishlistItems.AsNoTracking().Include(x => x.Product).Where(x => x.UserId == userId).ToListAsync(cancellationToken);

    public async Task AddWishlistItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken)
    {
        if (!await db.WishlistItems.AnyAsync(x => x.UserId == userId && x.ProductId == productId, cancellationToken))
        {
            db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveWishlistItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken)
    {
        var item = await db.WishlistItems.FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId, cancellationToken);
        if (item is null)
        {
            return;
        }

        db.WishlistItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<User?> GetMeAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().Include(x => x.BeautyProfile).FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public async Task<BeautyProfile> UpdateBeautyProfileAsync(Guid userId, BeautyProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await db.BeautyProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (profile is null)
        {
            profile = new BeautyProfile { UserId = userId };
            db.BeautyProfiles.Add(profile);
        }

        profile.SkinType = request.SkinType;
        profile.SkinConcerns = request.SkinConcerns;
        profile.Allergies = request.Allergies;
        await db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<bool> DeleteAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.NewsLikes.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.NewsSaves.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.WishlistItems.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.BeautyProfiles.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.SkinAnalyses.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.IngredientLookups.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.MakeupConsultations.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.AiUsageLogs.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Payments.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.UserSubscriptions.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }
}
