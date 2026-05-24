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

        var existing = await db.Products.FindAsync([id], cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = product.Name;
        existing.Brand = product.Brand;
        existing.Description = product.Description;
        existing.Ingredients = product.Ingredients;
        existing.Benefits = product.Benefits;
        existing.Price = product.Price;
        existing.ThumbnailUrl = product.ThumbnailUrl ?? existing.ThumbnailUrl;
        existing.ImageUrl = product.ImageUrl ?? existing.ImageUrl;
        existing.SuitableSkinTypes = product.SuitableSkinTypes ?? existing.SuitableSkinTypes;
        existing.CategoryId = product.CategoryId;
        existing.IsActive = product.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(existing);
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

    [HttpPost("news")]
    public async Task<IActionResult> CreateNews(BlogPost post, CancellationToken cancellationToken)
    {
        post.Slug = string.IsNullOrWhiteSpace(post.Slug) ? Slugify(post.Title) : Slugify(post.Slug);
        db.BlogPosts.Add(post);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/news/{post.Slug}", post);
    }

    [HttpPut("news/{id:guid}")]
    public async Task<IActionResult> UpdateNews(Guid id, BlogPost post, CancellationToken cancellationToken)
    {
        if (id != post.Id)
        {
            return BadRequest();
        }

        post.Slug = string.IsNullOrWhiteSpace(post.Slug) ? Slugify(post.Title) : Slugify(post.Slug);
        db.Entry(post).State = EntityState.Modified;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(post);
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
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string Slugify(string value) =>
        string.Join('-', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}

public sealed record UserStatusRequest(bool IsActive);
