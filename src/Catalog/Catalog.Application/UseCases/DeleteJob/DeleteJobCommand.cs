using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCases.DeleteJob;

public sealed record DeleteJobCommand(Guid JobId) : ICommand;
