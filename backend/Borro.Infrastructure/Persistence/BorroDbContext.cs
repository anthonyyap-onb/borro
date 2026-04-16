using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Borro.Infrastructure.Persistence;

public class BorroDbContext : DbContext, IApplicationDbContext
{
    public BorroDbContext(DbContextOptions<BorroDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Item> Items => Set<Item>();
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
            entity.Property(i => i.DailyPrice).HasColumnType("numeric(18,2)");
            entity.Property(i => i.Category).IsRequired().HasMaxLength(100);

            // Map ItemAttributes as a JSONB column using EF Core 8+ owned entity JSON mapping
            entity.OwnsOne(i => i.Attributes, builder =>
            {
                builder.ToJson();

                var jsonOptions = (System.Text.Json.JsonSerializerOptions?)null;

                var comparer = new ValueComparer<Dictionary<string, object>>(
                    (c1, c2) => System.Text.Json.JsonSerializer.Serialize(c1, jsonOptions)
                                == System.Text.Json.JsonSerializer.Serialize(c2, jsonOptions),
                    c => System.Text.Json.JsonSerializer.Serialize(c, jsonOptions).GetHashCode(),
                    c => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                             System.Text.Json.JsonSerializer.Serialize(c, jsonOptions), jsonOptions)
                         ?? new Dictionary<string, object>()
                );

                builder.Property(a => a.Values)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, jsonOptions),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions)
                             ?? new Dictionary<string, object>()
                    )
                    .Metadata.SetValueComparer(comparer);
            });
        });
    }
}
