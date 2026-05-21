using System.Security.Claims;
using System.Text.Encodings.Web;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Belumi.Infrastructure.Services;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Belumi.API.Common;

public sealed class BelumiBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    BelumiDbContext db,
    FirebaseAdminAppFactory firebaseAdminAppFactory,
    FirebaseRoleService firebaseRoleService,
    IConfiguration configuration)
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

        // 1. Try validating as a Firebase ID Token
        var principal = await ValidateFirebaseTokenAsync(token);
        
        // 2. If Firebase validation fails, try validating as our Custom JWT
        if (principal is null)
        {
            principal = ValidateCustomJwtToken(token);
        }

        return principal is null
            ? AuthenticateResult.Fail("Invalid Token (neither Firebase nor Custom JWT was valid).")
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

        var firestoreRole = await firebaseRoleService.GetRoleAsync(decodedToken);
        var role = firestoreRole ?? user.Role;
        Logger.LogInformation(
            "Firebase auth resolved {Email} ({FirebaseUid}) with Firestore role {FirestoreRole}, DB role {DbRole}, effective role {EffectiveRole}.",
            user.Email,
            decodedToken.Uid,
            firestoreRole?.ToString() ?? "missing",
            user.Role,
            role);

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

    private ClaimsPrincipal? ValidateCustomJwtToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var keyStr = configuration["Jwt:Key"] ?? "BelumiBeautyLocalDevelopmentKeyMustBeLong";
        var key = Encoding.UTF8.GetBytes(keyStr);
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var claims = jwtToken.Claims.ToList();

            Logger.LogInformation("Custom JWT validated successfully.");
            return new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Custom JWT validation failed: {Message}", ex.Message);
            return null;
        }
    }
}
