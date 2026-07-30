using Customers.Application.Abstractions;
using Customers.Application.DTOs;

namespace Customers.Application.UseCases.UpdateVehicle;

public sealed record UpdateVehicleCommand(
    Guid VehicleId, string LicensePlate, string Mark, string Model, string Color, int YearFabrication, int YearModel)
    : ICommand<VehicleDto>;
