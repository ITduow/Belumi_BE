using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Core.Exceptions;
using Belumi.Core.Interfaces;
using Belumi.Infrastructure.Data;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;

namespace Belumi.Infrastructure.Services;

public sealed class AuthService(
    BelumiDbContext db,
    FirebaseAdminAppFactory firebaseAdminAppFactory,
    FirebaseRoleService firebaseRoleService) : IAuthService
{
    public async Task<AuthResponse> FirebaseLoginAsync(FirebaseLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            throw new UnauthorizedException("Firebase ID token is required.");
        }

        firebaseAdminAppFactory.GetOrCreate();

        FirebaseToken decodedToken;
        try
        {
            decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken, cancellationToken);
        }
        catch (Exception ex) when (ex is FirebaseAuthException or ArgumentException)
        {
            throw new UnauthorizedException("Invalid Firebase ID token.");
        }

        var firebaseUid = decodedToken.Uid;
        var firestoreRole = await firebaseRoleService.GetRoleAsync(decodedToken, cancellationToken);
        var email = decodedToken.Claims.TryGetValue("email", out var emailValue)
            ? emailValue?.ToString()?.Trim().ToLowerInvariant()
            : null;

        var name = decodedToken.Claims.TryGetValue("name", out var nameValue)
            ? nameValue?.ToString()
            : null;

        var picture = decodedToken.Claims.TryGetValue("picture", out var pictureValue)
            ? pictureValue?.ToString()
            : null;

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedException("Firebase login requires an email.");
        }

        var user = await db.Users.FirstOrDefaultAsync(
            x => x.Email == email || x.FirebaseUid == firebaseUid,
            cancellationToken);

        if (user is null)
        {
            user = new User
            {
                FirebaseUid = firebaseUid,
                Email = email,
                FullName = string.IsNullOrWhiteSpace(name) ? email : name,
                AvatarUrl = picture,
                PasswordHash = PasswordHasher.Hash($"firebase:{firebaseUid}:{Guid.NewGuid()}"),
                Role = firestoreRole ?? UserRole.Customer
            };
            db.Users.Add(user);
        }
        else
        {
            if (!user.IsActive)
            {
                throw new UnauthorizedException("User is inactive.");
            }

            user.FirebaseUid ??= firebaseUid;
            if (firestoreRole.HasValue)
            {
                user.Role = firestoreRole.Value;
            }

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
        return new AuthResponse(user.Id, user.Email, user.FullName, user.Phone, user.Role, request.IdToken);
    }
}
