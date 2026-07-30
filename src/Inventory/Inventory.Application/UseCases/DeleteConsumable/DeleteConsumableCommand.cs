using Inventory.Application.Abstractions;

namespace Inventory.Application.UseCases.DeleteConsumable;

public sealed record DeleteConsumableCommand(Guid ConsumableId) : ICommand;
