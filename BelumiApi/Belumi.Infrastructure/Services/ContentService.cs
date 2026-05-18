using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Belumi.Infrastructure.Services;

public sealed class ContentService(BelumiDbContext db) : IContentService
{
    public async Task<IReadOnlyCollection<BlogPost>> GetBlogsAsync(CancellationToken cancellationToken) =>
        await db.BlogPosts.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.PublishedAt).ToListAsync(cancellationToken);

    public Task<BlogPost?> GetBlogAsync(Guid id, CancellationToken cancellationToken) =>
        db.BlogPosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<Banner>> GetBannersAsync(CancellationToken cancellationToken) =>
        await db.Banners.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);

    public async Task<ContactRequest> CreateContactAsync(ContactRequestDto request, CancellationToken cancellationToken)
    {
        var contact = new ContactRequest
        {
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email,
            Message = request.Message.Trim()
        };
        db.ContactRequests.Add(contact);
        await db.SaveChangesAsync(cancellationToken);
        return contact;
    }
}
