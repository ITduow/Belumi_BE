using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Belumi.Infrastructure.AI;

public sealed class InciApiClient : IInciApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InciApiClient> _logger;
    private readonly InciApiOptions _options;

    public InciApiClient(
        HttpClient httpClient,
        IOptions<InciApiOptions> options,
        ILogger<InciApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public Task<string?> GetIngredientAsync(string inciName, CancellationToken cancellationToken = default) =>
        GetAsync($"ingredients/{Uri.EscapeDataString(inciName)}", cancellationToken);

    public Task<string?> SearchIngredientsAsync(string query, int limit = 5, CancellationToken cancellationToken = default) =>
        GetAsync($"ingredients/search?q={Uri.EscapeDataString(query)}&limit={Math.Clamp(limit, 1, 20)}", cancellationToken);

    public async Task<string?> AnalyzeIngredientsAsync(IEnumerable<string> inciNames, CancellationToken cancellationToken = default)
    {
        if (!EnsureConfigured()) return null;

        var response = await _httpClient.PostAsJsonAsync(
            "analyze",
            new { inci = inciNames.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray() },
            cancellationToken);

        return await ReadResponseAsync(response, "POST /analyze", cancellationToken);
    }

    public Task<string?> GetIngredientEfficacyAsync(string inciName, CancellationToken cancellationToken = default) =>
        GetAsync($"ingredients/{Uri.EscapeDataString(inciName)}/efficacy", cancellationToken);

    public Task<string?> GetIngredientSkinTypeProfilesAsync(string inciName, CancellationToken cancellationToken = default) =>
        GetAsync($"ingredients/{Uri.EscapeDataString(inciName)}/skin-type-profiles", cancellationToken);

    public Task<string?> GetIngredientIncompatibilitiesAsync(string inciName, CancellationToken cancellationToken = default) =>
        GetAsync($"ingredients/{Uri.EscapeDataString(inciName)}/incompatibilities", cancellationToken);

    private async Task<string?> GetAsync(string path, CancellationToken cancellationToken)
    {
        if (!EnsureConfigured()) return null;

        var response = await _httpClient.GetAsync(path, cancellationToken);
        return await ReadResponseAsync(response, $"GET /{path}", cancellationToken);
    }

    private bool EnsureConfigured()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey)) return true;

        _logger.LogWarning("INCI API key is not configured; skipping external ingredient lookup.");
        return false;
    }

    private async Task<string?> ReadResponseAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("INCI API operation {Operation} is not available for this tier.", operation);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("INCI API operation {Operation} failed with {StatusCode}: {Error}",
                operation,
                (int)response.StatusCode,
                error);
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
