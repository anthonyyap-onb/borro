using MediatR;

namespace Borro.Application.Items.BlockedDates.Commands.BlockDates;

public record BlockDatesCommand(
    Guid ItemId,
    Guid RequesterId,
    DateOnly[] Dates) : IRequest;
