using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;

namespace Workshop.Application.UseCases.CreateServiceOrder;

public sealed record RequestedPartInput(Guid PartId, int Quantity);

public sealed record CreateServiceOrderCommand(
    Guid VehicleId,
    IReadOnlyList<Guid> JobIds,
    IReadOnlyList<RequestedPartInput> Parts) : ICommand<ServiceOrderDto>;
