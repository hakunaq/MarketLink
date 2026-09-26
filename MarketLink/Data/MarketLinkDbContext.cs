using MarketLink.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Data;

/// <summary>
/// The database context for MarketLink. It extends Identity's context so the
/// user/role tables and our own business tables live in the same database.
/// </summary>
public class MarketLinkDbContext : IdentityDbContext<ApplicationUser>
{
    public MarketLinkDbContext(DbContextOptions<MarketLinkDbContext> options)
        : base(options)
    {
    }

    public DbSet<FarmerProfile> FarmerProfiles => Set<FarmerProfile>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<FarmerMarket> FarmerMarkets => Set<FarmerMarket>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<PlatformReport> PlatformReports => Set<PlatformReport>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Always call base first so Identity's own mappings are registered.
        base.OnModelCreating(builder);

        // A user has at most one farmer profile.
        builder.Entity<FarmerProfile>()
            .HasOne(f => f.User)
            .WithOne(u => u.FarmerProfile)
            .HasForeignKey<FarmerProfile>(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Farmer <-> Market many-to-many through FarmerMarket.
        builder.Entity<FarmerMarket>()
            .HasOne(fm => fm.FarmerProfile)
            .WithMany(f => f.FarmerMarkets)
            .HasForeignKey(fm => fm.FarmerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FarmerMarket>()
            .HasOne(fm => fm.Market)
            .WithMany(m => m.FarmerMarkets)
            .HasForeignKey(fm => fm.MarketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Product>()
            .HasOne(p => p.Farmer)
            .WithMany(f => f.Products)
            .HasForeignKey(p => p.FarmerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order relationships.
        builder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Order>()
            .HasOne(o => o.Farmer)
            .WithMany(f => f.Orders)
            .HasForeignKey(o => o.FarmerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasOne(o => o.Market)
            .WithMany()
            .HasForeignKey(o => o.MarketId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Keep the product row even if an order item references a deleted product
        // is not desirable, so restrict deletes on OrderItem.Product.
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Review relationships. Customer owns the review (cascade), but the
        // product/farmer targets use Restrict so SQL Server does not see
        // multiple cascade paths (a product review would otherwise be reached
        // both via the farmer and via the product).
        builder.Entity<Review>()
            .HasOne(r => r.Customer)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Review>()
            .HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasOne(r => r.Farmer)
            .WithMany(f => f.Reviews)
            .HasForeignKey(r => r.FarmerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Favorite relationships. Same reasoning as reviews.
        builder.Entity<Favorite>()
            .HasOne(f => f.Customer)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Favorite>()
            .HasOne(f => f.FarmerProfile)
            .WithMany()
            .HasForeignKey(f => f.FarmerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Favorite>()
            .HasOne(f => f.Product)
            .WithMany()
            .HasForeignKey(f => f.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Store Status as a readable string instead of an int.
        builder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // A few indexes for the queries the app runs most often.
        builder.Entity<Product>().HasIndex(p => p.Name);
        builder.Entity<Order>().HasIndex(o => o.Status);
        builder.Entity<Favorite>().HasIndex(f => new { f.CustomerId, f.FarmerProfileId, f.ProductId });
    }
}
