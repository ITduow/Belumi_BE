namespace Belumi.Core.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    string Ingredients,
    string Benefits,
    decimal Price,
    string? ThumbnailUrl,
    Guid CategoryId,
    string? CategoryName,
    IReadOnlyCollection<string> Images);

public sealed record ContactRequestDto(string FullName, string Phone, string? Email, string Message);
public sealed record SkinAnalysisRequest(
    string? ImageUrl,
    string? SkinType,
    IReadOnlyCollection<string>? Concerns,
    string? Goal,
    string? PlanCode);
public sealed record SkinAnalysisResult(Guid Id, string ImageUrl, string SkinType, string Concerns, string Recommendations, int Score, DateTime AnalyzedAt);
public sealed record BeautyProfileRequest(string? SkinType, string? SkinConcerns, string? Allergies);
public sealed record IngredientDto(Guid Id, string NameInc, string Name, string Category, string Description, string Links, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record IngredientListResult(IReadOnlyCollection<IngredientDto> Items, int Total, int Page, int PageSize);
public sealed record IngredientCreateRequest(string NameInc, string Name, string Category, string Description, string Links);
public sealed record IngredientUpdateRequest(string NameInc, string Name, string Category, string Description, string Links);
public sealed record IngredientLookupRequest(string TextOrImageUrl);
public sealed record IngredientLookupResult(string Summary, IReadOnlyCollection<string> SafeIngredients, IReadOnlyCollection<string> Watchlist, IReadOnlyCollection<string> Recommendations);
public sealed record IngredientScanRequest(string RawTextOrImageUrl, string? SkinType, IReadOnlyCollection<string>? Allergies);
public sealed record IngredientScanItem(string Name, string Category, string Safety, string Reason);
public sealed record IngredientScanResult(int SafetyScore, string Status, string Summary, IReadOnlyCollection<IngredientScanItem> Beneficial, IReadOnlyCollection<IngredientScanItem> Neutral, IReadOnlyCollection<IngredientScanItem> Harmful, IReadOnlyCollection<string> Recommendations);
public sealed record MakeupConsultationRequest(string SkinTone, string Occasion, string Style);
public sealed record MakeupConsultationResult(string LookName, string Base, string Eyes, string Lips, IReadOnlyCollection<string> ProductSuggestions);
public sealed record MakeupTryOnRequest(string ImageUrl, string ProductName, string ProductType, string Shade, string HexColor);
public sealed record MakeupTryOnResult(string ProductName, string ProductType, string Shade, string HexColor, int MatchScore, string PreviewNote, IReadOnlyCollection<string> ApplicationTips);
public sealed record PayOsLinkRequest(Guid PlanId, string CancelUrl, string ReturnUrl);
public sealed record PayOsLinkResponse(string CheckoutUrl, long OrderCode, decimal Amount);
public sealed record PayOsWebhookRequest(string code, string desc, bool success, PayOsWebhookData data, string signature);
public sealed record PayOsWebhookData(
    long orderCode,
    int amount,
    string description,
    string accountNumber,
    string reference,
    string transactionDateTime,
    string paymentLinkId,
    string code,
    string desc,
    string counterAccountName,
    string counterAccountNumber,
    string counterAccountBankId,
    string counterAccountBankName);

// ─────────────────────────────────────────────────────────────────────
// Enhanced DTOs with Personalized Compatibility
// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Enhanced scan result that includes both safety and compatibility scores.
/// </summary>
public sealed record EnhancedIngredientScanResult(
    int SafetyScore,
    string Status,
    string Summary,
    IReadOnlyCollection<IngredientScanItem> Beneficial,
    IReadOnlyCollection<IngredientScanItem> Neutral,
    IReadOnlyCollection<IngredientScanItem> Harmful,
    IReadOnlyCollection<string> Recommendations,
    CompatibilityData? Compatibility
);

public sealed record CompatibilityData(
    int Score,
    string Status,
    IReadOnlyCollection<CompatibilityIngredientItem> Beneficial,
    IReadOnlyCollection<CompatibilityIngredientItem> Harmful,
    IReadOnlyCollection<CompatibilityIngredientItem> Neutral
);

public sealed record CompatibilityIngredientItem(
    string Name,
    string Reason,
    string PersonalReason
);

/// <summary>
/// Enhanced ingredient detail that includes personalized assessment.
/// </summary>
public sealed record EnhancedIngredientDto(
    Guid Id,
    string NameInc,
    string Name,
    string Category,
    string Description,
    string Links,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    PersonalizedAssessmentData? PersonalizedAssessment
);

public sealed record PersonalizedAssessmentData(
    string Status,
    IReadOnlyCollection<string> Reasons
);
