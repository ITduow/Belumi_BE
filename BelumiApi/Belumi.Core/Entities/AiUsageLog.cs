namespace Belumi.Core.Entities;

public sealed class AiUsageLog : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string FeatureName { get; set; } = string.Empty;
    public int TokenUsed { get; set; }
    public string RequestData { get; set; } = string.Empty;
    public string ResponseData { get; set; } = string.Empty;
}
