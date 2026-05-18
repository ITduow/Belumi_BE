using Belumi.Core.DTOs;

namespace Belumi.Application.Abstractions;

public interface IPaymentService
{
    IReadOnlyCollection<object> GetPlans();
    PaymentQrResponse CreateVietQr(PaymentQrRequest request);
}
