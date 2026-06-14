using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public sealed class AccountController(IUserInteractionService userInteractionService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await userInteractionService.GetMeAsync(User.GetUserId(), cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("beauty-profile")]
    public async Task<IActionResult> UpdateBeautyProfile(BeautyProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await userInteractionService.UpdateBeautyProfileAsync(User.GetUserId(), request, cancellationToken);
        return Ok(profile);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        var deleted = await userInteractionService.DeleteAccountAsync(User.GetUserId(), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
