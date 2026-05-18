using Belumi.Core.DTOs;
using Belumi.Core.Entities;

namespace Belumi.Application.Abstractions;

public interface ICatalogService
{
    Task<IReadOnlyCollection<Category>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(Guid? categoryId, CancellationToken cancellationToken);
    Task<ProductDto?> GetProductAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Service>> GetServicesAsync(CancellationToken cancellationToken);
}
