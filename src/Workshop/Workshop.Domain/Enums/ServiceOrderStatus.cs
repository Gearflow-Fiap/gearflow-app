namespace Workshop.Domain.Enums;

/// <summary>
/// Estados da Ordem de Serviço — valores preservados do GearFlow (a ordenação numérica é usada em
/// guards, ex.: <c>Update</c> só antes de <c>AwaitingApproval</c>).
/// </summary>
public enum ServiceOrderStatus
{
    Received = 1,
    InDiagnostic = 2,
    AwaitingApproval = 3,
    InExecution = 4,
    Finalized = 5,
    Delivered = 6,
    Canceled = 7,
    AwaitingPartsOrConsumables = 8,
}
