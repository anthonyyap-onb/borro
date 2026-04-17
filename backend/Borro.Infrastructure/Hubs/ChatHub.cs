using Borro.Application.Chat.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Borro.Infrastructure.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;

    public ChatHub(IMediator mediator) => _mediator = mediator;

    /// <summary>Called by client when they open a booking's chat.</summary>
    public async Task JoinBookingGroup(string bookingId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"booking-{bookingId}");
    }

    /// <summary>Client calls this to send a message. Server persists it and broadcasts to the group.</summary>
    public async Task SendMessage(string bookingId, string content)
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var senderId))
            throw new HubException("Unauthorized.");

        if (!Guid.TryParse(bookingId, out var bookingGuid))
            throw new HubException("Invalid bookingId.");

        try
        {
            var dto = await _mediator.Send(new SendMessageCommand(bookingGuid, senderId, content));
            await Clients.Group($"booking-{bookingId}").SendAsync("ReceiveMessage", dto);
        }
        catch (Exception ex)
        {
            throw new HubException(ex.Message);
        }
    }
}
