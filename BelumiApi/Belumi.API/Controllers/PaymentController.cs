using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet("plans")]
    public IActionResult Plans() => Ok(paymentService.GetPlans());

    [HttpPost("vietqr")]
    public ActionResult<PaymentQrResponse> CreateQr(PaymentQrRequest request) =>
        Ok(paymentService.CreateVietQr(request));
}
