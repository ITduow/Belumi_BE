namespace Belumi.Infrastructure.AI;

public sealed class ChatToolCallTracker
{
    private readonly List<string> _tools = [];
    private readonly List<ChatToolSource> _sources = [];

    public IReadOnlyCollection<string> Tools => _tools.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyCollection<ChatToolSource> Sources => _sources
        .DistinctBy(x => $"{x.Type}:{x.Label}:{x.Url}")
        .ToArray();

    public void AddTool(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            _tools.Add(name);
        }
    }

    public void AddSource(string type, string label, string? url = null)
    {
        if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(label))
        {
            _sources.Add(new ChatToolSource(type, label, url));
        }
    }
}

public sealed record ChatToolSource(string Type, string Label, string? Url);
