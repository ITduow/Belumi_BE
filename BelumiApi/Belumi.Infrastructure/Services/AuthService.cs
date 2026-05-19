using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Core.Interfaces;
using Belumi.Infrastructure.Data;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Belumi.Infrastructure.Services;

public sealed class AuthService(
    BelumiDbContext db,
    IConfiguration configuration,
    FirebaseAdminAppFactory firebaseAdminAppFactory) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Phone = request.Phone,
            PasswordHash = PasswordHasher.Hash(request.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        return ToResponse(user);
    }

    public async Task<AuthResponse> FirebaseLoginAsync(FirebaseLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            throw new UnauthorizedAccessException("Firebase ID token is required.");
        }

        firebaseAdminAppFactory.GetOrCreate();

        FirebaseToken decodedToken;
        try
        {
            decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken, cancellationToken);
        }
        catch (Exception ex) when (ex is FirebaseAuthException or ArgumentException)
        {
            throw new UnauthorizedAccessException("Invalid Firebase ID token.");
        }

        var email = decodedToken.Claims.TryGetValue("email", out var emailValue)
            ? emailValue?.ToString()?.Trim().ToLowerInvariant()
            : null;

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Firebase token does not contain an email.");
        }

        var name = decodedToken.Claims.TryGetValue("name", out var nameValue)
            ? nameValue?.ToString()
            : null;

        var picture = decodedToken.Claims.TryGetValue("picture", out var pictureValue)
            ? pictureValue?.ToString()
            : null;

        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Email = email,
                FullName = string.IsNullOrWhiteSpace(name) ? email : name,
                AvatarUrl = picture,
                PasswordHash = PasswordHasher.Hash($"firebase:{decodedToken.Uid}:{Guid.NewGuid()}"),
                Role = UserRole.Customer
            };
            db.Users.Add(user);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                user.FullName = name;
            }

            if (!string.IsNullOrWhiteSpace(picture))
            {
                user.AvatarUrl = picture;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var token = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (token is null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        token.IsRevoked = true;
        await db.SaveChangesAsync(cancellationToken);

        return await ToResponseWithRefreshAsync(token.User!, cancellationToken);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (token is null) return;
        token.IsRevoked = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    private AuthResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.FullName, user.Phone, user.Role, CreateToken(user));

    private async Task<AuthResponse> ToResponseWithRefreshAsync(User user, CancellationToken cancellationToken)
    {
        var refreshToken = new Belumi.Core.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        return new(user.Id, user.Email, user.FullName, user.Phone, user.Role, CreateToken(user), refreshToken.Token);
    }

    private string CreateToken(User user)
    {
        var key = configuration["Jwt:Key"] ?? "BelumiBeautyLocalDevelopmentKeyMustBeLong";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(14),
            signingCredentials: credentials));
    }
}

