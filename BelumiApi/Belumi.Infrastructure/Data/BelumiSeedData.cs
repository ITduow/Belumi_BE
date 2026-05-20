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

        db.BlogPosts.AddRange(
            new BlogPost
            {
            Title = "How to Build a Gentle Morning Routine",
                Slug = "gentle-morning-routine",
                Summary = "A simple AM routine for healthy, calm skin.",
                Content = "Cleanse lightly, hydrate well, protect with sunscreen, and keep actives simple.",
                CoverImageUrl = "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9",
                Category = "Skincare",
                Author = "Belumi Team",
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
                Author = "Belumi Lab",
                PublishedAt = DateTime.UtcNow.AddDays(-1)
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
}
