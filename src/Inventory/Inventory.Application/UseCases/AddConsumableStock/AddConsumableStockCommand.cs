using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;

namespace Inventory.Application.UseCases.AddConsumableStock;

public sealed record AddConsumableStockCommand(Guid ConsumableId, decimal Quantity) : ICommand<ConsumableDto>;
