using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;

namespace Inventory.Application.UseCases.GetParts;

public sealed record GetPartsQuery : IQuery<IReadOnlyList<PartDto>>;
