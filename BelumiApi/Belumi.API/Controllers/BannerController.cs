using Belumi.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/banners")]
public sealed class BannerController(IContentService contentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await contentService.GetBannersAsync(cancellationToken));
}
