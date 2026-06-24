using Belumi.Core.Entities;
using Belumi.Core.DTOs;
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
    [HttpGet("dashboard/analytics")]
    public async Task<IActionResult> GetDashboardAnalytics([FromQuery] string period = "daily", CancellationToken cancellationToken = default)
    {
        period = period.ToLowerInvariant();
        
        DateTime now = DateTime.UtcNow;
        DateTime currentStart;
        DateTime prevStart;
        DateTime prevEnd;
        
        if (period == "yearly")
        {
            currentStart = new DateTime(now.Year - 4, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            prevStart = new DateTime(now.Year - 9, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            prevEnd = currentStart.AddTicks(-1);
        }
        else if (period == "monthly")
        {
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            currentStart = startOfThisMonth.AddMonths(-11);
            prevStart = currentStart.AddMonths(-12);
            prevEnd = currentStart.AddTicks(-1);
        }
        else
        {
            var startOfToday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            currentStart = startOfToday.AddDays(-29);
            prevStart = currentStart.AddDays(-30);
            prevEnd = currentStart.AddTicks(-1);
        }
        
        var currentPayments = await db.Payments
            .Where(x => x.CreatedAt >= currentStart && (x.PaymentStatus == "Paid" || x.PaymentStatus == "MockPaid"))
            .Select(x => new { x.Amount, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var prevPayments = await db.Payments
            .Where(x => x.CreatedAt >= prevStart && x.CreatedAt <= prevEnd && (x.PaymentStatus == "Paid" || x.PaymentStatus == "MockPaid"))
            .Select(x => new { x.Amount, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var currentUsers = await db.Users
            .Where(x => x.CreatedAt >= currentStart)
            .Select(x => new { x.SubscriptionPlan, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var prevUsersCount = await db.Users
            .CountAsync(x => x.CreatedAt >= prevStart && x.CreatedAt <= prevEnd, cancellationToken);

        var currentSkinAnalyses = await db.SkinAnalyses
            .Where(x => x.CreatedAt >= currentStart)
            .Select(x => new { x.SkinType, x.Score, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var prevSkinAnalysesCount = await db.SkinAnalyses
            .CountAsync(x => x.CreatedAt >= prevStart && x.CreatedAt <= prevEnd, cancellationToken);

        var currentLookups = await db.IngredientLookups
            .Where(x => x.CreatedAt >= currentStart)
            .Select(x => new { x.InputText, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var prevLookupsCount = await db.IngredientLookups
            .CountAsync(x => x.CreatedAt >= prevStart && x.CreatedAt <= prevEnd, cancellationToken);

        var currentConsultations = await db.MakeupConsultations
            .Where(x => x.CreatedAt >= currentStart)
            .Select(x => new { x.CreatedAt })
            .ToListAsync(cancellationToken);

        var prevConsultationsCount = await db.MakeupConsultations
            .CountAsync(x => x.CreatedAt >= prevStart && x.CreatedAt <= prevEnd, cancellationToken);

        var currentArticles = await db.NewsArticles
            .Where(x => x.CreatedAt >= currentStart)
            .Select(x => new { x.CreatedAt })
            .ToListAsync(cancellationToken);

        int totalArticles = await db.NewsArticles.CountAsync(cancellationToken);

        decimal currentRevenue = currentPayments.Sum(x => x.Amount);
        decimal prevRevenue = prevPayments.Sum(x => x.Amount);
        double revenueGrowth = prevRevenue == 0 ? (currentRevenue > 0 ? 100 : 0) : (double)((currentRevenue - prevRevenue) / prevRevenue) * 100;

        int currentUsersCount = currentUsers.Count;
        double userGrowth = prevUsersCount == 0 ? (currentUsersCount > 0 ? 100 : 0) : (double)(currentUsersCount - prevUsersCount) / prevUsersCount * 100;

        int currentScansCount = currentSkinAnalyses.Count + currentLookups.Count + currentConsultations.Count;
        int prevScansCount = prevSkinAnalysesCount + prevLookupsCount + prevConsultationsCount;
        double scanGrowth = prevScansCount == 0 ? (currentScansCount > 0 ? 100 : 0) : (double)(currentScansCount - prevScansCount) / prevScansCount * 100;

        int totalUsers = await db.Users.CountAsync(cancellationToken);
        int premiumUsers = await db.Users.CountAsync(x => x.SubscriptionPlan != "Free", cancellationToken);
        double conversionRate = totalUsers == 0 ? 0 : (double)premiumUsers / totalUsers * 100;

        var overview = new TotalOverviewDto
        {
            TotalRevenue = currentRevenue,
            RevenueGrowthPercent = Math.Round(revenueGrowth, 1),
            NewUsers = currentUsersCount,
            UserGrowthPercent = Math.Round(userGrowth, 1),
            TotalScans = currentScansCount,
            ScanGrowthPercent = Math.Round(scanGrowth, 1),
            ConversionRate = Math.Round(conversionRate, 1),
            PremiumUsersCount = premiumUsers,
            TotalUsers = totalUsers,
            PremiumPurchases = currentPayments.Count,
            TotalArticles = totalArticles,
            NewArticles = currentArticles.Count
        };

        var timeSeries = new List<TimeSeriesPointDto>();

        if (period == "yearly")
        {
            for (int i = 4; i >= 0; i--)
            {
                int year = now.Year - i;
                string label = year.ToString();
                
                decimal rev = currentPayments.Where(x => x.CreatedAt.Year == year).Sum(x => x.Amount);
                int u = currentUsers.Where(x => x.CreatedAt.Year == year).Count();
                int s = currentSkinAnalyses.Where(x => x.CreatedAt.Year == year).Count() +
                        currentLookups.Where(x => x.CreatedAt.Year == year).Count() +
                        currentConsultations.Where(x => x.CreatedAt.Year == year).Count();
                int p = currentPayments.Where(x => x.CreatedAt.Year == year).Count();
                int art = currentArticles.Where(x => x.CreatedAt.Year == year).Count();
                
                timeSeries.Add(new TimeSeriesPointDto 
                { 
                    Label = label, 
                    Revenue = rev, 
                    NewUsers = u, 
                    Scans = s,
                    PremiumPurchases = p,
                    NewArticles = art
                });
            }
        }
        else if (period == "monthly")
        {
            for (int i = 11; i >= 0; i--)
            {
                DateTime targetMonth = now.AddMonths(-i);
                string label = targetMonth.ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
                
                decimal rev = currentPayments.Where(x => x.CreatedAt.Year == targetMonth.Year && x.CreatedAt.Month == targetMonth.Month).Sum(x => x.Amount);
                int u = currentUsers.Where(x => x.CreatedAt.Year == targetMonth.Year && x.CreatedAt.Month == targetMonth.Month).Count();
                int s = currentSkinAnalyses.Where(x => x.CreatedAt.Year == targetMonth.Year && x.CreatedAt.Month == targetMonth.Month).Count() +
                        currentLookups.Where(x => x.CreatedAt.Year == targetMonth.Year && x.CreatedAt.Month == targetMonth.Month).Count() +
                        currentConsultations.Where(x => x.CreatedAt.Year == targetMonth.Year && x.CreatedAt.Month == targetMonth.Month).Count();
                int p = currentPayments.Where(x => x.CreatedAt.Year == targetMonth.Year && x.CreatedAt.Month == targetMonth.Month).Count();
                int art = currentArticles.Where(x => x.CreatedAt.Year == targetMonth.Year && x.CreatedAt.Month == targetMonth.Month).Count();
                
                timeSeries.Add(new TimeSeriesPointDto 
                { 
                    Label = label, 
                    Revenue = rev, 
                    NewUsers = u, 
                    Scans = s,
                    PremiumPurchases = p,
                    NewArticles = art
                });
            }
        }
        else
        {
            for (int i = 29; i >= 0; i--)
            {
                DateTime targetDate = now.AddDays(-i);
                string label = targetDate.ToString("dd/MM");
                
                decimal rev = currentPayments.Where(x => x.CreatedAt.Date == targetDate.Date).Sum(x => x.Amount);
                int u = currentUsers.Where(x => x.CreatedAt.Date == targetDate.Date).Count();
                int s = currentSkinAnalyses.Where(x => x.CreatedAt.Date == targetDate.Date).Count() +
                        currentLookups.Where(x => x.CreatedAt.Date == targetDate.Date).Count() +
                        currentConsultations.Where(x => x.CreatedAt.Date == targetDate.Date).Count();
                int p = currentPayments.Where(x => x.CreatedAt.Date == targetDate.Date).Count();
                int art = currentArticles.Where(x => x.CreatedAt.Date == targetDate.Date).Count();
                
                timeSeries.Add(new TimeSeriesPointDto 
                { 
                    Label = label, 
                    Revenue = rev, 
                    NewUsers = u, 
                    Scans = s,
                    PremiumPurchases = p,
                    NewArticles = art
                });
            }
        }

        var plans = await db.Users
            .GroupBy(x => x.SubscriptionPlan)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        
        int totalUsersForPercentage = plans.Sum(x => x.Count);
        var subPlansDto = plans.Select(x => new PieItemDto
        {
            Name = string.IsNullOrWhiteSpace(x.Name) ? "Free" : x.Name,
            Count = x.Count,
            Percentage = totalUsersForPercentage == 0 ? 0 : Math.Round((double)x.Count / totalUsersForPercentage * 100, 1)
        }).ToList();

        var skinTypes = await db.SkinAnalyses
            .Where(x => !string.IsNullOrEmpty(x.SkinType))
            .GroupBy(x => x.SkinType)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        
        int totalSkinTypes = skinTypes.Sum(x => x.Count);
        var skinTypesDto = skinTypes.Select(x => new PieItemDto
        {
            Name = x.Name,
            Count = x.Count,
            Percentage = totalSkinTypes == 0 ? 0 : Math.Round((double)x.Count / totalSkinTypes * 100, 1)
        }).ToList();

        var topIngredients = await db.IngredientLookups
            .Where(x => !string.IsNullOrEmpty(x.InputText))
            .GroupBy(x => x.InputText)
            .Select(g => new BarItemDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentPaymentsRaw = await db.Payments
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new { x.PaymentStatus, x.TransactionCode, x.Amount, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var recentPayments = recentPaymentsRaw.Select(x => new RecentActivityDto
        {
            Type = "payment",
            Title = "Giao dịch " + (x.PaymentStatus == "Paid" || x.PaymentStatus == "MockPaid" ? "thành công" : "chờ xử lý"),
            Subtitle = x.TransactionCode + " - " + x.Amount.ToString("N0") + " VND",
            Timestamp = x.CreatedAt
        }).ToList();

        var recentSignupsRaw = await db.Users
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new { x.FullName, x.Email, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var recentSignups = recentSignupsRaw.Select(x => new RecentActivityDto
        {
            Type = "signup",
            Title = "Đăng ký thành viên mới",
            Subtitle = x.FullName + " (" + x.Email + ")",
            Timestamp = x.CreatedAt
        }).ToList();

        var recentScansRaw = await db.SkinAnalyses
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new { x.SkinType, x.Score, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var recentScans = recentScansRaw.Select(x => new RecentActivityDto
        {
            Type = "scan",
            Title = "Phân tích da mới",
            Subtitle = "Loại da: " + x.SkinType + " - Điểm số: " + x.Score,
            Timestamp = x.CreatedAt
        }).ToList();

        var recentActivities = recentPayments
            .Concat(recentSignups)
            .Concat(recentScans)
            .OrderByDescending(x => x.Timestamp)
            .Take(8)
            .ToList();

        var result = new AdminAnalyticsDto
        {
            Overview = overview,
            TimeSeries = timeSeries,
            Distributions = new DistributionDto
            {
                SubscriptionPlans = subPlansDto,
                SkinTypes = skinTypesDto,
                TopIngredients = topIngredients
            },
            RecentActivities = recentActivities
        };

        return Ok(result);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments(CancellationToken cancellationToken) =>
        Ok(await db.Payments.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new 
            { 
                x.Id, 
                x.UserId, 
                UserEmail = x.User != null ? x.User.Email : "Ẩn danh", 
                UserFullName = x.User != null ? x.User.FullName : "Ẩn danh",
                PlanName = x.Plan != null ? x.Plan.Name : "Chưa rõ", 
                x.Amount, 
                x.Currency, 
                x.PaymentMethod, 
                x.PaymentStatus, 
                x.TransactionCode, 
                x.CreatedAt 
            })
            .ToListAsync(cancellationToken));

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
        var query = db.NewsArticles.AsNoTracking().AsQueryable();

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
    public async Task<IActionResult> CreateNews(NewsArticle post, CancellationToken cancellationToken)
    {
        post.Title = post.Title.Trim();
        post.Summary = post.Summary.Trim();
        post.Content = post.Content.Trim();
        post.Category = string.IsNullOrWhiteSpace(post.Category) ? "Skincare" : post.Category.Trim();
        post.Author = string.IsNullOrWhiteSpace(post.Author) ? "Belumi Team" : post.Author.Trim();
        post.Slug = string.IsNullOrWhiteSpace(post.Slug) ? Slugify(post.Title) : Slugify(post.Slug);
        post.IsActive = post.Status != NewsStatus.Hidden;
        db.NewsArticles.Add(post);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/news/{post.Slug}", post);
    }

    [HttpPut("news/{id:guid}")]
    public async Task<IActionResult> UpdateNews(Guid id, NewsArticle post, CancellationToken cancellationToken)
    {
        var existing = await db.NewsArticles.FindAsync([id], cancellationToken);
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
        var post = await db.NewsArticles.FindAsync([id], cancellationToken);
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
        var post = await db.NewsArticles.FindAsync([id], cancellationToken);
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
        var posts = db.NewsArticles.AsNoTracking();
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

    [HttpPost("ingredients/import-csv")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportIngredientsCsv(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("CSV file is required.");
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var rows = ReadCsv(await reader.ReadToEndAsync(cancellationToken));
        if (rows.Count == 0)
        {
            return BadRequest("CSV file is empty.");
        }

        var headers = rows[0]
            .Select((header, index) => new { Header = NormalizeHeader(header), Index = index })
            .GroupBy(x => x.Header)
            .ToDictionary(x => x.Key, x => x.First().Index);
        var requiredHeaders = new[] { "name_inc", "name", "category", "description", "links" };
        var missingHeaders = requiredHeaders.Where(header => !headers.ContainsKey(header)).ToList();
        if (missingHeaders.Count > 0)
        {
            return BadRequest(new { message = "CSV is missing required columns.", missingHeaders });
        }

        var existingByNameInc = await db.Ingredients
            .ToDictionaryAsync(x => x.NameInc.ToLower(), cancellationToken);

        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in rows.Skip(1))
        {
            var value = CsvRow(headers, row);
            var nameInc = value("name_inc");
            if (string.IsNullOrWhiteSpace(nameInc))
            {
                skipped++;
                continue;
            }

            var key = nameInc.ToLower();
            if (existingByNameInc.TryGetValue(key, out var ingredient))
            {
                updated++;
            }
            else
            {
                ingredient = new Ingredient();
                existingByNameInc[key] = ingredient;
                db.Ingredients.Add(ingredient);
                created++;
            }

            ingredient.NameInc = nameInc;
            ingredient.Name = value("name");
            ingredient.Category = value("category");
            ingredient.Description = value("description");
            ingredient.Links = value("links");
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            totalRows = rows.Count - 1,
            created,
            updated,
            skipped
        });
    }

    private static string Slugify(string value) =>
        string.Join('-', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizeHeader(string value) => value.Trim().Trim('\ufeff').ToLowerInvariant();

    private static Func<string, string> CsvRow(IReadOnlyDictionary<string, int> headers, IReadOnlyList<string> row) =>
        header =>
        {
            return headers.TryGetValue(header, out var index) && index < row.Count ? row[index].Trim() : string.Empty;
        };

    private static List<List<string>> ReadCsv(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var value = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var current = csv[i];
            if (current == '"')
            {
                if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (current == ',' && !inQuotes)
            {
                row.Add(value.ToString());
                value.Clear();
                continue;
            }

            if ((current == '\n' || current == '\r') && !inQuotes)
            {
                if (current == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                {
                    i++;
                }
                row.Add(value.ToString());
                value.Clear();
                if (row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
                {
                    rows.Add(row);
                }
                row = [];
                continue;
            }

            value.Append(current);
        }

        row.Add(value.ToString());
        if (row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
        {
            rows.Add(row);
        }

        return rows;
    }
}

public sealed record UserStatusRequest(bool IsActive);
public sealed record NewsStatusRequest(NewsStatus Status);
