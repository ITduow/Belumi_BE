using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/news")]
public sealed class NewsController(BelumiDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? category, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = db.BlogPosts.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category.ToLower() == category.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(term) || x.Summary.ToLower().Contains(term));
        }

        return Ok(await query.OrderByDescending(x => x.PublishedAt).ToListAsync(cancellationToken));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var post = await db.BlogPosts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }
}
