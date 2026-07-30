using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;

namespace Inventory.Application.UseCases.AddPartStock;

public sealed record AddPartStockCommand(Guid PartId, int Quantity) : ICommand<PartDto>;
