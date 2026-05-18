namespace Belumi.Core.Entities;

public sealed class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public int SortOrder { get; set; }
}

public sealed class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Benefits { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ProductImage> Images { get; set; } = [];
}

public sealed class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class Service : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
}

public sealed class WishlistItem : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
}

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
