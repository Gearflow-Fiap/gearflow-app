using Catalog.Application.Abstractions;
using Catalog.Application.DTOs;

namespace Catalog.Application.UseCases.GetJobById;

public sealed record GetJobByIdQuery(Guid JobId) : IQuery<JobDto>;
