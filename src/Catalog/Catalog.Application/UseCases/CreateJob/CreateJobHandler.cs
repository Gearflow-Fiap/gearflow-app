using Catalog.Application.Abstractions;
using Catalog.Application.DTOs;
using Catalog.Domain.Aggregates;
using Shared.Domain.Primitives;

namespace Catalog.Application.UseCases.CreateJob;

internal sealed class CreateJobHandler : ICommandHandler<CreateJobCommand, JobDto>
{
    private readonly IJobRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateJobHandler(IJobRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<JobDto>> Handle(CreateJobCommand command, CancellationToken ct)
    {
        var result = Job.Create(command.Name, command.Description, command.PriceCents, _timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
            return Result.Failure<JobDto>(result.Error);

        await _repository.AddAsync(result.Value, ct);
        await _repository.SaveChangesAsync(ct);

        return Result.Success(JobDto.FromAggregate(result.Value));
    }
}
