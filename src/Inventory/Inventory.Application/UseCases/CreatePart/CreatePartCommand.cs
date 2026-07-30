using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;

namespace Inventory.Application.UseCases.CreatePart;

public sealed record CreatePartCommand(
    string Name, string Description, string PartNumber, string Manufacturer, int PriceCents, int Quantity)
    : ICommand<PartDto>;
