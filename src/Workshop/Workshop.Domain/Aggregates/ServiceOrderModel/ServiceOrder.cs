using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Domain.Enums;
using Workshop.Domain.ValueObjects;

namespace Workshop.Domain.Aggregates.ServiceOrderModel;

/// <summary>
/// Ordem de Serviço — o agregado central. A máquina de estados é preservada exatamente do GearFlow
/// (mesmos guards, mesmos alvos), agora expressa via <see cref="Result"/> em vez de exceção, com
/// autoria (<see cref="Actor"/>) em cada transição. Ver ARCHITECTURE_DIAGRAMS.md (stateDiagram).
/// </summary>
public sealed class ServiceOrder : AggregateRoot<ServiceOrderId>
{
    private const string ReceivedMessage = "Ordem de serviço criada.";

    private readonly List<ServiceOrderHistory> _histories = new();
    private readonly List<ServiceOrderJob> _requestedJobs = new();
    private readonly List<ServiceOrderPart> _requestedParts = new();

    public int OSCode { get; private set; }
    public Guid VehicleId { get; private set; }
    public ServiceOrderStatus Status { get; private set; }
    public Actor CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<ServiceOrderJob> RequestedJobs => _requestedJobs.AsReadOnly();
    public IReadOnlyCollection<ServiceOrderPart> RequestedParts => _requestedParts.AsReadOnly();
    public IReadOnlyCollection<ServiceOrderHistory> Histories => _histories.AsReadOnly();

    private ServiceOrder(
        ServiceOrderId id, Guid vehicleId, Actor createdBy,
        IEnumerable<ServiceOrderJob> requestedJobs, IEnumerable<ServiceOrderPart> requestedParts, DateTime nowUtc)
        : base(id)
    {
        VehicleId = vehicleId;
        Status = ServiceOrderStatus.Received;
        CreatedBy = createdBy;
        CreatedOn = nowUtc;

        _requestedJobs.AddRange(requestedJobs);
        _requestedParts.AddRange(requestedParts);
        _histories.Add(new ServiceOrderHistory(Status, ReceivedMessage, createdBy, nowUtc));
    }

    private ServiceOrder() : base(ServiceOrderId.New())
    {
        CreatedBy = Actor.System;
    }

    public static ServiceOrder Create(
        Guid vehicleId, Actor createdBy,
        IEnumerable<ServiceOrderJob> requestedJobs, IEnumerable<ServiceOrderPart>? requestedParts, DateTime nowUtc) =>
        new(ServiceOrderId.New(), vehicleId, createdBy, requestedJobs, requestedParts ?? [], nowUtc);

    public Result Update(Guid vehicleId, DateTime nowUtc)
    {
        if (Status >= ServiceOrderStatus.AwaitingApproval)
            return Fail("ServiceOrder.VehicleLocked", "Não é possível alterar o veículo após aprovação do orçamento.");

        VehicleId = vehicleId;
        UpdatedOn = nowUtc;
        return Result.Success();
    }

    public Result StartDiagnostic(Actor by, DateTime nowUtc) =>
        Transition(ServiceOrderStatus.Received, ServiceOrderStatus.InDiagnostic,
            "Diagnóstico iniciado.", by, nowUtc,
            "ServiceOrder.NotReceived", "A OS deve estar 'Received' para iniciar o diagnóstico.");

    public Result FinalizeDiagnostic(Actor by, DateTime nowUtc) =>
        Transition(ServiceOrderStatus.InDiagnostic, ServiceOrderStatus.AwaitingApproval,
            "Diagnóstico finalizado. Aguardando aprovação do orçamento.", by, nowUtc,
            "ServiceOrder.NotInDiagnostic", "A OS deve estar 'InDiagnostic' para finalizar o diagnóstico.");

    public Result StartExecution(Actor by, DateTime nowUtc) =>
        Transition(ServiceOrderStatus.AwaitingApproval, ServiceOrderStatus.InExecution,
            "Orçamento aprovado. OS movida para execução.", by, nowUtc,
            "ServiceOrder.NotAwaitingApproval", "A OS deve estar 'AwaitingApproval' para iniciar a execução.");

    public Result WaitingPartsOrConsumables(Actor by, DateTime nowUtc) =>
        Transition(ServiceOrderStatus.AwaitingApproval, ServiceOrderStatus.AwaitingPartsOrConsumables,
            "Aguardando peças/insumos.", by, nowUtc,
            "ServiceOrder.NotAwaitingApproval", "A OS deve estar 'AwaitingApproval' para aguardar peças.");

    public Result ResumeExecution(Actor by, DateTime nowUtc) =>
        Transition(ServiceOrderStatus.AwaitingPartsOrConsumables, ServiceOrderStatus.InExecution,
            "Peças e insumos disponíveis. Execução retomada.", by, nowUtc,
            "ServiceOrder.NotAwaitingParts", "A OS deve estar 'AwaitingPartsOrConsumables' para retomar a execução.");

    public Result Finalize(Actor by, DateTime nowUtc) =>
        Transition(ServiceOrderStatus.InExecution, ServiceOrderStatus.Finalized,
            "Ordem de serviço finalizada. Estoque atualizado.", by, nowUtc,
            "ServiceOrder.NotInExecution", "A OS deve estar 'InExecution' para ser finalizada.");

    public Result Deliver(Actor by, DateTime nowUtc) =>
        Transition(ServiceOrderStatus.Finalized, ServiceOrderStatus.Delivered,
            "Veículo entregue ao cliente.", by, nowUtc,
            "ServiceOrder.NotFinalized", "A OS deve estar 'Finalized' para ser entregue.");

    public Result Cancel(Actor by, DateTime nowUtc) =>
        Transition(ServiceOrderStatus.AwaitingApproval, ServiceOrderStatus.Canceled,
            "Orçamento rejeitado pelo cliente. Ordem de serviço cancelada.", by, nowUtc,
            "ServiceOrder.NotAwaitingApproval", "A OS só pode ser cancelada quando aguardando aprovação do orçamento.");

    public Result Deactivate(Actor by, DateTime nowUtc)
    {
        if (Status != ServiceOrderStatus.Received && Status != ServiceOrderStatus.InDiagnostic)
            return Fail("ServiceOrder.CannotDeactivate",
                "Não é possível desativar uma OS em aprovação ou posterior.");

        IsActive = false;
        UpdatedOn = nowUtc;
        _histories.Add(new ServiceOrderHistory(Status, "Ordem de serviço desativada.", by, nowUtc));
        return Result.Success();
    }

    private Result Transition(
        ServiceOrderStatus from, ServiceOrderStatus to, string message, Actor by, DateTime nowUtc,
        string errorCode, string errorMessage)
    {
        if (Status != from)
            return Fail(errorCode, errorMessage);

        Status = to;
        UpdatedOn = nowUtc;
        _histories.Add(new ServiceOrderHistory(Status, message, by, nowUtc));
        return Result.Success();
    }

    private static Result Fail(string code, string message) =>
        Result.Failure(Error.Conflict(code, message));
}
