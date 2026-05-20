using Belumi.Core.DTOs;

namespace Belumi.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> FirebaseLoginAsync(FirebaseLoginRequest request, CancellationToken cancellationToken);
}
