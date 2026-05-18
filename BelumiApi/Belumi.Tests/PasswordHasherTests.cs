using Belumi.Infrastructure.Services;

namespace Belumi.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Verify_ReturnsTrue_ForOriginalPassword()
    {
        var hash = PasswordHasher.Hash("Admin@123");

        Assert.True(PasswordHasher.Verify("Admin@123", hash));
        Assert.False(PasswordHasher.Verify("wrong-password", hash));
    }
}
