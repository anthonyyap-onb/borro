using Borro.Application.Chat.DTOs;
using MediatR;

namespace Borro.Application.Chat.Commands.SendMessage;

public record SendMessageCommand(
    Guid BookingId,
    Guid SenderId,
    string Content
) : IRequest<MessageDto>;
