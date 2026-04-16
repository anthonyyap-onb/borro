using MediatR;

namespace Borro.Application.Items.Commands;

/// <summary>Returns true if deleted, false if item was not found.</summary>
public sealed record DeleteItemCommand(Guid ItemId, Guid RequestingUserId) : IRequest<bool>;
