using Belumi.Core.DTOs;
using Belumi.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("firebase-login")]
    public async Task<ActionResult<AuthResponse>> FirebaseLogin(
        FirebaseLoginRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await authService.FirebaseLoginAsync(request, cancellationToken));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponse>> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.RefreshTokenAsync(request.RefreshToken, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await authService.RevokeTokenAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }
}
