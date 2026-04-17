using Borro.Application.Bookings.Commands.CreateBooking;
using Borro.Application.Bookings.DTOs;
using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Bookings.Queries.GetBooking;

public class GetBookingQueryHandler : IRequestHandler<GetBookingQuery, BookingDto>
{
    private readonly IApplicationDbContext _db;
    public GetBookingQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<BookingDto> Handle(GetBookingQuery q, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Item).ThenInclude(i => i.Lender)
            .Include(b => b.Renter)
            .FirstOrDefaultAsync(b => b.Id == q.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {q.BookingId} not found.");

        var isParticipant = booking.RenterId == q.RequestingUserId
                         || booking.Item.LenderId == q.RequestingUserId;
        if (!isParticipant)
            throw new UnauthorizedAccessException("Access denied.");

        return CreateBookingCommandHandler.ToDto(booking, booking.Item, booking.Renter);
    }
}
