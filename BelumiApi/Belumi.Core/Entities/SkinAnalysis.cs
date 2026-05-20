namespace Belumi.Core.Entities;

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
