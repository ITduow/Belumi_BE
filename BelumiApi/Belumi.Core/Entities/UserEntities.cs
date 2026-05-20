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
    public string? AgeRange { get; set; }
    public string? SensitivityLevel { get; set; }
    public string? UserNote { get; set; }
    public string? AiResult { get; set; }
    public string? MorningRoutine { get; set; }
    public string? NightRoutine { get; set; }
    public string? RecommendedIngredients { get; set; }
    public string? AvoidIngredients { get; set; }
    public string Recommendations { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

public sealed class IngredientLookup : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string InputText { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? OcrText { get; set; }
    public string AiResult { get; set; } = string.Empty;
    public int SafetyScore { get; set; }
    public string SuitableSkinTypes { get; set; } = string.Empty;
    public string WarningNotes { get; set; } = string.Empty;
}

public sealed class MakeupConsultation : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string SkinTone { get; set; } = string.Empty;
    public string Occasion { get; set; } = string.Empty;
    public string StylePreference { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string AiResult { get; set; } = string.Empty;
    public string LipColorSuggestion { get; set; } = string.Empty;
    public string FoundationSuggestion { get; set; } = string.Empty;
    public string EyeMakeupSuggestion { get; set; } = string.Empty;
    public string BlushSuggestion { get; set; } = string.Empty;
}

public sealed class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = "Free";
    public decimal Price { get; set; }
    public int MonthlyAiLimit { get; set; }
    public int IngredientLookupLimit { get; set; }
    public int MakeupConsultationLimit { get; set; }
    public bool CanUseAdvancedAnalysis { get; set; }
}

public sealed class UserSubscription : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string PaymentStatus { get; set; } = "MockPaid";
}

public sealed class AiUsageLog : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string FeatureName { get; set; } = string.Empty;
    public int TokenUsed { get; set; }
    public string RequestData { get; set; } = string.Empty;
    public string ResponseData { get; set; } = string.Empty;
}

public sealed class Payment : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string PaymentMethod { get; set; } = "Mock";
    public string PaymentStatus { get; set; } = "Pending";
    public string TransactionCode { get; set; } = string.Empty;
}
