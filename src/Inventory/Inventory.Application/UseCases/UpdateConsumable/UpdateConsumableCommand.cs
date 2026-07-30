using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;

namespace Inventory.Application.UseCases.UpdateConsumable;

public sealed record UpdateConsumableCommand(Guid ConsumableId, string Name, int UnitPriceCents, decimal Quantity)
    : ICommand<ConsumableDto>;
