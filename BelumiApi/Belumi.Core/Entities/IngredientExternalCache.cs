namespace Belumi.Core.Entities;

public sealed class IngredientExternalCache : BaseEntity
{
    public string Query { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string RawJson { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime LastFetchedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
}
