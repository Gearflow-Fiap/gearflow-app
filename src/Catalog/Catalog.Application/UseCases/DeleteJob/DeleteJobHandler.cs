using Catalog.Application.Abstractions;
using Catalog.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Catalog.Application.UseCases.DeleteJob;

internal sealed class DeleteJobHandler : ICommandHandler<DeleteJobCommand>
{
    private readonly IJobRepository _repository;

    public DeleteJobHandler(IJobRepository repository) => _repository = repository;

    public async Task<Result> Handle(DeleteJobCommand command, CancellationToken ct)
    {
        var job = await _repository.GetByIdAsync(JobId.From(command.JobId), ct);
        if (job is null)
            return Result.Failure(Error.NotFound("Job.NotFound", "Serviço não encontrado."));

        _repository.Remove(job);
        await _repository.SaveChangesAsync(ct);
        return Result.Success();
    }
}
