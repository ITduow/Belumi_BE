using System;

namespace Belumi.Core.Entities;

public sealed class VoucherUsage : BaseEntity
{
    public Guid VoucherId { get; set; }
    public Voucher? Voucher { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}
