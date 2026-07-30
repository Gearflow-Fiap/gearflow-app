using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;

namespace Inventory.Application.UseCases.GetPartById;

public sealed record GetPartByIdQuery(Guid PartId) : IQuery<PartDto>;
