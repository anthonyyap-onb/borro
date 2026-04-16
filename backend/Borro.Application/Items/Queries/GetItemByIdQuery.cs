using Borro.Application.Items.DTOs;
using MediatR;

namespace Borro.Application.Items.Queries;

/// <summary>Returns the item DTO or null if not found.</summary>
public sealed record GetItemByIdQuery(Guid ItemId) : IRequest<ItemDto?>;
