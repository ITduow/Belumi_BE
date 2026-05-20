namespace Belumi.Core.Entities;

public sealed class UserSubscription : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string PaymentStatus { get; set; } = "MockPaid";
}
