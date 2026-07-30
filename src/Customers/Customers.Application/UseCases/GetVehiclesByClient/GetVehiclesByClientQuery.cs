using Customers.Application.Abstractions;
using Customers.Application.DTOs;

namespace Customers.Application.UseCases.GetVehiclesByClient;

public sealed record GetVehiclesByClientQuery(Guid ClientId) : IQuery<IReadOnlyList<VehicleDto>>;
