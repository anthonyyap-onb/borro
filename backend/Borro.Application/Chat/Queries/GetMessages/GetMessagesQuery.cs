using Borro.Application.Chat.DTOs;
using MediatR;

namespace Borro.Application.Chat.Queries.GetMessages;

public record GetMessagesQuery(Guid BookingId, Guid RequestingUserId) : IRequest<List<MessageDto>>;
