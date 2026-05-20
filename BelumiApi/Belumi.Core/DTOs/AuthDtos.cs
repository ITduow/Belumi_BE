using Belumi.Core.Entities;

namespace Belumi.Core.DTOs;

<<<<<<< Updated upstream
public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string Email, string Password, string FullName, string? Phone);
public sealed record FirebaseLoginRequest(string? IdToken, string? FirebaseUid, string? Email, string? FullName, string? AvatarUrl);
=======
public sealed record FirebaseLoginRequest(string IdToken);
>>>>>>> Stashed changes
public sealed record AuthResponse(Guid UserId, string Email, string FullName, string? Phone, UserRole Role, string Token);
