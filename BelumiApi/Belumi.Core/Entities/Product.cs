namespace Belumi.Core.Entities;

public sealed class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = "Belumi";
    public string Description { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Benefits { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? SuitableSkinTypes { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ProductImage> Images { get; set; } = [];
}
