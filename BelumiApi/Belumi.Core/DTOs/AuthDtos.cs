using Belumi.Core.Entities;

namespace Belumi.Core.DTOs;

public sealed record FirebaseLoginRequest(string IdToken);

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    string? Phone,
    UserRole Role,
    string Token);
