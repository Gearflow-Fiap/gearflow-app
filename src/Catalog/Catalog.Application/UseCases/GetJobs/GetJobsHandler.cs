using Catalog.Application.Abstractions;
using Catalog.Application.DTOs;
using Shared.Domain.Primitives;

namespace Catalog.Application.UseCases.GetJobs;

internal sealed class GetJobsHandler : IQueryHandler<GetJobsQuery, IReadOnlyList<JobDto>>
{
    private readonly IJobRepository _repository;

    public GetJobsHandler(IJobRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<JobDto>>> Handle(GetJobsQuery query, CancellationToken ct)
    {
        var jobs = await _repository.ListAsync(ct);
        IReadOnlyList<JobDto> dtos = jobs.Select(JobDto.FromAggregate).ToList();
        return Result.Success(dtos);
    }
}
