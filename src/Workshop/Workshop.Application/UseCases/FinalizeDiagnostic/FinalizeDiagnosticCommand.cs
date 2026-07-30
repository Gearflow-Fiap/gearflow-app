using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.FinalizeDiagnostic;

public sealed record DiagnosticConsumableInput(Guid ConsumableId, decimal Quantity);

/// <summary>
/// Finaliza o diagnóstico e gera o orçamento. Preserva o fluxo do GearFlow: usa os serviços/peças já
/// solicitados na OS + insumos apontados no diagnóstico, com preços snapshotados dos BCs Catalog/Inventory.
/// </summary>
public sealed record FinalizeDiagnosticCommand(
    Guid ServiceOrderId,
    IReadOnlyList<DiagnosticConsumableInput> Consumables) : ICommand;
