namespace Belumi.Core.Entities;

public sealed class BeautyProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? SkinType { get; set; }
    public string? SkinConcerns { get; set; }
    public string? Allergies { get; set; }
}
