using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Tests.Infrastructure;

/// <summary>
/// Lightweight in-memory DbContext for unit tests.
/// Skips PostgreSQL-specific features (JSONB) that are incompatible with the InMemory provider.
/// </summary>
public class TestDbContext : DbContext, IApplicationDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<BlockedDate> BlockedDates => Set<BlockedDate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Title).IsRequired().HasMaxLength(200);
            entity.Property(i => i.DailyPrice).HasColumnType("decimal(18,2)");
            entity.HasOne(i => i.Lender)
                  .WithMany()
                  .HasForeignKey(i => i.LenderId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Skip ToJson() — InMemory provider does not support PostgreSQL JSONB
            entity.Ignore(i => i.Attributes);

            entity.Property(i => i.ImageUrls)
                  .HasConversion(
                      v => string.Join(',', v),
                      v => v.Length == 0 ? Array.Empty<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries));
        });

        modelBuilder.Entity<BlockedDate>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.HasOne(b => b.Item)
                  .WithMany()
                  .HasForeignKey(b => b.ItemId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(b => new { b.ItemId, b.Date }).IsUnique();
        });
    }
}
