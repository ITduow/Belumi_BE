using Belumi.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoryController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await catalogService.GetCategoriesAsync(cancellationToken));
}
