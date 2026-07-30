using Catalog.Application.Abstractions;
using Catalog.Application.DTOs;
using Catalog.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Catalog.Application.UseCases.GetJobById;

internal sealed class GetJobByIdHandler : IQueryHandler<GetJobByIdQuery, JobDto>
{
    private readonly IJobRepository _repository;

    public GetJobByIdHandler(IJobRepository repository) => _repository = repository;

    public async Task<Result<JobDto>> Handle(GetJobByIdQuery query, CancellationToken ct)
    {
        var job = await _repository.GetByIdAsync(JobId.From(query.JobId), ct);
        if (job is null)
            return Result.Failure<JobDto>(Error.NotFound("Job.NotFound", "Serviço não encontrado."));

        return Result.Success(JobDto.FromAggregate(job));
    }
}
