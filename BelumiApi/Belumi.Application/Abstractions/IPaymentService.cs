using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Belumi.Core.DTOs;

namespace Belumi.Application.Abstractions;

public interface IPaymentService
{
    Task<IReadOnlyCollection<object>> GetPlansAsync(CancellationToken cancellationToken);
    Task<PayOsLinkResponse> CreatePayOsLinkAsync(Guid planId, Guid userId, string cancelUrl, string returnUrl, CancellationToken cancellationToken);
    Task<string> VerifyAndCheckStatusAsync(long orderCode, CancellationToken cancellationToken);
    Task<bool> ProcessWebhookAsync(PayOsWebhookRequest webhookData, CancellationToken cancellationToken);
}
