using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.ApproveBudgetById;

/// <summary>Aprovação keyed por orçamento (fluxo público do cliente — link de e-mail no legado).</summary>
public sealed record ApproveBudgetByIdCommand(Guid BudgetId) : ICommand;
