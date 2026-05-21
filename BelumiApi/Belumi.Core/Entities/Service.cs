namespace Belumi.Core.Entities;

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
