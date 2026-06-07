namespace Belumi.Infrastructure.AI;

public interface IInciApiClient
{
    Task<string?> GetIngredientAsync(string inciName, CancellationToken cancellationToken = default);

    Task<string?> SearchIngredientsAsync(string query, int limit = 5, CancellationToken cancellationToken = default);

    Task<string?> AnalyzeIngredientsAsync(IEnumerable<string> inciNames, CancellationToken cancellationToken = default);

    Task<string?> GetIngredientEfficacyAsync(string inciName, CancellationToken cancellationToken = default);

    Task<string?> GetIngredientSkinTypeProfilesAsync(string inciName, CancellationToken cancellationToken = default);

    Task<string?> GetIngredientIncompatibilitiesAsync(string inciName, CancellationToken cancellationToken = default);
}
