namespace Belumi.Core.Entities;

public sealed class Ingredient : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? InciName { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SkinTypes { get; set; } = string.Empty;
    public string Benefits { get; set; } = string.Empty;
    public string? Concerns { get; set; }
    public string SafetyRating { get; set; } = "Safe";
}
