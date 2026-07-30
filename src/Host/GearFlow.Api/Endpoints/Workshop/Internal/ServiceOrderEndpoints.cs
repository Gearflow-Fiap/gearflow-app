using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.Http;
using Workshop.Application.DTOs;
using Workshop.Application.UseCases.ApproveBudget;
using Workshop.Application.UseCases.CreateServiceOrder;
using Workshop.Application.UseCases.DeactivateServiceOrder;
using Workshop.Application.UseCases.DeliverServiceOrder;
using Workshop.Application.UseCases.ExecuteJob;
using Workshop.Application.UseCases.FinalizeDiagnostic;
using Workshop.Application.UseCases.FinalizeServiceOrder;
using Workshop.Application.UseCases.GetExecutionAverage;
using Workshop.Application.UseCases.GetServiceOrderById;
using Workshop.Application.UseCases.GetServiceOrderDetails;
using Workshop.Application.UseCases.GetServiceOrders;
using Workshop.Application.UseCases.RejectBudget;
using Workshop.Application.UseCases.ResumeExecution;
using Workshop.Application.UseCases.StartDiagnostic;
using Workshop.Application.UseCases.UpdateServiceOrderVehicle;

namespace GearFlow.Api.Endpoints.Workshop.Internal;

/// <summary>
/// Ordens de serviço (staff autenticado). Paridade com o ServiceOrdersController legado: consultas
/// (lista, detalhes, média de execução) + todo o ciclo de vida da máquina de estados.
/// </summary>
public sealed class ServiceOrderEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workshop/service-orders").WithTags("Workshop").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetServiceOrdersQuery(page ?? 1, pageSize ?? 20), ct)).ToOk())
            .WithSummary("Lista as ordens de serviço ativas (paginado, ordenado por prioridade de status).")
            .WithDescription("Retorna uma página de OS não finalizadas/entregues, priorizando as em execução; aceita page/pageSize.")
            .Produces<PagedResult<ServiceOrderDto>>();

        group.MapGet("/monitoring-average", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetExecutionAverageQuery(), ct)).ToOk())
            .WithSummary("Tempo médio por fase (Diagnóstico, Execução, Finalização).")
            .WithDescription("Métrica para o dashboard da Fase 3, calculada a partir do histórico de status.")
            .Produces<MonitoringAverageDto>();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetServiceOrderByIdQuery(id), ct)).ToOk())
            .WithSummary("Consulta uma OS por id (com histórico de status).")
            .WithDescription("Retorna a OS (código, veículo, status, serviços solicitados) e a linha do tempo de status; 404 se não existir.")
            .Produces<ServiceOrderDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/details", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetServiceOrderDetailsQuery(id), ct)).ToOk())
            .WithSummary("Consulta detalhada da OS (com o orçamento).")
            .WithDescription("Retorna a OS mais o orçamento (itens de serviço/peça/insumo, total e status de aprovação); 404 se não existir.")
            .Produces<ServiceOrderDetailsDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateServiceOrderCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToOk())
            .WithSummary("Abre uma ordem de serviço.")
            .WithDescription("Cria a OS (status Received) para um veículo, com serviços e peças solicitados.")
            .Produces<ServiceOrderDto>()
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", async (Guid id, UpdateOrderVehicleRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateServiceOrderVehicleCommand(id, body.VehicleId), ct)).ToOk())
            .WithSummary("Troca o veículo da OS (bloqueado após aprovação do orçamento).")
            .WithDescription("Altera o veículo vinculado à OS. Permitido apenas antes de AwaitingApproval (409 caso contrário).")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/in-diagnostic", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new StartDiagnosticCommand(id), ct)).ToOk())
            .WithSummary("Inicia o diagnóstico (Received → InDiagnostic).")
            .WithDescription("Move a OS de Received para InDiagnostic. 409 se não estiver em Received.")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/finalize-diagnostic", async (Guid id, FinalizeDiagnosticRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new FinalizeDiagnosticCommand(id, body.Consumables), ct)).ToOk())
            .WithSummary("Finaliza o diagnóstico e gera o orçamento (InDiagnostic → AwaitingApproval).")
            .WithDescription("Cria o orçamento com preços snapshotted (serviços/peças da OS + insumos do diagnóstico) e notifica o cliente. 409 se não estiver em InDiagnostic.")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/approve-budget", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ApproveBudgetCommand(id), ct)).ToOk())
            .WithSummary("Aprova o orçamento (por OS); reserva estoque (→ InExecution ou AwaitingParts).")
            .WithDescription("Aprova o orçamento e tenta reservar as peças/insumos: se houver estoque vai para InExecution, senão para AwaitingPartsOrConsumables.")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/reject-budget", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RejectBudgetCommand(id), ct)).ToOk())
            .WithSummary("Rejeita o orçamento (por OS) e cancela a OS.")
            .WithDescription("Marca o orçamento como rejeitado e cancela a OS (→ Canceled). 409 se a OS não estiver aguardando aprovação.")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/execute-job", async (Guid id, ExecuteJobRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ExecuteJobCommand(id, body.BudgetJobId), ct)).ToOk())
            .WithSummary("Marca um serviço do orçamento como executado (OS em execução).")
            .WithDescription("Registra a execução de um item de serviço do orçamento (idempotente). Exige a OS em InExecution.")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/resume-execution-from-waiting-parts", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ResumeExecutionCommand(id), ct)).ToOk())
            .WithSummary("Retoma a execução após reposição (re-tenta a reserva).")
            .WithDescription("Para uma OS em AwaitingPartsOrConsumables, re-tenta reservar o estoque e, se couber, volta para InExecution.")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/finalize", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new FinalizeServiceOrderCommand(id), ct)).ToOk())
            .WithSummary("Finaliza a OS; consome o estoque reservado (InExecution → Finalized).")
            .WithDescription("Baixa a reserva de peças/insumos (consumo), move a OS para Finalized e dispara alerta de estoque mínimo quando aplicável.")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/deliver", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeliverServiceOrderCommand(id), ct)).ToOk())
            .WithSummary("Entrega o veículo (Finalized → Delivered).")
            .WithDescription("Marca a OS como entregue ao cliente. Exige a OS em Finalized (409 caso contrário).")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeactivateServiceOrderCommand(id), ct)).ToNoContent())
            .WithSummary("Desativa (soft-delete) a OS — só antes da aprovação.")
            .WithDescription("Marca a OS como inativa (soft-delete). Permitido apenas em Received ou InDiagnostic (409 caso contrário).")
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
    }

    public sealed record UpdateOrderVehicleRequest(Guid VehicleId);
    public sealed record ExecuteJobRequest(Guid BudgetJobId);
    public sealed record FinalizeDiagnosticRequest(IReadOnlyList<DiagnosticConsumableInput> Consumables);
}
