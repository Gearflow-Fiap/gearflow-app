using Shared.Domain.Primitives;
using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;
using Workshop.Domain.Aggregates.ServiceOrderModel;
using Workshop.Domain.Enums;

namespace Workshop.Application.UseCases.GetExecutionAverage;

/// <summary>
/// Tempo médio (min) por fase — Diagnóstico (InDiagnostic→AwaitingApproval), Execução
/// (InExecution→Finalized) e Finalização (Finalized→Delivered) — para o dashboard da Fase 3.
/// Calculado a partir do histórico de status de cada OS.
/// </summary>
internal sealed class GetExecutionAverageHandler : IQueryHandler<GetExecutionAverageQuery, MonitoringAverageDto>
{
    private readonly IServiceOrderRepository _repository;

    public GetExecutionAverageHandler(IServiceOrderRepository repository) => _repository = repository;

    public async Task<Result<MonitoringAverageDto>> Handle(GetExecutionAverageQuery query, CancellationToken ct)
    {
        var orders = await _repository.ListAsync(ct);

        var diagnostic = new List<double>();
        var execution = new List<double>();
        var finalization = new List<double>();

        foreach (var order in orders)
        {
            AddPhase(order, ServiceOrderStatus.InDiagnostic, ServiceOrderStatus.AwaitingApproval, diagnostic);
            AddPhase(order, ServiceOrderStatus.InExecution, ServiceOrderStatus.Finalized, execution);
            AddPhase(order, ServiceOrderStatus.Finalized, ServiceOrderStatus.Delivered, finalization);
        }

        var sampleSize = orders.Count(o =>
            o.Status is ServiceOrderStatus.Finalized or ServiceOrderStatus.Delivered);

        return Result.Success(new MonitoringAverageDto(
            Avg(diagnostic), Avg(execution), Avg(finalization), sampleSize));
    }

    private static void AddPhase(ServiceOrder order, ServiceOrderStatus from, ServiceOrderStatus to, List<double> bucket)
    {
        var start = order.Histories.Where(h => h.Status == from).OrderBy(h => h.CreatedOn).FirstOrDefault();
        var end = order.Histories.Where(h => h.Status == to).OrderBy(h => h.CreatedOn).FirstOrDefault();
        if (start is not null && end is not null && end.CreatedOn > start.CreatedOn)
            bucket.Add((end.CreatedOn - start.CreatedOn).TotalMinutes);
    }

    private static double Avg(List<double> values) => values.Count == 0 ? 0 : values.Average();
}
