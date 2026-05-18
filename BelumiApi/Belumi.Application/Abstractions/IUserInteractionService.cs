using Belumi.Core.DTOs;
using Belumi.Core.Entities;

namespace Belumi.Application.Abstractions;

public interface IUserInteractionService
{
    Task<IReadOnlyCollection<WishlistItem>> GetWishlistAsync(Guid userId, CancellationToken cancellationToken);
    Task AddWishlistItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken);
    Task RemoveWishlistItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken);
    Task<User?> GetMeAsync(Guid userId, CancellationToken cancellationToken);
    Task<BeautyProfile> UpdateBeautyProfileAsync(Guid userId, BeautyProfileRequest request, CancellationToken cancellationToken);
}
