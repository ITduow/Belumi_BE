using System;
using System.Threading;
using System.Threading.Tasks;
using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PayOsPaymentController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken) =>
        Ok(await paymentService.GetPlansAsync(cancellationToken));

    [HttpPost("payos-link")]
    [Authorize]
    public async Task<ActionResult<PayOsLinkResponse>> CreatePayOsLink(
        [FromBody] PayOsLinkRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        try
        {
            var response = await paymentService.CreatePayOsLinkAsync(
                request.PlanId,
                userId,
                request.CancelUrl,
                request.ReturnUrl,
                cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("status/{orderCode:long}")]
    [Authorize]
    public async Task<IActionResult> CheckStatus(long orderCode, CancellationToken cancellationToken)
    {
        try
        {
            var status = await paymentService.VerifyAndCheckStatusAsync(orderCode, cancellationToken);
            return Ok(new { status });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi kiểm tra giao dịch: " + ex.Message });
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] PayOsWebhookRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var success = await paymentService.ProcessWebhookAsync(request, cancellationToken);
            if (success)
            {
                return Ok(new { message = "OK" });
            }
            return BadRequest(new { message = "Chữ ký webhook không hợp lệ." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi xử lý webhook: " + ex.Message });
        }
    }
}
