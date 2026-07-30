using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.Http;
using Workshop.Application.DTOs;
using Workshop.Application.UseCases.ApproveBudgetById;
using Workshop.Application.UseCases.GetBudgetById;
using Workshop.Application.UseCases.RejectBudgetById;

namespace GearFlow.Api.Endpoints.Workshop.Public;

/// <summary>
/// Orçamento por id. Aprovar/rejeitar é o fluxo do CLIENTE (anônimo — no legado vinha do link de
/// e-mail). A consulta exige staff. Paridade com o BudgetsController legado.
/// </summary>
public sealed class BudgetEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workshop/budgets").WithTags("Workshop");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetBudgetByIdQuery(id), ct)).ToOk())
            .WithSummary("Consulta um orçamento por id.")
            .Produces<BudgetDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapPut("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ApproveBudgetByIdCommand(id), ct)).ToOk())
            .WithSummary("Aprova o orçamento (cliente).")
            .WithDescription("Aprova o orçamento e reserva o estoque; move a OS para execução ou aguardando peças.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/reject", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RejectBudgetByIdCommand(id), ct)).ToOk())
            .WithSummary("Rejeita o orçamento (cliente).")
            .WithDescription("Rejeita o orçamento e cancela a OS.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }
}
