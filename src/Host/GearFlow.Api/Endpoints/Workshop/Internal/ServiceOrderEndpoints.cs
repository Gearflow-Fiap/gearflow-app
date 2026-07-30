using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.Http;
using Workshop.Application.DTOs;
using Workshop.Application.UseCases.ApproveBudget;
using Workshop.Application.UseCases.CreateServiceOrder;
using Workshop.Application.UseCases.DeliverServiceOrder;
using Workshop.Application.UseCases.FinalizeDiagnostic;
using Workshop.Application.UseCases.FinalizeServiceOrder;
using Workshop.Application.UseCases.GetServiceOrderById;
using Workshop.Application.UseCases.RejectBudget;
using Workshop.Application.UseCases.StartDiagnostic;

namespace GearFlow.Api.Endpoints.Workshop.Internal;

/// <summary>
/// Ordens de serviço (staff autenticado). Cada transição é um command; o ciclo de vida completo
/// (Received → Diagnostic → AwaitingApproval → Execution → Finalized → Delivered) está aqui.
/// </summary>
public sealed class ServiceOrderEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workshop/service-orders").WithTags("Workshop").RequireAuthorization();

        group.MapPost("/", async (CreateServiceOrderCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToOk())
            .WithSummary("Abre uma ordem de serviço.")
            .WithDescription("Cria a OS (status Received) para um veículo, com serviços e peças solicitados.")
            .Produces<ServiceOrderDto>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetServiceOrderByIdQuery(id), ct)).ToOk())
            .WithSummary("Consulta uma OS por id (com histórico de status).")
            .Produces<ServiceOrderDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/in-diagnostic", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new StartDiagnosticCommand(id), ct)).ToOk())
            .WithSummary("Inicia o diagnóstico (Received → InDiagnostic).")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/finalize-diagnostic", async (Guid id, FinalizeDiagnosticRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new FinalizeDiagnosticCommand(id, body.Consumables), ct)).ToOk())
            .WithSummary("Finaliza o diagnóstico e gera o orçamento (InDiagnostic → AwaitingApproval).")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/approve-budget", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ApproveBudgetCommand(id), ct)).ToOk())
            .WithSummary("Aprova o orçamento; reserva estoque (→ InExecution ou AwaitingParts).")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/reject-budget", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RejectBudgetCommand(id), ct)).ToOk())
            .WithSummary("Rejeita o orçamento e cancela a OS.")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/finalize", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new FinalizeServiceOrderCommand(id), ct)).ToOk())
            .WithSummary("Finaliza a OS; consome o estoque reservado (InExecution → Finalized).")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/deliver", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeliverServiceOrderCommand(id), ct)).ToOk())
            .WithSummary("Entrega o veículo (Finalized → Delivered).")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
    }

    public sealed record FinalizeDiagnosticRequest(IReadOnlyList<DiagnosticConsumableInput> Consumables);
}
