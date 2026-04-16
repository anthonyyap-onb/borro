using MediatR;

namespace Borro.Application.Items.BlockedDates.Commands.UnblockDates;

public record UnblockDatesCommand(
    Guid ItemId,
    Guid RequesterId,
    DateOnly[] Dates) : IRequest;
