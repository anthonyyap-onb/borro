using Borro.Application.Bookings.Commands.CreateBooking;
using Borro.Application.Bookings.DTOs;
using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Bookings.Queries.GetUserBookings;

public class GetUserBookingsQueryHandler : IRequestHandler<GetUserBookingsQuery, List<BookingDto>>
{
    private readonly IApplicationDbContext _db;
    public GetUserBookingsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<BookingDto>> Handle(GetUserBookingsQuery q, CancellationToken ct)
    {
        var bookings = await _db.Bookings
            .Include(b => b.Item).ThenInclude(i => i.Lender)
            .Include(b => b.Renter)
            .Where(b => b.RenterId == q.UserId || b.Item.LenderId == q.UserId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(ct);

        return bookings.Select(b => CreateBookingCommandHandler.ToDto(b, b.Item, b.Renter)).ToList();
    }
}
