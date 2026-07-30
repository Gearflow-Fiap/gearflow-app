using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;

namespace Workshop.Application.UseCases.GetServiceOrderDetails;

public sealed record GetServiceOrderDetailsQuery(Guid ServiceOrderId) : IQuery<ServiceOrderDetailsDto>;
