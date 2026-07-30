using Catalog.Application.Abstractions;
using Catalog.Application.DTOs;

namespace Catalog.Application.UseCases.GetJobs;

public sealed record GetJobsQuery : IQuery<IReadOnlyList<JobDto>>;
