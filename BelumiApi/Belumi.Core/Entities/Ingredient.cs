namespace Belumi.Core.Entities;

public sealed class Ingredient : BaseEntity
{
    public string NameInc { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Links { get; set; } = string.Empty;
}
