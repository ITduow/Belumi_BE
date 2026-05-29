using Belumi.Core.Entities;

namespace Belumi.Infrastructure.Services;

public static class FirebaseRoleClaimReader
{
    public static UserRole ResolveRole(IReadOnlyDictionary<string, object> claims)
    {
        return TryReadString(claims, "role")?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true
            ? UserRole.Admin
            : UserRole.Customer;
    }

    private static string? TryReadString(IReadOnlyDictionary<string, object> claims, string key)
    {
        return claims.TryGetValue(key, out var value)
            ? value?.ToString()?.Trim()
            : null;
    }
}
