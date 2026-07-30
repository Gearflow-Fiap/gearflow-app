using Catalog.Application.DTOs;
using Catalog.Application.UseCases.CreateJob;
using Catalog.Application.UseCases.DeleteJob;
using Catalog.Application.UseCases.GetJobById;
using Catalog.Application.UseCases.GetJobs;
using Catalog.Application.UseCases.UpdateJob;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.Http;

namespace GearFlow.Api.Endpoints.Catalog.Internal;

/// <summary>
/// Endpoints do catálogo de serviços (Job) — audiência interna (staff autenticado). Interface
/// adapter puro: traduz HTTP → command/query (ISender) → HTTP (ToOk/ToProblem).
/// </summary>
public sealed class JobEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog/jobs")
            .WithTags("Catalog");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetJobsQuery(), ct)).ToOk())
            .WithSummary("Lista os serviços (Job) do catálogo.")
            .WithDescription("Retorna todos os serviços de mão de obra cadastrados, ordenados por nome.")
            .Produces<IReadOnlyList<JobDto>>()
            .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetJobByIdQuery(id), ct)).ToOk())
            .WithSummary("Busca um serviço por id.")
            .Produces<JobDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapPost("/", async (CreateJobCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToOk())
            .WithSummary("Cria um serviço (Job) no catálogo.")
            .WithDescription("Cadastra um serviço de mão de obra com nome, descrição e preço (em centavos).")
            .Produces<JobDto>()
            .ProducesValidationProblem()
            .RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, UpdateJobRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateJobCommand(id, body.Name, body.Description, body.PriceCents), ct)).ToOk())
            .WithSummary("Atualiza um serviço.")
            .Produces<JobDto>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteJobCommand(id), ct)).ToNoContent())
            .WithSummary("Remove um serviço.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    public sealed record UpdateJobRequest(string Name, string Description, int PriceCents);
}
