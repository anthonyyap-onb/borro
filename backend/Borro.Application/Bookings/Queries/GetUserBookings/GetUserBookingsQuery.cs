using Borro.Application.Bookings.DTOs;
using MediatR;

namespace Borro.Application.Bookings.Queries.GetUserBookings;

public record GetUserBookingsQuery(Guid UserId) : IRequest<List<BookingDto>>;
