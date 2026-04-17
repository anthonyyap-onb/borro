using Borro.Application.Bookings.Commands.CreateBooking;
using Borro.Application.Bookings.DTOs;
using Borro.Application.Common.Interfaces;
using Borro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Bookings.Commands.TransitionBooking;

public class TransitionBookingCommandHandler : IRequestHandler<TransitionBookingCommand, BookingDto>
{
    // Maps (currentStatus, targetStatus) → set of roles allowed: "lender" | "renter"
    // A transition that both parties can trigger uses both roles in the set.
    private static readonly Dictionary<(BookingStatus From, BookingStatus To), HashSet<string>> AllowedTransitions = new()
    {
        { (BookingStatus.PendingApproval, BookingStatus.Approved),    ["lender"] },
        { (BookingStatus.PendingApproval, BookingStatus.Cancelled),   ["lender"] },
        { (BookingStatus.Approved,        BookingStatus.PaymentHeld), ["renter"] },
        { (BookingStatus.Approved,        BookingStatus.Cancelled),   ["lender"] },
        { (BookingStatus.PaymentHeld,     BookingStatus.Active),      ["renter"] },  // after pickup photo checklist
        { (BookingStatus.Active,          BookingStatus.Completed),   ["renter"] },  // after dropoff photo checklist
        { (BookingStatus.Active,          BookingStatus.Disputed),    ["lender", "renter"] },
        { (BookingStatus.PaymentHeld,     BookingStatus.Disputed),    ["lender", "renter"] },
        { (BookingStatus.Completed,       BookingStatus.Disputed),    ["lender", "renter"] },
    };

    private readonly IApplicationDbContext _db;

    public TransitionBookingCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<BookingDto> Handle(TransitionBookingCommand cmd, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Item).ThenInclude(i => i.Lender)
            .Include(b => b.Renter)
            .FirstOrDefaultAsync(b => b.Id == cmd.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {cmd.BookingId} not found.");

        var key = (booking.Status, cmd.TargetStatus);
        if (!AllowedTransitions.TryGetValue(key, out var allowedRoles))
            throw new InvalidOperationException(
                $"Transition from {booking.Status} to {cmd.TargetStatus} is not allowed.");

        var isLender = booking.Item.LenderId == cmd.RequestingUserId;
        var isRenter = booking.RenterId == cmd.RequestingUserId;

        var authorized = (isLender && allowedRoles.Contains("lender"))
                      || (isRenter && allowedRoles.Contains("renter"));

        if (!authorized)
            throw new UnauthorizedAccessException(
                $"You are not authorized to perform this transition.");

        booking.Status = cmd.TargetStatus;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return CreateBookingCommandHandler.ToDto(booking, booking.Item, booking.Renter);
    }
}
