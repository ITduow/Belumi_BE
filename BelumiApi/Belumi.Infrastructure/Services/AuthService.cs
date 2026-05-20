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
<<<<<<< Updated upstream
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new ConflictException("Email already exists.");
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
            ?? throw new UnauthorizedException("Invalid credentials.");

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User is inactive.");
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials.");
        }

        return ToResponse(user);
    }

=======
>>>>>>> Stashed changes
    public async Task<AuthResponse> FirebaseLoginAsync(FirebaseLoginRequest request, CancellationToken cancellationToken)
    {
        string? firebaseUid = request.FirebaseUid;
        string? email = request.Email?.Trim().ToLowerInvariant();
        string? name = request.FullName;
        string? picture = request.AvatarUrl;

        if (!string.IsNullOrWhiteSpace(request.IdToken))
        {
<<<<<<< Updated upstream
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

            firebaseUid = decodedToken.Uid;
            email = decodedToken.Claims.TryGetValue("email", out var emailValue)
                ? emailValue?.ToString()?.Trim().ToLowerInvariant()
                : null;

            name = decodedToken.Claims.TryGetValue("name", out var nameValue)
                ? nameValue?.ToString()
                : name;

            picture = decodedToken.Claims.TryGetValue("picture", out var pictureValue)
                ? pictureValue?.ToString()
                : picture;
=======
            throw new UnauthorizedException("Firebase ID token is required.");
>>>>>>> Stashed changes
        }

        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            throw new UnauthorizedException("Firebase UID or ID token is required.");
        }
<<<<<<< Updated upstream

=======
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

>>>>>>> Stashed changes
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedException("Firebase login requires an email.");
        }

<<<<<<< Updated upstream
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email || x.FirebaseUid == firebaseUid, cancellationToken);
=======
        var user = await db.Users.FirstOrDefaultAsync(
            x => x.Email == email || x.FirebaseUid == firebaseUid,
            cancellationToken);

>>>>>>> Stashed changes
        if (user is null)
        {
            user = new User
            {
                FirebaseUid = firebaseUid,
                Email = email,
                FullName = string.IsNullOrWhiteSpace(name) ? email : name,
                AvatarUrl = picture,
                PasswordHash = PasswordHasher.Hash($"firebase:{firebaseUid}:{Guid.NewGuid()}"),
<<<<<<< Updated upstream
                Role = UserRole.Customer
=======
                Role = firestoreRole ?? UserRole.Customer
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
=======
            if (firestoreRole.HasValue)
            {
                user.Role = firestoreRole.Value;
            }

>>>>>>> Stashed changes
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
