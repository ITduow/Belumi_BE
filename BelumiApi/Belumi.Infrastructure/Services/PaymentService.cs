using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace Belumi.Infrastructure.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly BelumiDbContext _db;
    private readonly PayOSClient _payOSClient;

    public PaymentService(BelumiDbContext db, PayOSClient payOSClient)
    {
        _db = db;
        _payOSClient = payOSClient;
    }

    public async Task<IReadOnlyCollection<object>> GetPlansAsync(CancellationToken cancellationToken)
    {
        var dbPlans = await _db.SubscriptionPlans.AsNoTracking()
            .OrderBy(x => x.Price)
            .ToListAsync(cancellationToken);

        return dbPlans.Select(plan => new
        {
            id = plan.Id,
            code = plan.BillingCycle == "yearly" ? "yearly" : plan.Name.ToLower(),
            name = plan.Name == "Free" ? "Gói Miễn Phí" : plan.Name == "Monthly" ? "Gói Mỗi Tháng" : "Gói Mỗi Năm",
            price = plan.Price,
            billingCycle = plan.BillingCycle,
            features = plan.Name == "Free" ? new[]
            {
                "Kết quả tra cứu thành phần mỹ phẩm và phân tích da bị hạn chế",
                "Giới hạn lượt giải thích với AI về tình trạng da hiện tại",
                "Quy trình chăm sóc da cá nhân hóa theo loại da bị hạn chế"
            } : plan.Name == "Monthly" ? new[]
            {
                "Kết quả tra cứu thành phần mỹ phẩm và phân tích da đầy đủ",
                "Không giới hạn lượt giải thích với AI về tình trạng da hiện tại và so sánh các mỹ phẩm phù hợp với loại da",
                "Quy trình chăm sóc da cá nhân hóa theo loại da đầy đủ"
            } : new[]
            {
                "Chi phí mỗi tháng rẻ hơn",
                "Trải nghiệm miễn phí tính năng mới trang điểm ảo khi được ra mắt chính thức",
                "Kết quả tra cứu thành phần mỹ phẩm và phân tích da đầy đủ",
                "Không giới hạn lượt giải thích với AI về tình trạng da hiện tại và so sánh các mỹ phẩm phù hợp với loại da",
                "Quy trình chăm sóc da cá nhân hóa theo loại da đầy đủ"
            }
        }).Cast<object>().ToList();
    }

    public async Task<PayOsLinkResponse> CreatePayOsLinkAsync(
        Guid planId,
        Guid userId,
        string cancelUrl,
        string returnUrl,
        CancellationToken cancellationToken)
    {
        var plan = await _db.SubscriptionPlans.FindAsync([planId], cancellationToken);
        if (plan == null)
        {
            throw new ArgumentException("Gói đăng ký không tồn tại.");
        }

        var user = await _db.Users.FindAsync([userId], cancellationToken);
        if (user == null)
        {
            throw new ArgumentException("Người dùng không tồn tại.");
        }

        // Sinh mã đơn hàng duy nhất long (dưới giới hạn 9007199254740991 của Javascript)
        // Dùng timestamp giây nhân 10.000 + random 4 chữ số
        long orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 10000 + Random.Shared.Next(1000, 10000);

        var payment = new Payment
        {
            UserId = user.Id,
            PlanId = plan.Id,
            Amount = plan.Price,
            Currency = "VND",
            PaymentMethod = "PayOS",
            PaymentStatus = "Pending",
            TransactionCode = orderCode.ToString()
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        // Chuẩn bị request cho PayOS
        var planShortName = plan.Name == "Monthly" ? "Goi Thang" : plan.Name == "Yearly" ? "Goi Nam" : plan.Name;
        // Tên mô tả không dấu, không ký tự đặc biệt, max 25 ký tự
        var description = $"BELUMI {planShortName.ToUpperInvariant()}".Replace(" ", "");
        if (description.Length > 25)
        {
            description = description.Substring(0, 25);
        }

        var item = new PaymentLinkItem
        {
            Name = plan.Name == "Monthly" ? "Gói Mỗi Tháng" : plan.Name == "Yearly" ? "Gói Mỗi Năm" : plan.Name,
            Quantity = 1,
            Price = (int)plan.Price
        };

        var request = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = (int)plan.Price,
            Description = description,
            Items = new List<PaymentLinkItem> { item },
            CancelUrl = cancelUrl,
            ReturnUrl = returnUrl
        };

        var result = await _payOSClient.PaymentRequests.CreateAsync(request);

        return new PayOsLinkResponse(result.CheckoutUrl, orderCode, plan.Price);
    }

    public async Task<string> VerifyAndCheckStatusAsync(long orderCode, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.TransactionCode == orderCode.ToString(), cancellationToken);

        if (payment == null)
        {
            throw new ArgumentException("Giao dịch không tồn tại trên hệ thống Belumi.");
        }

        if (payment.PaymentStatus == "Paid")
        {
            return "Paid";
        }

        var payOsInfo = await _payOSClient.PaymentRequests.GetAsync(orderCode);
        if (payOsInfo == null)
        {
            throw new Exception("Không tìm thấy thông tin giao dịch trên PayOS.");
        }

        if (payOsInfo.Status == PaymentLinkStatus.Paid)
        {
            await ActivateUserSubscriptionAsync(payment, cancellationToken);
            return "Paid";
        }

        return payOsInfo.Status.ToString();
    }

    public async Task<bool> ProcessWebhookAsync(PayOsWebhookRequest webhookData, CancellationToken cancellationToken)
    {
        if (webhookData.data == null)
        {
            return true; // Test webhook ping từ PayOS dashboard
        }

        // Chuyển đổi DTO webhook của ta sang model của SDK để verify
        var sdkWebhookBody = new Webhook
        {
            Code = webhookData.code,
            Description = webhookData.desc,
            Success = webhookData.success,
            Data = new WebhookData
            {
                OrderCode = webhookData.data.orderCode,
                Amount = webhookData.data.amount,
                Description = webhookData.data.description,
                AccountNumber = webhookData.data.accountNumber,
                Reference = webhookData.data.reference,
                TransactionDateTime = webhookData.data.transactionDateTime,
                Currency = "VND",
                PaymentLinkId = webhookData.data.paymentLinkId,
                Code = webhookData.data.code,
                Description2 = webhookData.data.desc,
                CounterAccountName = webhookData.data.counterAccountName,
                CounterAccountNumber = webhookData.data.counterAccountNumber,
                CounterAccountBankId = webhookData.data.counterAccountBankId,
                CounterAccountBankName = webhookData.data.counterAccountBankName
            },
            Signature = webhookData.signature
        };

        // Verify webhook signature (no cancellationToken parameter supported)
        var verifiedData = await _payOSClient.Webhooks.VerifyAsync(sdkWebhookBody);
        if (verifiedData == null)
        {
            return false;
        }

        var payment = await _db.Payments
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.TransactionCode == verifiedData.OrderCode.ToString(), cancellationToken);

        if (payment == null || payment.PaymentStatus == "Paid")
        {
            return true; // Đã xử lý hoặc giao dịch không khớp
        }

        await ActivateUserSubscriptionAsync(payment, cancellationToken);
        return true;
    }

    private async Task ActivateUserSubscriptionAsync(Payment payment, CancellationToken cancellationToken)
    {
        payment.PaymentStatus = "Paid";

        var user = await _db.Users.FindAsync([payment.UserId], cancellationToken);
        if (user != null && payment.Plan != null)
        {
            user.SubscriptionPlan = payment.Plan.Name;

            var startDate = DateTime.UtcNow;
            var endDate = payment.Plan.BillingCycle == "yearly"
                ? startDate.AddYears(1)
                : startDate.AddMonths(1);

            _db.UserSubscriptions.Add(new UserSubscription
            {
                UserId = user.Id,
                PlanId = payment.Plan.Id,
                Status = "Active",
                StartDate = startDate,
                EndDate = endDate,
                PaymentStatus = "Paid"
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
