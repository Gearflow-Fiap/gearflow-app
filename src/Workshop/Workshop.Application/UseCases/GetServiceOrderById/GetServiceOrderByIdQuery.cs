using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;

namespace Workshop.Application.UseCases.GetServiceOrderById;

public sealed record GetServiceOrderByIdQuery(Guid ServiceOrderId) : IQuery<ServiceOrderDto>;
