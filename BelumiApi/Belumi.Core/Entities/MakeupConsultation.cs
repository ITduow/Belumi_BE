namespace Belumi.Core.Entities;

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
