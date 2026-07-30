using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.ExecuteJob;

public sealed record ExecuteJobCommand(Guid ServiceOrderId, Guid BudgetJobId) : ICommand;
