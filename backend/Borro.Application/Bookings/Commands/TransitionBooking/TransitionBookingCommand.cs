using Borro.Application.Bookings.DTOs;
using Borro.Domain.Enums;
using MediatR;

namespace Borro.Application.Bookings.Commands.TransitionBooking;

public record TransitionBookingCommand(
    Guid BookingId,
    Guid RequestingUserId,
    BookingStatus TargetStatus
) : IRequest<BookingDto>;
