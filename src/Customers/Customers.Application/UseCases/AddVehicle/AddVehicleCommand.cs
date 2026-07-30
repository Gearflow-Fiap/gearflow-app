using Customers.Application.Abstractions;
using Customers.Application.DTOs;

namespace Customers.Application.UseCases.AddVehicle;

public sealed record AddVehicleCommand(
    Guid ClientId,
    string LicensePlate,
    string Mark,
    string Model,
    string Color,
    int YearFabrication,
    int YearModel) : ICommand<VehicleDto>;
