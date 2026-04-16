using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Borro.Infrastructure.Persistence;

public class BorroDbContext : DbContext, IApplicationDbContext
{
    public BorroDbContext(DbContextOptions<BorroDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemBlockedDate> ItemBlockedDates => Set<ItemBlockedDate>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Title).IsRequired().HasMaxLength(200);
            entity.Property(i => i.Description).IsRequired().HasMaxLength(2000);
            entity.Property(i => i.DailyPrice).HasColumnType("numeric(18,2)");
            entity.Property(i => i.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(i => i.Location).IsRequired().HasMaxLength(200);
            entity.Property(i => i.HandoverOptionsRaw).HasColumnName("handover_options").HasMaxLength(500);

            entity.HasOne(i => i.Owner)
                .WithMany()
                .HasForeignKey(i => i.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(i => i.ImageUrls).HasColumnType("text[]");
            entity.Ignore(i => i.HandoverOptions);

            entity.OwnsOne(i => i.Attributes, builder => builder.ToJson());
        });

        modelBuilder.Entity<ItemBlockedDate>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.DateUtc).IsRequired();
            entity.HasOne(d => d.Item)
                .WithMany(i => i.BlockedDates)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(d => new { d.ItemId, d.DateUtc });
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => new { w.UserId, w.ItemId }).IsUnique();
            entity.HasOne(w => w.User).WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(w => w.Item).WithMany().HasForeignKey(w => w.ItemId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
