using Borro.Application.Items.DTOs;
using MediatR;

namespace Borro.Application.Items.Queries.GetItem;

public record GetItemQuery(Guid ItemId) : IRequest<ItemDto>;
