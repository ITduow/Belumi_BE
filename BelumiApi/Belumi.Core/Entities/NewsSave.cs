namespace Belumi.Core.Entities;

public sealed class NewsSave : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid NewsId { get; set; }
    public NewsArticle? News { get; set; }
}
