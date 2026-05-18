using Belumi.Core.DTOs;
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
        try
        {
            return Ok(await authService.RegisterAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.LoginAsync(request, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("admin-login")]
    public Task<ActionResult<AuthResponse>> AdminLogin(LoginRequest request, CancellationToken cancellationToken) =>
        Login(request, cancellationToken);

    [HttpPost("firebase-login")]
    public async Task<ActionResult<AuthResponse>> FirebaseLogin(FirebaseLoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.FirebaseLoginAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("google-mock")]
    public async Task<ActionResult<AuthResponse>> GoogleMock(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.RegisterAsync(request with { Password = "GoogleMock@2026" }, cancellationToken));
        }
        catch (InvalidOperationException)
        {
            return await Login(new LoginRequest(request.Email, "GoogleMock@2026"), cancellationToken);
        }
    }
}
