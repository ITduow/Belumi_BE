using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/news")]
public sealed class NewsController(BelumiDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var query = db.NewsArticles.AsNoTracking()
            .Where(x => x.IsActive && x.Status == NewsStatus.Published);

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

        query = sort?.Trim().ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(x => x.PublishedAt),
            "popular" => query.OrderByDescending(x => x.ViewCount).ThenByDescending(x => x.PublishedAt),
            _ => query.OrderByDescending(x => x.PublishedAt)
        };

        var posts = await query.ToListAsync(cancellationToken);
        return Ok(await ToNewsResponsesAsync(posts, cancellationToken));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var post = await db.NewsArticles
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive && x.Status == NewsStatus.Published, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        post.ViewCount += 1;
        await db.SaveChangesAsync(cancellationToken);
        return Ok((await ToNewsResponsesAsync([post], cancellationToken)).Single());
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var managedCategories = await db.NewsCategories.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        if (managedCategories.Count > 0)
        {
            return Ok(managedCategories);
        }

        var categories = await db.NewsArticles.AsNoTracking()
            .Where(x => x.IsActive && x.Status == NewsStatus.Published)
            .Select(x => x.Category)
            .Where(x => x != "")
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("saved")]
    [Authorize]
    public async Task<IActionResult> GetSaved(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var posts = await db.NewsSaves.AsNoTracking()
            .Include(x => x.News)
            .Where(x => x.UserId == userId && x.News != null && x.News.IsActive && x.News.Status == NewsStatus.Published)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.News!)
            .ToListAsync(cancellationToken);

        return Ok(await ToNewsResponsesAsync(posts, cancellationToken));
    }

    [HttpPost("{id:guid}/toggle-like")]
    [Authorize]
    public async Task<IActionResult> ToggleLike(Guid id, CancellationToken cancellationToken)
    {
        var post = await FindPublishedPostAsync(id, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var like = await db.NewsLikes.FirstOrDefaultAsync(x => x.UserId == userId && x.NewsId == id, cancellationToken);
        var isLiked = like is null;
        if (like is null)
        {
            db.NewsLikes.Add(new NewsLike { UserId = userId, NewsId = id });
        }
        else
        {
            db.NewsLikes.Remove(like);
        }

        await db.SaveChangesAsync(cancellationToken);
        post.LikeCount = await db.NewsLikes.CountAsync(x => x.NewsId == id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ToggleLikeResponse(id, isLiked, post.LikeCount));
    }

    [HttpPost("{id:guid}/like")]
    [Authorize]
    public async Task<IActionResult> Like(Guid id, CancellationToken cancellationToken)
    {
        var post = await FindPublishedPostAsync(id, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        if (!await db.NewsLikes.AnyAsync(x => x.UserId == userId && x.NewsId == id, cancellationToken))
        {
            db.NewsLikes.Add(new NewsLike { UserId = userId, NewsId = id });
            await db.SaveChangesAsync(cancellationToken);
        }

        post.LikeCount = await db.NewsLikes.CountAsync(x => x.NewsId == id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ToggleLikeResponse(id, true, post.LikeCount));
    }

    [HttpDelete("{id:guid}/like")]
    [Authorize]
    public async Task<IActionResult> Unlike(Guid id, CancellationToken cancellationToken)
    {
        var post = await FindPublishedPostAsync(id, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var like = await db.NewsLikes.FirstOrDefaultAsync(x => x.UserId == userId && x.NewsId == id, cancellationToken);
        if (like is not null)
        {
            db.NewsLikes.Remove(like);
            await db.SaveChangesAsync(cancellationToken);
        }

        post.LikeCount = await db.NewsLikes.CountAsync(x => x.NewsId == id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ToggleLikeResponse(id, false, post.LikeCount));
    }

    [HttpGet("{id:guid}/like-status")]
    [Authorize]
    public async Task<IActionResult> LikeStatus(Guid id, CancellationToken cancellationToken)
    {
        if (!await db.NewsArticles.AnyAsync(x => x.Id == id && x.IsActive && x.Status == NewsStatus.Published, cancellationToken))
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var isLiked = await db.NewsLikes.AnyAsync(x => x.UserId == userId && x.NewsId == id, cancellationToken);
        var likeCount = await db.NewsLikes.CountAsync(x => x.NewsId == id, cancellationToken);
        return Ok(new ToggleLikeResponse(id, isLiked, likeCount));
    }

    [HttpPost("{id:guid}/toggle-save")]
    [Authorize]
    public async Task<IActionResult> ToggleSave(Guid id, CancellationToken cancellationToken)
    {
        if (!await db.NewsArticles.AnyAsync(x => x.Id == id && x.IsActive && x.Status == NewsStatus.Published, cancellationToken))
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var save = await db.NewsSaves.FirstOrDefaultAsync(x => x.UserId == userId && x.NewsId == id, cancellationToken);
        var isSaved = save is null;
        if (save is null)
        {
            db.NewsSaves.Add(new NewsSave { UserId = userId, NewsId = id });
        }
        else
        {
            db.NewsSaves.Remove(save);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ToggleSaveResponse(id, isSaved));
    }

    [HttpPost("{id:guid}/save")]
    [Authorize]
    public async Task<IActionResult> Save(Guid id, CancellationToken cancellationToken)
    {
        if (!await db.NewsArticles.AnyAsync(x => x.Id == id && x.IsActive && x.Status == NewsStatus.Published, cancellationToken))
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        if (!await db.NewsSaves.AnyAsync(x => x.UserId == userId && x.NewsId == id, cancellationToken))
        {
            db.NewsSaves.Add(new NewsSave { UserId = userId, NewsId = id });
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new ToggleSaveResponse(id, true));
    }

    [HttpDelete("{id:guid}/save")]
    [Authorize]
    public async Task<IActionResult> Unsave(Guid id, CancellationToken cancellationToken)
    {
        if (!await db.NewsArticles.AnyAsync(x => x.Id == id && x.IsActive && x.Status == NewsStatus.Published, cancellationToken))
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var save = await db.NewsSaves.FirstOrDefaultAsync(x => x.UserId == userId && x.NewsId == id, cancellationToken);
        if (save is not null)
        {
            db.NewsSaves.Remove(save);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new ToggleSaveResponse(id, false));
    }

    [HttpGet("{id:guid}/save-status")]
    [Authorize]
    public async Task<IActionResult> SaveStatus(Guid id, CancellationToken cancellationToken)
    {
        if (!await db.NewsArticles.AnyAsync(x => x.Id == id && x.IsActive && x.Status == NewsStatus.Published, cancellationToken))
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var isSaved = await db.NewsSaves.AnyAsync(x => x.UserId == userId && x.NewsId == id, cancellationToken);
        return Ok(new ToggleSaveResponse(id, isSaved));
    }

    private Task<NewsArticle?> FindPublishedPostAsync(Guid id, CancellationToken cancellationToken) =>
        db.NewsArticles.FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.Status == NewsStatus.Published, cancellationToken);

    private async Task<IReadOnlyCollection<NewsResponse>> ToNewsResponsesAsync(IReadOnlyCollection<NewsArticle> posts, CancellationToken cancellationToken)
    {
        if (posts.Count == 0)
        {
            return [];
        }

        var ids = posts.Select(x => x.Id).ToArray();
        var likeCounts = await db.NewsLikes.AsNoTracking()
            .Where(x => ids.Contains(x.NewsId))
            .GroupBy(x => x.NewsId)
            .Select(x => new { NewsId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.NewsId, x => x.Count, cancellationToken);

        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : Guid.Empty;
        var likedIds = new HashSet<Guid>();
        var savedIds = new HashSet<Guid>();
        if (userId != Guid.Empty)
        {
            likedIds = (await db.NewsLikes.AsNoTracking()
                .Where(x => x.UserId == userId && ids.Contains(x.NewsId))
                .Select(x => x.NewsId)
                .ToListAsync(cancellationToken)).ToHashSet();
            savedIds = (await db.NewsSaves.AsNoTracking()
                .Where(x => x.UserId == userId && ids.Contains(x.NewsId))
                .Select(x => x.NewsId)
                .ToListAsync(cancellationToken)).ToHashSet();
        }

        return posts.Select(post =>
        {
            var likeCount = likeCounts.GetValueOrDefault(post.Id, post.LikeCount);
            return new NewsResponse(
                post.Id,
                post.Title,
                post.Slug,
                post.Summary,
                post.Content,
                post.CoverImageUrl,
                post.Category,
                post.Tags,
                post.Author,
                post.Status,
                post.ViewCount,
                likeCount,
                post.PublishedAt,
                post.IsActive,
                likedIds.Contains(post.Id),
                savedIds.Contains(post.Id));
        }).ToArray();
    }
}

public sealed record NewsResponse(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    string Content,
    string? CoverImageUrl,
    string Category,
    string Tags,
    string Author,
    NewsStatus Status,
    int ViewCount,
    int LikeCount,
    DateTime PublishedAt,
    bool IsActive,
    bool IsLiked,
    bool IsSaved);

public sealed record ToggleLikeResponse(Guid NewsId, bool IsLiked, int LikeCount);
public sealed record ToggleSaveResponse(Guid NewsId, bool IsSaved);
