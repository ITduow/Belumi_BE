namespace Belumi.Core.Entities;

public sealed class BlogPost : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string Category { get; set; } = "Skincare";
    public string Author { get; set; } = "Belumi Team";
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
