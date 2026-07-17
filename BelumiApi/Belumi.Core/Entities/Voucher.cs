using System;
using System.Collections.Generic;

namespace Belumi.Core.Entities;

public enum VoucherType
{
    SingleUse = 0,         // Sử dụng 1 lần duy nhất toàn hệ thống (bị sử dụng là hết hạn luôn)
    MultiUsePerUser = 1    // Mỗi tài khoản được sử dụng tối đa 1 lần
}

public enum DiscountType
{
    FixedAmount = 0,       // Trừ thẳng số tiền cố định (VND)
    Percentage = 1         // Giảm giá theo %
}

public sealed class Voucher : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public VoucherType Type { get; set; } = VoucherType.MultiUsePerUser;
    public decimal DiscountValue { get; set; }
    public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;
    public int? UsageLimit { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<VoucherUsage> Usages { get; set; } = new List<VoucherUsage>();
}
