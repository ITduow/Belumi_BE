namespace Belumi.Core.Entities;

public sealed class Payment : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string PaymentMethod { get; set; } = "Mock";
    public string PaymentStatus { get; set; } = "Pending";
    public string TransactionCode { get; set; } = string.Empty;
}
