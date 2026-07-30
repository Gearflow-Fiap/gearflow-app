using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.RejectBudget;

public sealed record RejectBudgetCommand(Guid ServiceOrderId) : ICommand;
