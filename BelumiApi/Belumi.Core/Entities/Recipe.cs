namespace Belumi.Core.Entities;

public sealed class Recipe : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Steps { get; set; } = string.Empty;
    public string SkinType { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Easy";
    public int DurationMinutes { get; set; }
    public List<Ingredient> Ingredients { get; set; } = [];
}
