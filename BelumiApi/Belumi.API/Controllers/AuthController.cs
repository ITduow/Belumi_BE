using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Core.Exceptions;
using Belumi.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        return Ok(await authService.RegisterAsync(request, cancellationToken));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await authService.LoginAsync(request, cancellationToken));
    }

    [HttpPost("admin-login")]
    public async Task<ActionResult<AuthResponse>> AdminLogin(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        if (response.Role != UserRole.Admin)
        {
            throw new ForbiddenException("Bạn không có quyền truy cập");
        }
        return Ok(response);
    }

    [HttpPost("firebase-login")]
    public async Task<ActionResult<AuthResponse>> FirebaseLogin(FirebaseLoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await authService.FirebaseLoginAsync(request, cancellationToken));
    }

#if DEBUG
    [HttpPost("google-mock")]
    public async Task<ActionResult<AuthResponse>> GoogleMock(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.RegisterAsync(request with { Password = "GoogleMock@2026" }, cancellationToken));
        }
        catch (ConflictException)
        {
            return await Login(new LoginRequest(request.Email, "GoogleMock@2026"), cancellationToken);
        }
    }
#endif
}
