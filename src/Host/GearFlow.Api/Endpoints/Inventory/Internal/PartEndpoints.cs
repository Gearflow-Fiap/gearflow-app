using Inventory.Application.DTOs;
using Inventory.Application.UseCases.AddPartStock;
using Inventory.Application.UseCases.CreatePart;
using Inventory.Application.UseCases.DeletePart;
using Inventory.Application.UseCases.GetPartById;
using Inventory.Application.UseCases.GetParts;
using Inventory.Application.UseCases.UpdatePart;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.Http;

namespace GearFlow.Api.Endpoints.Inventory.Internal;

/// <summary>Endpoints de peças (estoque) — staff autenticado.</summary>
public sealed class PartEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/parts").WithTags("Inventory");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetPartsQuery(), ct)).ToOk())
            .WithSummary("Lista as peças em estoque.")
            .WithDescription("Retorna todas as peças com quantidade disponível/reservada e flag de estoque mínimo.")
            .Produces<IReadOnlyList<PartDto>>()
            .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetPartByIdQuery(id), ct)).ToOk())
            .WithSummary("Busca uma peça por id.")
            .Produces<PartDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapPost("/", async (CreatePartCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToOk())
            .WithSummary("Cadastra uma peça.")
            .Produces<PartDto>()
            .ProducesValidationProblem()
            .RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, UpdatePartRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdatePartCommand(
                    id, body.Name, body.Description, body.PartNumber, body.Manufacturer, body.PriceCents, body.Quantity), ct)).ToOk())
            .WithSummary("Atualiza os dados de uma peça.")
            .Produces<PartDto>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeletePartCommand(id), ct)).ToNoContent())
            .WithSummary("Remove uma peça.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapPatch("/{id:guid}/stock", async (Guid id, AddPartStockRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AddPartStockCommand(id, body.Quantity), ct)).ToOk())
            .WithSummary("Adiciona estoque a uma peça.")
            .WithDescription("Repõe a quantidade em estoque; publica evento para reavaliar OS aguardando peças (auto-resume).")
            .Produces<PartDto>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .RequireAuthorization();
    }

    public sealed record AddPartStockRequest(int Quantity);
    public sealed record UpdatePartRequest(
        string Name, string Description, string PartNumber, string Manufacturer, int PriceCents, int Quantity);
}
