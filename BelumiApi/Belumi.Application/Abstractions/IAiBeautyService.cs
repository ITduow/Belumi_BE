using Belumi.Core.DTOs;
using Belumi.Core.Entities;

namespace Belumi.Application.Abstractions;

public interface IAiBeautyService
{
    IngredientLookupResult LookupIngredients(IngredientLookupRequest request);
    MakeupConsultationResult ConsultMakeup(MakeupConsultationRequest request);
    Task<IReadOnlyCollection<MakeupCatalogItem>> GetMakeupCatalogAsync(CancellationToken cancellationToken);
}
