using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;

namespace Inventory.Application.UseCases.GetConsumables;

public sealed record GetConsumablesQuery : IQuery<IReadOnlyList<ConsumableDto>>;
