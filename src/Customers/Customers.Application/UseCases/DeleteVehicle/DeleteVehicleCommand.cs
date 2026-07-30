using Customers.Application.Abstractions;

namespace Customers.Application.UseCases.DeleteVehicle;

public sealed record DeleteVehicleCommand(Guid VehicleId) : ICommand;
