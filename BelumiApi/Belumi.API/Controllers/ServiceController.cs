using Belumi.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/services")]
public sealed class ServiceController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await catalogService.GetServicesAsync(cancellationToken));
}
