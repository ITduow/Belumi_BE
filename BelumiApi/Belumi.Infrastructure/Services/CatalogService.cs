using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Belumi.Infrastructure.Services;

public sealed class CatalogService(BelumiDbContext db) : ICatalogService
{
    public async Task<IReadOnlyCollection<Category>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await db.Categories.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking().Include(x => x.Category).Include(x => x.Images).Where(x => x.IsActive);
        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var products = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return products.Select(ToDto).ToArray();
    }

    public async Task<ProductDto?> GetProductAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images.OrderBy(image => image.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        return product is null ? null : ToDto(product);
    }

    public async Task<IReadOnlyCollection<Service>> GetServicesAsync(CancellationToken cancellationToken) =>
        await db.Services.AsNoTracking().Include(x => x.Category).OrderBy(x => x.Name).ToListAsync(cancellationToken);

    private static ProductDto ToDto(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Description,
            product.Ingredients,
            product.Benefits,
            product.Price,
            product.ThumbnailUrl,
            product.CategoryId,
            product.Category?.Name,
            product.Images.OrderBy(x => x.SortOrder).Select(x => x.ImageUrl).ToArray());
}
