using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;

namespace Workshop.Application.UseCases.GetServiceOrders;

public sealed record GetServiceOrdersQuery : IQuery<IReadOnlyList<ServiceOrderDto>>;
