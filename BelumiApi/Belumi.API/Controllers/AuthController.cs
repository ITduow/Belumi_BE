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
}
