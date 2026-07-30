using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;

namespace Inventory.Application.UseCases.CreateConsumable;

public sealed record CreateConsumableCommand(string Name, int UnitPriceCents, decimal Quantity) : ICommand<ConsumableDto>;
