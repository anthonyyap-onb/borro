using Borro.Application.Chat.DTOs;
using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Chat.Commands.SendMessage;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IApplicationDbContext _db;
    public SendMessageCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<MessageDto> Handle(SendMessageCommand cmd, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Item)
            .FirstOrDefaultAsync(b => b.Id == cmd.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {cmd.BookingId} not found.");

        var isParticipant = booking.RenterId == cmd.SenderId || booking.Item.LenderId == cmd.SenderId;
        if (!isParticipant)
            throw new UnauthorizedAccessException("Only booking participants can send messages.");

        var sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == cmd.SenderId, ct)
            ?? throw new InvalidOperationException($"User {cmd.SenderId} not found.");

        var message = new Message
        {
            Id = Guid.NewGuid(),
            BookingId = cmd.BookingId,
            SenderId = cmd.SenderId,
            Content = cmd.Content,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync(ct);

        return new MessageDto(message.Id, message.BookingId, message.SenderId,
            $"{sender.FirstName} {sender.LastName}", message.Content, message.CreatedAtUtc);
    }
}
