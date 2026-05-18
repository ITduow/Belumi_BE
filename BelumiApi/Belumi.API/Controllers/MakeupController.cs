using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/makeup")]
public sealed class MakeupController(IAiBeautyService aiBeautyService) : ControllerBase
{
    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken) =>
        Ok(await aiBeautyService.GetMakeupCatalogAsync(cancellationToken));

    [HttpPost("consultation")]
    public ActionResult<MakeupConsultationResult> Consult(MakeupConsultationRequest request) =>
        Ok(aiBeautyService.ConsultMakeup(request));
}
