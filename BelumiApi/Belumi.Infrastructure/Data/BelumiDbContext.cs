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
    public DbSet<User> Users => Set<User>();
    public DbSet<BeautyProfile> BeautyProfiles => Set<BeautyProfile>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<MakeupCatalogItem> MakeupCatalogItems => Set<MakeupCatalogItem>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<Product>().Property(product => product.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Service>().Property(service => service.Price).HasPrecision(18, 2);
        modelBuilder.Entity<User>().Property(user => user.Role).HasConversion<string>();
        modelBuilder.Entity<ContactRequest>().Property(contact => contact.Status).HasConversion<string>();
        modelBuilder.Entity<WishlistItem>().HasIndex(item => new { item.UserId, item.ProductId }).IsUnique();

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
