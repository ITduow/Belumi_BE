using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;

namespace Belumi.Infrastructure.Services;

public sealed class PaymentService : IPaymentService
{
    public IReadOnlyCollection<object> GetPlans() =>
    [
        new { code = "free", name = "Free", price = 0, features = new[] { "Skin AI mock", "News", "Wishlist basic" } },
        new { code = "plus", name = "Plus", price = 99000, features = new[] { "Ingredient OCR lookup", "More AI scans", "Wishlist sync" } },
        new { code = "pro", name = "Pro", price = 199000, features = new[] { "Virtual makeup", "Advanced Gemini consultation", "Priority recommendations" } }
    ];

    public PaymentQrResponse CreateVietQr(PaymentQrRequest request)
    {
        var amount = request.PlanCode.Equals("pro", StringComparison.OrdinalIgnoreCase) ? 199000m : 99000m;
        var description = $"BELUMI {request.PlanCode.ToUpperInvariant()} {request.CustomerEmail}";
        var url = "https://img.vietqr.io/image/BIDV-1234567890-compact2.png"
            + $"?amount={amount:0}&addInfo={Uri.EscapeDataString(description)}&accountName=BELUMI%20BEAUTY";

        return new PaymentQrResponse(request.PlanCode, amount, "BIDV", "1234567890", description, url);
    }
}
