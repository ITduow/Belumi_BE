namespace Belumi.Core.Entities;

public sealed class User : BaseEntity
{
    public string? FirebaseUid { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public string SubscriptionPlan { get; set; } = "Free";
    public bool IsActive { get; set; } = true;
    public BeautyProfile? BeautyProfile { get; set; }
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
