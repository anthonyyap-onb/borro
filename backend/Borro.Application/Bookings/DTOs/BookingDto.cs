using Borro.Domain.Enums;

namespace Borro.Application.Bookings.DTOs;

public record BookingDto(
    Guid Id,
    Guid ItemId,
    string ItemTitle,
    Guid RenterId,
    string RenterName,
    Guid LenderId,
    string LenderName,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    decimal TotalPrice,
    BookingStatus Status,
    DateTime CreatedAtUtc
);
