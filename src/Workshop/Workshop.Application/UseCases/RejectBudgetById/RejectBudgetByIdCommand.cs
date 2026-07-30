using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.RejectBudgetById;

/// <summary>Rejeição keyed por orçamento (fluxo público do cliente — link de e-mail no legado).</summary>
public sealed record RejectBudgetByIdCommand(Guid BudgetId) : ICommand;
