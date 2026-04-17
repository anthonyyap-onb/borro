using Borro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Item> Items { get; }
    DbSet<Wishlist> Wishlists { get; }
    DbSet<BlockedDate> BlockedDates { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
