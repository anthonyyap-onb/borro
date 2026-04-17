namespace Borro.Application.Chat.DTOs;

public record MessageDto(
    Guid Id,
    Guid BookingId,
    Guid SenderId,
    string SenderName,
    string Content,
    DateTime CreatedAtUtc
);
