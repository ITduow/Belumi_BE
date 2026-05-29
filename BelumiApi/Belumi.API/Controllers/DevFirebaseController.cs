using Belumi.Infrastructure.Services;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/dev/firebase")]
public sealed class DevFirebaseController(
    FirebaseAdminAppFactory firebaseAdminAppFactory,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("set-admin-claim")]
    public async Task<IActionResult> SetAdminClaim(SetAdminClaimRequest request, CancellationToken cancellationToken)
    {
        if (!IsDevToolAllowed())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Uid))
        {
            return BadRequest(new { message = "uid is required." });
        }

        firebaseAdminAppFactory.GetOrCreate();
        var user = await FirebaseAuth.DefaultInstance.GetUserAsync(request.Uid, cancellationToken);
        var claims = user.CustomClaims is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(user.CustomClaims);

        claims["role"] = "admin";
        await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(request.Uid, claims, cancellationToken);

        return Ok(new { uid = request.Uid, role = "admin" });
    }

    private bool IsDevToolAllowed()
    {
        if (environment.IsDevelopment())
        {
            return true;
        }

        var expectedSecret = configuration["DevTools:Secret"] ?? configuration["DEV_TOOLS_SECRET"];
        if (string.IsNullOrWhiteSpace(expectedSecret))
        {
            return false;
        }

        return Request.Headers.TryGetValue("X-Dev-Key", out var providedSecret)
            && providedSecret.ToString() == expectedSecret;
    }
}

public sealed record SetAdminClaimRequest(string Uid);
