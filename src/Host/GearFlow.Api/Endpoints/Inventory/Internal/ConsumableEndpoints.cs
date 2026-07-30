using Inventory.Application.DTOs;
using Inventory.Application.UseCases.AddConsumableStock;
using Inventory.Application.UseCases.CreateConsumable;
using Inventory.Application.UseCases.GetConsumables;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.Http;

namespace GearFlow.Api.Endpoints.Inventory.Internal;

/// <summary>Endpoints de insumos (estoque) — staff autenticado.</summary>
public sealed class ConsumableEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/consumables").WithTags("Inventory");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetConsumablesQuery(), ct)).ToOk())
            .WithSummary("Lista os insumos em estoque.")
            .WithDescription("Retorna todos os insumos com quantidade disponível/reservada e flag de estoque mínimo.")
            .Produces<IReadOnlyList<ConsumableDto>>()
            .RequireAuthorization();

        group.MapPost("/", async (CreateConsumableCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToOk())
            .WithSummary("Cadastra um insumo.")
            .Produces<ConsumableDto>()
            .ProducesValidationProblem()
            .RequireAuthorization();

        group.MapPatch("/{id:guid}/stock", async (Guid id, AddConsumableStockRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AddConsumableStockCommand(id, body.Quantity), ct)).ToOk())
            .WithSummary("Adiciona estoque a um insumo.")
            .WithDescription("Repõe a quantidade; publica evento para reavaliar OS aguardando insumos (auto-resume).")
            .Produces<ConsumableDto>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .RequireAuthorization();
    }

    public sealed record AddConsumableStockRequest(decimal Quantity);
}
