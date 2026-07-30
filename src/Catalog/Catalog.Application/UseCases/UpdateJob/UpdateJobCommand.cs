using Catalog.Application.Abstractions;
using Catalog.Application.DTOs;

namespace Catalog.Application.UseCases.UpdateJob;

public sealed record UpdateJobCommand(Guid JobId, string Name, string Description, int PriceCents) : ICommand<JobDto>;
