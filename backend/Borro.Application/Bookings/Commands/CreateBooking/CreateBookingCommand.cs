using Borro.Application.Bookings.DTOs;
using MediatR;

namespace Borro.Application.Bookings.Commands.CreateBooking;

public record CreateBookingCommand(
    Guid ItemId,
    Guid RenterId,
    DateTime StartDateUtc,
    DateTime EndDateUtc
) : IRequest<BookingDto>;
