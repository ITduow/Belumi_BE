using System.Security.Claims;
using System.Text.Encodings.Web;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Belumi.Infrastructure.Services;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Belumi.API.Common;

public sealed class BelumiBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    BelumiDbContext db,
    FirebaseAdminAppFactory firebaseAdminAppFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "BelumiFirebase";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var principal = await ValidateFirebaseTokenAsync(token);
        return principal is null
            ? AuthenticateResult.Fail("Invalid Firebase ID token.")
            : AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }

    private async Task<ClaimsPrincipal?> ValidateFirebaseTokenAsync(string token)
    {
        FirebaseToken decodedToken;
        try
        {
            firebaseAdminAppFactory.GetOrCreate();
            decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
        }
        catch (Exception ex) when (ex is FirebaseAuthException or ArgumentException)
        {
            return null;
        }

        var email = decodedToken.Claims.TryGetValue("email", out var emailValue)
            ? emailValue?.ToString()?.Trim().ToLowerInvariant()
            : null;

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(
            x => x.FirebaseUid == decodedToken.Uid || (!string.IsNullOrWhiteSpace(email) && x.Email == email));

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var role = FirebaseRoleClaimReader.ResolveRole(decodedToken.Claims);
        Logger.LogInformation(
            "Firebase auth resolved {Email} ({FirebaseUid}) with token role {TokenRole}, DB role {DbRole}.",
            user.Email,
            decodedToken.Uid,
            role,
            user.Role);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role.ToString()),
            new("firebase_uid", decodedToken.Uid)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
    }
}
