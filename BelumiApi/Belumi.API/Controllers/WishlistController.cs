using Belumi.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public sealed class WishlistController(IUserInteractionService userInteractionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        return Ok(await userInteractionService.GetWishlistAsync(User.GetUserId(), cancellationToken));
    }

    [HttpPost("{productId:guid}")]
    public async Task<IActionResult> Add(Guid productId, CancellationToken cancellationToken)
    {
        await userInteractionService.AddWishlistItemAsync(User.GetUserId(), productId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> Remove(Guid productId, CancellationToken cancellationToken)
    {
        await userInteractionService.RemoveWishlistItemAsync(User.GetUserId(), productId, cancellationToken);
        return NoContent();
    }
}
