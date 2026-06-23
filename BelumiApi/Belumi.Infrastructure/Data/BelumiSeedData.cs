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
            SubscriptionPlan = "Yearly",
            IsActive = true
        });
    }

    private static async Task EnsureSubscriptionPlansAsync(BelumiDbContext db)
    {
        var plans = await db.SubscriptionPlans.ToListAsync();

        if (!plans.Any())
        {
            db.SubscriptionPlans.AddRange(
                new SubscriptionPlan
                {
                    Name = "Free",
                    BillingCycle = "monthly",
                    Price = 0,
                    MonthlyAiLimit = 3,
                    IngredientLookupLimit = 5,
                    MakeupConsultationLimit = 2,
                    CanUseAdvancedAnalysis = false
                },
                new SubscriptionPlan
                {
                    Name = "Monthly",
                    BillingCycle = "monthly",
                    Price = 59000,
                    MonthlyAiLimit = 9999,
                    IngredientLookupLimit = 9999,
                    MakeupConsultationLimit = 9999,
                    CanUseAdvancedAnalysis = true
                },
                new SubscriptionPlan
                {
                    Name = "Yearly",
                    BillingCycle = "yearly",
                    Price = 599000,
                    MonthlyAiLimit = 9999,
                    IngredientLookupLimit = 9999,
                    MakeupConsultationLimit = 9999,
                    CanUseAdvancedAnalysis = true
                });
            await db.SaveChangesAsync();
            return;
        }

        // Cập nhật thông tin các gói hiện có để tránh lỗi Khóa ngoại (Foreign Key)
        var freePlan = plans.FirstOrDefault(p => p.Price == 0 || p.Name.Contains("Free") || p.Name.Contains("Miễn Phí"));
        if (freePlan != null)
        {
            freePlan.Name = "Free";
            freePlan.BillingCycle = "monthly";
            freePlan.Price = 0;
            freePlan.MonthlyAiLimit = 3;
            freePlan.IngredientLookupLimit = 5;
            freePlan.MakeupConsultationLimit = 2;
            freePlan.CanUseAdvancedAnalysis = false;
        }

        var monthlyPlan = plans.FirstOrDefault(p => p.Price == 99000 || p.Name.Contains("Monthly") || p.Name.Contains("Tháng") || p.Name.Contains("Premium") || p.Name.Contains("plus"));
        if (monthlyPlan != null)
        {
            monthlyPlan.Name = "Monthly";
            monthlyPlan.BillingCycle = "monthly";
            monthlyPlan.Price = 59000;
            monthlyPlan.MonthlyAiLimit = 9999;
            monthlyPlan.IngredientLookupLimit = 9999;
            monthlyPlan.MakeupConsultationLimit = 9999;
            monthlyPlan.CanUseAdvancedAnalysis = true;
        }

        var yearlyPlan = plans.FirstOrDefault(p => p.Price == 199000 || p.Name.Contains("Yearly") || p.Name.Contains("Năm") || p.Name.Contains("Annual") || p.Name.Contains("pro"));
        if (yearlyPlan != null)
        {
            yearlyPlan.Name = "Yearly";
            yearlyPlan.BillingCycle = "yearly";
            yearlyPlan.Price = 599000;
            yearlyPlan.MonthlyAiLimit = 9999;
            yearlyPlan.IngredientLookupLimit = 9999;
            yearlyPlan.MakeupConsultationLimit = 9999;
            yearlyPlan.CanUseAdvancedAnalysis = true;
        }

        await db.SaveChangesAsync();
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
