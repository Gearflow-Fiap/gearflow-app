using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;

namespace Inventory.Application.UseCases.UpdatePart;

public sealed record UpdatePartCommand(
    Guid PartId, string Name, string Description, string PartNumber, string Manufacturer, int PriceCents, int Quantity)
    : ICommand<PartDto>;
