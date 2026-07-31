using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Domain.Primitives;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.Http;
using Workshop.Application.DTOs;
using Workshop.Application.UseCases.ApproveBudgetById;
using Workshop.Application.UseCases.GetBudgetById;
using Workshop.Application.UseCases.RejectBudgetById;

namespace GearFlow.Api.Endpoints.Workshop.Public;

/// <summary>
/// Orçamento por id. Aprovar/rejeitar é o fluxo do CLIENTE (anônimo). Há duas formas:
/// os <c>GET .../approve|reject</c> (clicáveis no link do e-mail, devolvem página HTML de confirmação —
/// paridade com o BudgetsController legado) e os <c>PUT</c> equivalentes (consumidos pelo app/API).
/// </summary>
public sealed class BudgetEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workshop/budgets").WithTags("Workshop");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetBudgetByIdQuery(id), ct)).ToOk())
            .WithSummary("Consulta um orçamento por id.")
            .WithDescription("Retorna o orçamento com itens (serviços/peças/insumos), total e status de aprovação; 404 se não existir.")
            .Produces<BudgetDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        // Links do e-mail (GET) — executam a ação e devolvem uma página de confirmação.
        group.MapGet("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
                Page(await sender.Send(new ApproveBudgetByIdCommand(id), ct), "Orçamento aprovado!", "Sua ordem de serviço seguirá para execução."))
            .WithSummary("Aprova o orçamento via link (cliente).")
            .WithDescription("Aprova o orçamento e reserva o estoque; devolve uma página HTML de confirmação (usado no link do e-mail).");

        group.MapGet("/{id:guid}/reject", async (Guid id, ISender sender, CancellationToken ct) =>
                Page(await sender.Send(new RejectBudgetByIdCommand(id), ct), "Orçamento rejeitado", "A ordem de serviço foi cancelada."))
            .WithSummary("Rejeita o orçamento via link (cliente).")
            .WithDescription("Rejeita o orçamento e cancela a OS; devolve uma página HTML de confirmação (usado no link do e-mail).");

        // Equivalentes PUT — consumidos pelo app/API.
        group.MapPut("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ApproveBudgetByIdCommand(id), ct)).ToOk())
            .WithSummary("Aprova o orçamento (cliente).")
            .WithDescription("Aprova o orçamento e reserva o estoque; move a OS para execução ou aguardando peças.")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/reject", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RejectBudgetByIdCommand(id), ct)).ToOk())
            .WithSummary("Rejeita o orçamento (cliente).")
            .WithDescription("Rejeita o orçamento e cancela a OS.")
            .Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
    }

    private static IResult Page(Result result, string okTitle, string okMessage)
    {
        var (title, message, color) = result.IsSuccess
            ? (okTitle, okMessage, "#059669")
            : ("Não foi possível concluir", result.Error.Description, "#dc2626");

        var html = $$"""
            <!doctype html>
            <html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <title>GearFlow</title></head>
            <body style="font-family: system-ui, sans-serif; background:#f8fafc; margin:0; display:grid; place-items:center; min-height:100vh;">
              <div style="background:#fff; border:1px solid #e2e8f0; border-radius:12px; padding:32px; max-width:420px; text-align:center; box-shadow:0 1px 3px rgba(0,0,0,.06);">
                <div style="font-size:40px; margin-bottom:8px;">🔧</div>
                <h1 style="color:{{color}}; font-size:20px; margin:0 0 8px;">{{title}}</h1>
                <p style="color:#475569; font-size:14px; margin:0;">{{message}}</p>
              </div>
            </body></html>
            """;

        return Results.Content(html, "text/html; charset=utf-8");
    }
}
