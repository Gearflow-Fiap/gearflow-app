using Customers.Application.DTOs;
using Customers.Application.UseCases.AddVehicle;
using Customers.Application.UseCases.CreateClient;
using Customers.Application.UseCases.DeleteClient;
using Customers.Application.UseCases.DeleteVehicle;
using Customers.Application.UseCases.GetClientById;
using Customers.Application.UseCases.GetClients;
using Customers.Application.UseCases.GetVehicleById;
using Customers.Application.UseCases.GetVehiclesByClient;
using Customers.Application.UseCases.UpdateClient;
using Customers.Application.UseCases.UpdateVehicle;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.Http;

namespace GearFlow.Api.Endpoints.Customers.Internal;

/// <summary>Endpoints de clientes e veículos (staff autenticado). Paridade com o ClientController legado.</summary>
public sealed class ClientEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers/clients").WithTags("Customers").RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetClientsQuery(), ct)).ToOk())
            .WithSummary("Lista os clientes.")
            .WithDescription("Retorna todos os clientes com seus veículos, ordenados por nome.")
            .Produces<IReadOnlyList<ClientDto>>();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetClientByIdQuery(id), ct)).ToOk())
            .WithSummary("Busca um cliente por id.")
            .WithDescription("Retorna o cliente (CPF/CNPJ, contato, endereço) e seus veículos; 404 se não existir.")
            .Produces<ClientDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateClientCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToOk())
            .WithSummary("Cria um cliente.")
            .WithDescription("Cadastra um cliente (CPF ou CNPJ obrigatório). Valida documento e e-mail.")
            .Produces<ClientDto>()
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", async (Guid id, UpdateClientRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateClientCommand(id, body.Name, body.Email, body.Phone, body.Address), ct)).ToOk())
            .WithSummary("Atualiza os dados de um cliente.")
            .WithDescription("Altera nome, e-mail, telefone e endereço; valida o e-mail. 404 se não existir.")
            .Produces<ClientDto>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteClientCommand(id), ct)).ToNoContent())
            .WithSummary("Remove um cliente (e seus veículos).")
            .WithDescription("Exclui o cliente e, em cascata, seus veículos; 404 se não existir.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // --- Veículos ---
        group.MapGet("/{id:guid}/vehicles", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetVehiclesByClientQuery(id), ct)).ToOk())
            .WithSummary("Lista os veículos de um cliente.")
            .WithDescription("Retorna todos os veículos cadastrados sob o cliente; 404 se o cliente não existir.")
            .Produces<IReadOnlyList<VehicleDto>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/vehicles", async (Guid id, AddVehicleRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AddVehicleCommand(
                    id, body.LicensePlate, body.Mark, body.Model, body.Color, body.YearFabrication, body.YearModel), ct)).ToOk())
            .WithSummary("Adiciona um veículo ao cliente.")
            .WithDescription("Cadastra um veículo (placa, marca, modelo, cor, ano) sob o cliente e retorna o veículo criado.")
            .Produces<VehicleDto>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapGet("/vehicles/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetVehicleByIdQuery(id), ct)).ToOk())
            .WithSummary("Busca um veículo por id.")
            .WithDescription("Retorna os dados de um veículo; 404 se não existir.")
            .Produces<VehicleDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/vehicles/{id:guid}", async (Guid id, UpdateVehicleRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateVehicleCommand(
                    id, body.LicensePlate, body.Mark, body.Model, body.Color, body.YearFabrication, body.YearModel), ct)).ToOk())
            .WithSummary("Atualiza um veículo.")
            .WithDescription("Altera placa, marca, modelo, cor e anos de um veículo; 404 se não existir.")
            .Produces<VehicleDto>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/vehicles/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteVehicleCommand(id), ct)).ToNoContent())
            .WithSummary("Remove um veículo.")
            .WithDescription("Exclui um veículo do cliente; 404 se não existir.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    public sealed record UpdateClientRequest(string Name, string Email, string Phone, AddressDto Address);

    public sealed record AddVehicleRequest(
        string LicensePlate, string Mark, string Model, string Color, int YearFabrication, int YearModel);

    public sealed record UpdateVehicleRequest(
        string LicensePlate, string Mark, string Model, string Color, int YearFabrication, int YearModel);
}
