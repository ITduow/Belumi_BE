using Belumi.Core.DTOs;
using Belumi.Core.Entities;

namespace Belumi.Application.Abstractions;

public interface IAiBeautyService
{
    IngredientLookupResult LookupIngredients(IngredientLookupRequest request);
    IngredientScanResult AnalyzeIngredientLabel(IngredientScanRequest request);
    MakeupConsultationResult ConsultMakeup(MakeupConsultationRequest request);
    MakeupTryOnResult TryOnMakeup(MakeupTryOnRequest request);
    Task<IReadOnlyCollection<MakeupCatalogItem>> GetMakeupCatalogAsync(CancellationToken cancellationToken);
}
