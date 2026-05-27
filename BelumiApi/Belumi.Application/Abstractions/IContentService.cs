using Belumi.Core.DTOs;
using Belumi.Core.Entities;

namespace Belumi.Application.Abstractions;

public interface IContentService
{
    Task<IReadOnlyCollection<Banner>> GetBannersAsync(CancellationToken cancellationToken);
    Task<ContactRequest> CreateContactAsync(ContactRequestDto request, CancellationToken cancellationToken);
}
