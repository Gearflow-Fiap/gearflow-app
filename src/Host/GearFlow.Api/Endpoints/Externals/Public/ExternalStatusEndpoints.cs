using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.Http;
using Workshop.Application.DTOs;
using Workshop.Application.UseCases.GetServiceOrderById;

namespace GearFlow.Api.Endpoints.Externals.Public;

/// <summary>
/// Consulta pública de status da OS pelo cliente (paridade com o ExternalsController legado).
/// Anônimo — o DTO expõe só o TIPO do ator no histórico, nunca ids sensíveis.
/// TODO: validar que a OS pertence ao <c>clientId</c> (join OS→veículo→cliente, cross-BC) — hoje a
/// consulta é por id de OS; o <c>clientId</c> da rota é aceito para compatibilidade de URL.
/// </summary>
public sealed class ExternalStatusEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/externals/{clientId:guid}/serviceOrder/{id:guid}",
                async (Guid clientId, Guid id, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new GetServiceOrderByIdQuery(id), ct)).ToOk())
            .WithTags("Externals")
            .WithSummary("Consulta pública do status de uma ordem de serviço.")
            .WithDescription("Retorna status e histórico da OS para acompanhamento pelo cliente.")
            .Produces<ServiceOrderDto>()
            .Produces(StatusCodes.Status404NotFound);
    }
}
