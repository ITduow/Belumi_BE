namespace Belumi.Core.Entities;

public sealed class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public int SortOrder { get; set; }
}
