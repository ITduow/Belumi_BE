namespace Belumi.Core.Entities;

public sealed class NewsLike : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid NewsId { get; set; }
    public BlogPost? News { get; set; }
}
