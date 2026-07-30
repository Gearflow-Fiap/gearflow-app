using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.UpdateServiceOrderVehicle;

public sealed record UpdateServiceOrderVehicleCommand(Guid ServiceOrderId, Guid VehicleId) : ICommand;
