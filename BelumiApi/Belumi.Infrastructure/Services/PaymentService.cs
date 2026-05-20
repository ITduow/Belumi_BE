using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;

namespace Belumi.Infrastructure.Services;

public sealed class PaymentService : IPaymentService
{
    public IReadOnlyCollection<object> GetPlans() =>
    [
        new { code = "free", name = "Free", price = 0, features = new[] { "Basic ingredient lookup", "Basic skin analysis", "Virtual makeup preview", "Wishlist local save" } },
        new { code = "plus", name = "Premium", price = 99000, features = new[] { "Advanced ingredient scan", "Unlimited AI recommendations", "Personalized skincare routine", "No interruptive ads" } },
        new { code = "pro", name = "Annual", price = 199000, features = new[] { "Best value yearly access", "Advanced Gemini consultation", "Virtual try-on priority", "Priority product recommendations" } }
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
