namespace Belumi.Core.Entities;

public sealed class MakeupCatalogItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = "Belumi";
    public string ProductType { get; set; } = string.Empty;
    public string Shade { get; set; } = string.Empty;
    public string HexColor { get; set; } = "#5ba4d2";
    public string? ImageUrl { get; set; }
    public bool IsPro { get; set; }
}
