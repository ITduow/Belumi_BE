namespace Belumi.Core.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public BeautyProfile? BeautyProfile { get; set; }
}

public sealed class BeautyProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? SkinType { get; set; }
    public string? SkinConcerns { get; set; }
    public string? Allergies { get; set; }
}

public sealed class SkinAnalysis : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string SkinType { get; set; } = string.Empty;
    public string Concerns { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}
