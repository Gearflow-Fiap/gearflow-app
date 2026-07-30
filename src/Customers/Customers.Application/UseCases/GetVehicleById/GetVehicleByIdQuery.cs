using Customers.Application.Abstractions;
using Customers.Application.DTOs;

namespace Customers.Application.UseCases.GetVehicleById;

public sealed record GetVehicleByIdQuery(Guid VehicleId) : IQuery<VehicleDto>;
