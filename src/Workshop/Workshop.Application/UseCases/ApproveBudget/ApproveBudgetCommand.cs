using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.ApproveBudget;

public sealed record ApproveBudgetCommand(Guid ServiceOrderId) : ICommand;
