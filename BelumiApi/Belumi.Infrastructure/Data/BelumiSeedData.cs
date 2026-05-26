using Belumi.Core.Entities;
using Belumi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Belumi.Infrastructure.Data;

public static class BelumiSeedData
{
    public static async Task SeedAsync(BelumiDbContext db)
    {
        await EnsureSubscriptionPlansAsync(db);
        await EnsureAdminAsync(db);
        await EnsureNewsCategoriesAsync(db);

        if (await db.Categories.AnyAsync())
        {
            await EnsureNewsAsync(db);
            await db.SaveChangesAsync();
            return;
        }

        var skincare = new Category { Name = "Skincare", IconUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03", SortOrder = 1 };
        var makeup = new Category { Name = "Makeup", IconUrl = "https://images.unsplash.com/photo-1596462502278-27bfdc403348", SortOrder = 2 };
        var spa = new Category { Name = "Spa Services", IconUrl = "https://images.unsplash.com/photo-1515377905703-c4788e51af15", SortOrder = 3 };

        var customer = new User
        {
            Email = "customer@belumi.vn",
            FullName = "Belumi Customer",
            Phone = "0911111111",
            PasswordHash = PasswordHasher.Hash("Customer@123"),
            Role = UserRole.Customer,
            BeautyProfile = new BeautyProfile
            {
                SkinType = "Combination",
                SkinConcerns = "Acne, dullness",
                Allergies = "None"
            }
        };

        var serum = new Product
        {
            Name = "Belumi Glow Serum",
            Description = "Lightweight vitamin serum for a bright, hydrated finish.",
            Ingredients = "Niacinamide, Vitamin C derivative, Hyaluronic Acid",
            Benefits = "Brightening, hydration, smoother skin texture",
            Price = 420000,
            ThumbnailUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be",
            Category = skincare
        };
        serum.Images.Add(new ProductImage { ImageUrl = serum.ThumbnailUrl!, SortOrder = 1 });

        var cream = new Product
        {
            Name = "Belumi Barrier Cream",
            Description = "Comforting moisturizer for daily barrier support.",
            Ingredients = "Ceramide NP, Panthenol, Squalane",
            Benefits = "Barrier repair, calming, moisture lock",
            Price = 360000,
            ThumbnailUrl = "https://images.unsplash.com/photo-1617897903246-719242758050",
            Category = skincare
        };

        db.AddRange(skincare, makeup, spa, customer, serum, cream);
        db.Services.AddRange(
            new Service
            {
                Name = "AI Skin Consultation",
                Description = "Skin scan review with personalized routine guidance.",
                Price = 250000,
                DurationMinutes = 30,
                ImageUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881",
                Category = spa
            },
            new Service
            {
                Name = "Hydra Facial",
                Description = "Deep hydration facial for fresh, bouncy skin.",
                Price = 650000,
                DurationMinutes = 60,
                ImageUrl = "https://images.unsplash.com/photo-1512290923902-8a9f81dc236c",
                Category = spa
            });

        db.BlogPosts.AddRange(
            new BlogPost
            {
            Title = "How to Build a Gentle Morning Routine",
                Slug = "gentle-morning-routine",
                Summary = "A simple AM routine for healthy, calm skin.",
                Content = "Cleanse lightly, hydrate well, protect with sunscreen, and keep actives simple.",
                CoverImageUrl = "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9",
                Category = "Skincare",
                Tags = "routine,skincare,beginner",
                Author = "Belumi Team",
                Status = NewsStatus.Published,
                ViewCount = 128,
                LikeCount = 18,
                PublishedAt = DateTime.UtcNow.AddDays(-3)
            },
            new BlogPost
            {
                Title = "Niacinamide: Small Ingredient, Big Range",
                Slug = "niacinamide-guide",
                Summary = "Why niacinamide is useful for oil, tone, and barrier care.",
                Content = "Niacinamide can support oil balance, tone, and skin barrier comfort.",
                CoverImageUrl = "https://images.unsplash.com/photo-1608248543803-ba4f8c70ae0b",
                Category = "Ingredient",
                Tags = "niacinamide,ingredient,barrier",
                Author = "Belumi Lab",
                Status = NewsStatus.Published,
                ViewCount = 96,
                LikeCount = 14,
                PublishedAt = DateTime.UtcNow.AddDays(-1)
            },
            new BlogPost
            {
                Title = "Makeup nền mỏng cho ngày nắng",
                Slug = "makeup-nen-mong-ngay-nang",
                Summary = "Cách giữ lớp nền nhẹ, bền màu và không bí da.",
                Content = "Ưu tiên dưỡng ẩm vừa đủ, kem chống nắng ráo mặt, cushion mỏng và phấn phủ vùng chữ T.",
                CoverImageUrl = "https://images.unsplash.com/photo-1596462502278-27bfdc403348",
                Category = "Makeup",
                Tags = "makeup,base,summer",
                Author = "Belumi Studio",
                Status = NewsStatus.Published,
                ViewCount = 74,
                LikeCount = 9,
                PublishedAt = DateTime.UtcNow.AddDays(-2)
            },
            new BlogPost
            {
                Title = "Đọc bảng thành phần trong 3 phút",
                Slug = "doc-bang-thanh-phan-3-phut",
                Summary = "Những nhóm thành phần nên nhận diện trước khi mua mỹ phẩm.",
                Content = "Hãy tìm nhóm cấp ẩm, phục hồi, hoạt chất chính và các thành phần dễ kích ứng như hương liệu hoặc cồn khô.",
                CoverImageUrl = "https://images.unsplash.com/photo-1571781926291-c477ebfd024b",
                Category = "Ingredient Knowledge",
                Tags = "ingredient,ocr,safety",
                Author = "Belumi Lab",
                Status = NewsStatus.Published,
                ViewCount = 112,
                LikeCount = 21,
                PublishedAt = DateTime.UtcNow.AddDays(-5)
            });

        db.Banners.AddRange(
            new Banner
            {
                Title = "Belumi Glow Week",
                ImageUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908",
                LinkUrl = "/products",
                SortOrder = 1
            },
            new Banner
            {
                Title = "Book Your Skin Scan",
                ImageUrl = "https://images.unsplash.com/photo-1596755389378-c31d21fd1273",
                LinkUrl = "/skin-analysis",
                SortOrder = 2
            });

        db.MakeupCatalogItems.AddRange(
            new MakeupCatalogItem
            {
                Name = "Cloud Tint Lip",
                ProductType = "Lipstick",
                Shade = "Rose Latte",
                HexColor = "#c86a7a",
                IsPro = false
            },
            new MakeupCatalogItem
            {
                Name = "Soft Focus Blush",
                ProductType = "Blush",
                Shade = "Petal Bloom",
                HexColor = "#e8a1aa",
                IsPro = true
            },
            new MakeupCatalogItem
            {
                Name = "Skin Veil Cushion",
                ProductType = "Foundation",
                Shade = "Neutral Light",
                HexColor = "#d7b097",
                IsPro = true
            });

        await db.SaveChangesAsync();
    }

    private static async Task EnsureAdminAsync(BelumiDbContext db)
    {
        if (await db.Users.AnyAsync(user => user.Email == "admin@belumi.com"))
        {
            return;
        }

        db.Users.Add(new User
        {
            Email = "admin@belumi.com",
            FullName = "Belumi Admin",
            Phone = "0900000000",
            PasswordHash = PasswordHasher.Hash("belumi2026"),
            Role = UserRole.Admin,
            SubscriptionPlan = "Pro",
            IsActive = true
        });
    }

    private static async Task EnsureSubscriptionPlansAsync(BelumiDbContext db)
    {
        if (await db.SubscriptionPlans.AnyAsync())
        {
            return;
        }

        db.SubscriptionPlans.AddRange(
            new SubscriptionPlan
            {
                Name = "Free",
                Price = 0,
                MonthlyAiLimit = 3,
                IngredientLookupLimit = 5,
                MakeupConsultationLimit = 2,
                CanUseAdvancedAnalysis = false
            },
            new SubscriptionPlan
            {
                Name = "Plus",
                Price = 99000,
                MonthlyAiLimit = 50,
                IngredientLookupLimit = 100,
                MakeupConsultationLimit = 50,
                CanUseAdvancedAnalysis = true
            },
            new SubscriptionPlan
            {
                Name = "Pro",
                Price = 199000,
                MonthlyAiLimit = 200,
                IngredientLookupLimit = 300,
                MakeupConsultationLimit = 200,
                CanUseAdvancedAnalysis = true
            });
    }

    private static async Task EnsureNewsAsync(BelumiDbContext db)
    {
        var seedPosts = new[]
        {
            new BlogPost
            {
                Title = "Xu hướng skin cycling cho người mới",
                Slug = "xu-huong-skin-cycling-cho-nguoi-moi",
                Summary = "Luân phiên phục hồi và treatment để da dễ thích nghi hơn.",
                Content = "Skin cycling thường gồm đêm tẩy da chết, đêm retinoid và các đêm phục hồi. Người mới nên bắt đầu chậm và theo dõi phản ứng da.",
                CoverImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03",
                Category = "Beauty Trend",
                Tags = "trend,skincare,retinoid",
                Author = "Belumi Team",
                ViewCount = 88,
                LikeCount = 12,
                PublishedAt = DateTime.UtcNow.AddDays(-6)
            },
            new BlogPost
            {
                Title = "Chọn serum cho da dầu mụn",
                Slug = "chon-serum-cho-da-dau-mun",
                Summary = "Các hoạt chất dịu nhẹ giúp hỗ trợ dầu thừa và lỗ chân lông.",
                Content = "Niacinamide, salicylic acid nồng độ phù hợp và panthenol là những lựa chọn thường gặp. Tránh dùng quá nhiều active cùng lúc.",
                CoverImageUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be",
                Category = "Product Review",
                Tags = "serum,oily,acne",
                Author = "Belumi Lab",
                ViewCount = 134,
                LikeCount = 25,
                PublishedAt = DateTime.UtcNow.AddDays(-4)
            },
            new BlogPost
            {
                Title = "Trang điểm công sở nhanh trong 10 phút",
                Slug = "trang-diem-cong-so-10-phut",
                Summary = "Một layout makeup gọn, sạch và dễ ứng dụng mỗi ngày.",
                Content = "Dùng nền mỏng, má kem nhẹ, chân mày tự nhiên và son tint MLBB để giữ vẻ tươi tắn mà không mất nhiều thời gian.",
                CoverImageUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796",
                Category = "Beauty Tips",
                Tags = "makeup,office,tips",
                Author = "Belumi Studio",
                ViewCount = 67,
                LikeCount = 8,
                PublishedAt = DateTime.UtcNow.AddDays(-8)
            }
        };

        foreach (var post in seedPosts)
        {
            if (!await db.BlogPosts.AnyAsync(x => x.Slug == post.Slug))
            {
                db.BlogPosts.Add(post);
            }
        }
    }

    private static async Task EnsureNewsCategoriesAsync(BelumiDbContext db)
    {
        var categories = new[]
        {
            new NewsCategory { Name = "Skincare", Slug = "skincare", Description = "Routine, treatment and skin health guidance." },
            new NewsCategory { Name = "Makeup", Slug = "makeup", Description = "Makeup looks, shades and application ideas." },
            new NewsCategory { Name = "Ingredient Knowledge", Slug = "ingredient-knowledge", Description = "Ingredient safety and benefit explainers." },
            new NewsCategory { Name = "Product Review", Slug = "product-review", Description = "Belumi product and cosmetic reviews." },
            new NewsCategory { Name = "Beauty Tips", Slug = "beauty-tips", Description = "Practical daily beauty tips." },
            new NewsCategory { Name = "Beauty Trend", Slug = "beauty-trend", Description = "New beauty trends and seasonal updates." }
        };

        foreach (var category in categories)
        {
            if (!await db.NewsCategories.AnyAsync(x => x.Slug == category.Slug))
            {
                db.NewsCategories.Add(category);
            }
        }
    }
}
