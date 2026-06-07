namespace Belumi.Infrastructure.AI;

public sealed class OpenAiOptions
{
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4o-mini";
}
