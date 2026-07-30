using Inventory.Application.Abstractions;

namespace Inventory.Application.UseCases.DeletePart;

public sealed record DeletePartCommand(Guid PartId) : ICommand;
