using Belumi.Core.Entities;
using Belumi.Infrastructure.Services;

namespace Belumi.Tests;

public sealed class FirebaseRoleClaimReaderTests
{
    [Fact]
    public void ResolveRole_ReturnsAdmin_WhenRoleClaimIsAdmin()
    {
        var claims = new Dictionary<string, object>
        {
            ["role"] = "admin"
        };

        Assert.Equal(UserRole.Admin, FirebaseRoleClaimReader.ResolveRole(claims));
    }

    [Fact]
    public void ResolveRole_ReturnsCustomer_WhenRoleClaimIsMissing()
    {
        Assert.Equal(UserRole.Customer, FirebaseRoleClaimReader.ResolveRole(new Dictionary<string, object>()));
    }

    [Fact]
    public void ResolveRole_ReturnsCustomer_WhenRoleClaimIsNotAdmin()
    {
        var claims = new Dictionary<string, object>
        {
            ["role"] = "user"
        };

        Assert.Equal(UserRole.Customer, FirebaseRoleClaimReader.ResolveRole(claims));
    }
}
