using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/ingredients")]
public sealed class IngredientController(IAiBeautyService aiBeautyService) : ControllerBase
{
    [HttpPost("lookup")]
    public ActionResult<IngredientLookupResult> Lookup(IngredientLookupRequest request) =>
        Ok(aiBeautyService.LookupIngredients(request));
}
