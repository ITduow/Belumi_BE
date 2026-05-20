namespace Belumi.Core.Entities;

public sealed class ContactRequest : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Message { get; set; } = string.Empty;
    public ContactStatus Status { get; set; } = ContactStatus.New;
}
