using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;

namespace Inventory.Application.UseCases.GetConsumableById;

public sealed record GetConsumableByIdQuery(Guid ConsumableId) : IQuery<ConsumableDto>;
