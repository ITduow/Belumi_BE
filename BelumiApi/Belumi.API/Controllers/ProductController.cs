using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProductDto>>> Get([FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.GetProductsAsync(categoryId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await catalogService.GetProductAsync(id, cancellationToken);

        return product is null ? NotFound() : Ok(product);
    }
}
