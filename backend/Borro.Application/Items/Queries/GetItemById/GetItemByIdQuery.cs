using Borro.Application.Items.DTOs;
using MediatR;

namespace Borro.Application.Items.Queries.GetItemById;

public record GetItemByIdQuery(Guid ItemId) : IRequest<ItemDto?>;
