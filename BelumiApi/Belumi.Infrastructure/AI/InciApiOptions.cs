namespace Belumi.Infrastructure.AI;

public sealed class InciApiOptions
{
    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://inciapi.com/v1";

    public int CacheDays { get; set; } = 30;
}
