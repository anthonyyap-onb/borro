using Borro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext used by Application layer handlers.
/// Defined in Application; implemented by BorroDbContext in Infrastructure.
/// </summary>
public interface IBorroDbContext
{
    DbSet<Item> Items { get; }
    DbSet<ItemBlockedDate> ItemBlockedDates { get; }
    DbSet<Wishlist> Wishlists { get; }
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
