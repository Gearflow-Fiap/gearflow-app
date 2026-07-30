using Catalog.Application.Abstractions;
using Catalog.Application.DTOs;
using Catalog.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Catalog.Application.UseCases.UpdateJob;

internal sealed class UpdateJobHandler : ICommandHandler<UpdateJobCommand, JobDto>
{
    private readonly IJobRepository _repository;

    public UpdateJobHandler(IJobRepository repository) => _repository = repository;

    public async Task<Result<JobDto>> Handle(UpdateJobCommand command, CancellationToken ct)
    {
        var job = await _repository.GetByIdAsync(JobId.From(command.JobId), ct);
        if (job is null)
            return Result.Failure<JobDto>(Error.NotFound("Job.NotFound", "Serviço não encontrado."));

        var result = job.Update(command.Name, command.Description, command.PriceCents);
        if (result.IsFailure)
            return Result.Failure<JobDto>(result.Error);

        await _repository.SaveChangesAsync(ct);
        return Result.Success(JobDto.FromAggregate(job));
    }
}
