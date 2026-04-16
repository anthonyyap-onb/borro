using Borro.Application.Items.DTOs;
using MediatR;

namespace Borro.Application.Items.Queries.GetMyItems;

public record GetMyItemsQuery(Guid LenderId) : IRequest<List<ItemDto>>;
