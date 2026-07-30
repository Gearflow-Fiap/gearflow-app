using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.ResumeExecution;

public sealed record ResumeExecutionCommand(Guid ServiceOrderId) : ICommand;
