using Borro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Item> Items { get; }
    DbSet<ItemBlockedDate> ItemBlockedDates { get; }
    DbSet<Wishlist> Wishlists { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
