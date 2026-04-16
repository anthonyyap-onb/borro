using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Borro.Infrastructure.Persistence;

public class BorroDbContext : DbContext, IBorroDbContext
{
    public BorroDbContext(DbContextOptions<BorroDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemBlockedDate> ItemBlockedDates => Set<ItemBlockedDate>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureItem(modelBuilder);
        ConfigureItemBlockedDate(modelBuilder);
        ConfigureWishlist(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        });
    }

    private static void ConfigureItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.OwnerId).IsRequired();
            entity.Property(i => i.Title).IsRequired().HasMaxLength(200);
            entity.Property(i => i.Description).HasMaxLength(2000);
            entity.Property(i => i.DailyPrice).HasColumnType("numeric(18,2)");
            entity.Property(i => i.Location).IsRequired().HasMaxLength(300);

            // Category stored as int.
            entity.Property(i => i.Category)
                  .HasConversion<int>();

            // HandoverOptions stored as PostgreSQL integer[].
            // Npgsql natively maps List<HandoverOption> to integer[] via its built-in
            // enum-array support — no custom HasConversion needed.
            entity.Property(i => i.HandoverOptions)
                  .HasColumnType("integer[]");

            // ImageUrls stored as PostgreSQL text[].
            entity.Property(i => i.ImageUrls)
                  .HasColumnType("text[]");

            // Attributes mapped as JSONB via EF Core owned entity JSON mapping.
            entity.OwnsOne(i => i.Attributes, a =>
            {
                a.ToJson();
            });

            // BlockedDates collection navigation backed by private field.
            entity.HasMany(i => i.BlockedDates)
                  .WithOne(b => b.Item)
                  .HasForeignKey(b => b.ItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(i => i.BlockedDates)
                  .UsePropertyAccessMode(PropertyAccessMode.Field)
                  .HasField("_blockedDates");
        });
    }

    private static void ConfigureItemBlockedDate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemBlockedDate>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.DateUtc)
                  .HasColumnType("timestamp with time zone")
                  .IsRequired();

            entity.Property(b => b.CreatedAtUtc)
                  .HasColumnType("timestamp with time zone")
                  .IsRequired();

            // Composite index for efficient availability queries.
            entity.HasIndex(b => new { b.ItemId, b.DateUtc });
        });
    }

    private static void ConfigureWishlist(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(w => w.Id);

            entity.HasOne(w => w.User)
                  .WithMany()
                  .HasForeignKey(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(w => w.Item)
                  .WithMany()
                  .HasForeignKey(w => w.ItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            // A user can wishlist an item at most once.
            entity.HasIndex(w => new { w.UserId, w.ItemId }).IsUnique();
        });
    }
}
