using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminController(BelumiDbContext db) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken cancellationToken) =>
        Ok(await db.Users.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.Email, x.FullName, x.Role, x.SubscriptionPlan, x.IsActive, x.CreatedAt })
            .ToListAsync(cancellationToken));

    [HttpPut("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([id], cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(user);
    }

    [HttpGet("contacts")]
    public async Task<IActionResult> Contacts(CancellationToken cancellationToken) =>
        Ok(await db.ContactRequests.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken));

    [HttpPatch("contacts/{id:guid}/status")]
    public async Task<IActionResult> UpdateContactStatus(Guid id, [FromBody] ContactStatus status, CancellationToken cancellationToken)
    {
        var contact = await db.ContactRequests.FindAsync([id], cancellationToken);
        if (contact is null)
        {
            return NotFound();
        }

        contact.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(contact);
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(Product product, CancellationToken cancellationToken)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/products/{product.Id}", product);
    }

    [HttpPut("products/{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, Product product, CancellationToken cancellationToken)
    {
        if (id != product.Id)
        {
            return BadRequest();
        }

        db.Entry(product).State = EntityState.Modified;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(product);
    }

    [HttpDelete("products/{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        product.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("news")]
    public async Task<IActionResult> GetNews(
        [FromQuery] string? status,
        [FromQuery] string? category,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = db.BlogPosts.AsNoTracking().AsQueryable();

        if (Enum.TryParse<NewsStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category.ToLower() == category.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(term) ||
                x.Summary.ToLower().Contains(term) ||
                x.Content.ToLower().Contains(term));
        }

        return Ok(await query
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken));
    }

    [HttpPost("news")]
    public async Task<IActionResult> CreateNews(BlogPost post, CancellationToken cancellationToken)
    {
        post.Title = post.Title.Trim();
        post.Summary = post.Summary.Trim();
        post.Content = post.Content.Trim();
        post.Category = string.IsNullOrWhiteSpace(post.Category) ? "Skincare" : post.Category.Trim();
        post.Author = string.IsNullOrWhiteSpace(post.Author) ? "Belumi Team" : post.Author.Trim();
        post.Slug = string.IsNullOrWhiteSpace(post.Slug) ? Slugify(post.Title) : Slugify(post.Slug);
        post.IsActive = post.Status != NewsStatus.Hidden;
        db.BlogPosts.Add(post);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/news/{post.Slug}", post);
    }

    [HttpPut("news/{id:guid}")]
    public async Task<IActionResult> UpdateNews(Guid id, BlogPost post, CancellationToken cancellationToken)
    {
        var existing = await db.BlogPosts.FindAsync([id], cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Title = post.Title.Trim();
        existing.Slug = string.IsNullOrWhiteSpace(post.Slug) ? Slugify(post.Title) : Slugify(post.Slug);
        existing.Summary = post.Summary.Trim();
        existing.Content = post.Content.Trim();
        existing.CoverImageUrl = post.CoverImageUrl;
        existing.Category = post.Category.Trim();
        existing.Tags = post.Tags;
        existing.Author = string.IsNullOrWhiteSpace(post.Author) ? "Belumi Team" : post.Author.Trim();
        existing.Status = post.Status;
        existing.IsActive = post.Status != NewsStatus.Hidden;
        existing.PublishedAt = post.PublishedAt;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(existing);
    }

    [HttpDelete("news/{id:guid}")]
    public async Task<IActionResult> DeleteNews(Guid id, CancellationToken cancellationToken)
    {
        var post = await db.BlogPosts.FindAsync([id], cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        post.IsActive = false;
        post.Status = NewsStatus.Hidden;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("news/{id:guid}/status")]
    public async Task<IActionResult> UpdateNewsStatus(Guid id, [FromBody] NewsStatusRequest request, CancellationToken cancellationToken)
    {
        var post = await db.BlogPosts.FindAsync([id], cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        post.Status = request.Status;
        post.IsActive = request.Status != NewsStatus.Hidden;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(post);
    }

    [HttpGet("news/statistics")]
    public async Task<IActionResult> NewsStatistics(CancellationToken cancellationToken)
    {
        var posts = db.BlogPosts.AsNoTracking();
        var topPost = await posts
            .OrderByDescending(x => x.ViewCount)
            .Select(x => new { x.Id, x.Title, x.Slug, x.ViewCount })
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new
        {
            total = await posts.CountAsync(cancellationToken),
            published = await posts.CountAsync(x => x.Status == NewsStatus.Published && x.IsActive, cancellationToken),
            draft = await posts.CountAsync(x => x.Status == NewsStatus.Draft, cancellationToken),
            hidden = await posts.CountAsync(x => x.Status == NewsStatus.Hidden || !x.IsActive, cancellationToken),
            totalViews = await posts.SumAsync(x => x.ViewCount, cancellationToken),
            topPost
        });
    }

    [HttpGet("news-categories")]
    public async Task<IActionResult> GetNewsCategories(CancellationToken cancellationToken) =>
        Ok(await db.NewsCategories.AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken));

    [HttpPost("news-categories")]
    public async Task<IActionResult> CreateNewsCategory(NewsCategory category, CancellationToken cancellationToken)
    {
        category.Name = category.Name.Trim();
        category.Slug = string.IsNullOrWhiteSpace(category.Slug) ? Slugify(category.Name) : Slugify(category.Slug);
        db.NewsCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/news/categories/{category.Slug}", category);
    }

    [HttpPut("news-categories/{id:guid}")]
    public async Task<IActionResult> UpdateNewsCategory(Guid id, NewsCategory category, CancellationToken cancellationToken)
    {
        var existing = await db.NewsCategories.FindAsync([id], cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = category.Name.Trim();
        existing.Slug = string.IsNullOrWhiteSpace(category.Slug) ? Slugify(category.Name) : Slugify(category.Slug);
        existing.Description = category.Description;
        existing.IsActive = category.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(existing);
    }

    [HttpDelete("news-categories/{id:guid}")]
    public async Task<IActionResult> DeleteNewsCategory(Guid id, CancellationToken cancellationToken)
    {
        var category = await db.NewsCategories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        category.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string Slugify(string value) =>
        string.Join('-', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}

public sealed record UserStatusRequest(bool IsActive);
public sealed record NewsStatusRequest(NewsStatus Status);
