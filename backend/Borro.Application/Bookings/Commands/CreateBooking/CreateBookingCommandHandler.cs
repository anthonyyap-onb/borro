using Borro.Application.Bookings.DTOs;
using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Bookings.Commands.CreateBooking;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingDto>
{
    private readonly IApplicationDbContext _db;

    public CreateBookingCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<BookingDto> Handle(CreateBookingCommand cmd, CancellationToken ct)
    {
        var item = await _db.Items
            .Include(i => i.Lender)
            .FirstOrDefaultAsync(i => i.Id == cmd.ItemId, ct)
            ?? throw new InvalidOperationException($"Item {cmd.ItemId} not found.");

        if (item.LenderId == cmd.RenterId)
            throw new InvalidOperationException("You cannot book your own item.");

        var renter = await _db.Users.FirstOrDefaultAsync(u => u.Id == cmd.RenterId, ct)
            ?? throw new InvalidOperationException($"User {cmd.RenterId} not found.");

        var days = Math.Max(1, (int)(cmd.EndDateUtc.Date - cmd.StartDateUtc.Date).TotalDays);
        var status = item.InstantBookEnabled ? BookingStatus.Approved : BookingStatus.PendingApproval;

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ItemId = cmd.ItemId,
            RenterId = cmd.RenterId,
            StartDateUtc = cmd.StartDateUtc,
            EndDateUtc = cmd.EndDateUtc,
            TotalPrice = item.DailyPrice * days,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);

        return ToDto(booking, item, renter);
    }

    internal static BookingDto ToDto(Booking b, Item item, User renter) => new(
        Id: b.Id,
        ItemId: b.ItemId,
        ItemTitle: item.Title,
        RenterId: b.RenterId,
        RenterName: $"{renter.FirstName} {renter.LastName}",
        LenderId: item.LenderId,
        LenderName: $"{item.Lender.FirstName} {item.Lender.LastName}",
        StartDateUtc: b.StartDateUtc,
        EndDateUtc: b.EndDateUtc,
        TotalPrice: b.TotalPrice,
        Status: b.Status,
        CreatedAtUtc: b.CreatedAtUtc
    );
}
