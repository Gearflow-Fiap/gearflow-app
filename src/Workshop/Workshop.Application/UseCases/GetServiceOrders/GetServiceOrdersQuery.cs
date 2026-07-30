using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;

namespace Workshop.Application.UseCases.GetServiceOrders;

public sealed record GetServiceOrdersQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<ServiceOrderDto>>;
