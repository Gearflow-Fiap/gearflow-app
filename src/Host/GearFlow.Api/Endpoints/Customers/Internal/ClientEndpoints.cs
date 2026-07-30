using Customers.Application.DTOs;
using Customers.Application.UseCases.CreateClient;
using Customers.Application.UseCases.GetClientById;
using Customers.Application.UseCases.GetClients;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.Http;

namespace GearFlow.Api.Endpoints.Customers.Internal;

/// <summary>Endpoints de clientes (staff autenticado). Interface adapter: HTTP → command/query → HTTP.</summary>
public sealed class ClientEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers/clients").WithTags("Customers");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetClientsQuery(), ct)).ToOk())
            .WithSummary("Lista os clientes.")
            .WithDescription("Retorna todos os clientes com seus veículos, ordenados por nome.")
            .Produces<IReadOnlyList<ClientDto>>()
            .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetClientByIdQuery(id), ct)).ToOk())
            .WithSummary("Busca um cliente por id.")
            .WithDescription("Retorna o cliente e seus veículos; 404 se não existir.")
            .Produces<ClientDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapPost("/", async (CreateClientCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToOk())
            .WithSummary("Cria um cliente.")
            .WithDescription("Cadastra um cliente (CPF ou CNPJ obrigatório). Valida documento e e-mail.")
            .Produces<ClientDto>()
            .ProducesValidationProblem()
            .RequireAuthorization();
    }
}
