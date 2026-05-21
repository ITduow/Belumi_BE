using Belumi.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Belumi.Infrastructure.Data;

public sealed class BelumiDbContext(DbContextOptions<BelumiDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<ContactRequest> ContactRequests => Set<ContactRequest>();
    public DbSet<SkinAnalysis> SkinAnalyses => Set<SkinAnalysis>();
    public DbSet<IngredientLookup> IngredientLookups => Set<IngredientLookup>();
    public DbSet<MakeupConsultation> MakeupConsultations => Set<MakeupConsultation>();
    public DbSet<User> Users => Set<User>();
    public DbSet<BeautyProfile> BeautyProfiles => Set<BeautyProfile>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<MakeupCatalogItem> MakeupCatalogItems => Set<MakeupCatalogItem>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<AiUsageLog> AiUsageLogs => Set<AiUsageLog>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<Product>().Property(product => product.Price).HasPrecision(18, 2);
        modelBuilder.Entity<SubscriptionPlan>().Property(plan => plan.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(payment => payment.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Service>().Property(service => service.Price).HasPrecision(18, 2);
        modelBuilder.Entity<User>().Property(user => user.Role).HasConversion<string>();
        modelBuilder.Entity<ContactRequest>().Property(contact => contact.Status).HasConversion<string>();
        modelBuilder.Entity<WishlistItem>().HasIndex(item => new { item.UserId, item.ProductId }).IsUnique();
        modelBuilder.Entity<SubscriptionPlan>().HasIndex(plan => plan.Name).IsUnique();
        modelBuilder.Entity<BlogPost>().HasIndex(post => post.Slug).IsUnique();

        modelBuilder.Entity<BeautyProfile>()
            .HasOne(profile => profile.User)
            .WithOne(user => user.BeautyProfile)
            .HasForeignKey<BeautyProfile>(profile => profile.UserId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
