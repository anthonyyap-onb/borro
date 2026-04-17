using Borro.Application.Chat.DTOs;
using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Chat.Queries.GetMessages;

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, List<MessageDto>>
{
    private readonly IApplicationDbContext _db;
    public GetMessagesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<MessageDto>> Handle(GetMessagesQuery q, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Item)
            .FirstOrDefaultAsync(b => b.Id == q.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {q.BookingId} not found.");

        var isParticipant = booking.RenterId == q.RequestingUserId || booking.Item.LenderId == q.RequestingUserId;
        if (!isParticipant)
            throw new UnauthorizedAccessException("Access denied.");

        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Where(m => m.BookingId == q.BookingId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(ct);

        return messages.Select(m => new MessageDto(
            m.Id, m.BookingId, m.SenderId,
            $"{m.Sender.FirstName} {m.Sender.LastName}",
            m.Content, m.CreatedAtUtc)).ToList();
    }
}
