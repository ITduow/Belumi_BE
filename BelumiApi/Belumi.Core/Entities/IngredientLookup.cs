namespace Belumi.Core.Entities;

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
