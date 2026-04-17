using Borro.Application.Common.Interfaces;
using Borro.Application.Items.DTOs;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands.CreateItem;

public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, ItemDto>
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public CreateItemCommandHandler(IApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<ItemDto> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        var lender = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.LenderId, cancellationToken)
            ?? throw new InvalidOperationException($"User '{request.LenderId}' not found.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var item = new Item
        {
            Id = Guid.NewGuid(),
            LenderId = request.LenderId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DailyPrice = request.DailyPrice,
            Location = request.Location.Trim(),
            Category = request.Category.Trim(),
            Attributes = new ItemAttributes { Values = request.Attributes },
            InstantBookEnabled = request.InstantBookEnabled,
            DeliveryAvailable = request.DeliveryAvailable,
            HandoverOptions = request.HandoverOptions
                .Select(s => Enum.TryParse<HandoverOption>(s, out var v)
                    ? v
                    : throw new InvalidOperationException($"Invalid handover option: '{s}'."))
                .ToList(),
            ImageUrls = [],
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return item.ToDto(lender);
    }
}
