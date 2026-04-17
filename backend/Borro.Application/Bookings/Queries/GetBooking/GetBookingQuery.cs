using Borro.Application.Bookings.DTOs;
using MediatR;

namespace Borro.Application.Bookings.Queries.GetBooking;

public record GetBookingQuery(Guid BookingId, Guid RequestingUserId) : IRequest<BookingDto>;
