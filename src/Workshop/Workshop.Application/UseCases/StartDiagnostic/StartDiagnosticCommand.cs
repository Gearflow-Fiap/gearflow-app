using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.StartDiagnostic;

public sealed record StartDiagnosticCommand(Guid ServiceOrderId) : ICommand;
