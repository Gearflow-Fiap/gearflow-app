using Catalog.Application.Abstractions;
using Catalog.Application.DTOs;

namespace Catalog.Application.UseCases.CreateJob;

public sealed record CreateJobCommand(string Name, string Description, int PriceCents) : ICommand<JobDto>;
