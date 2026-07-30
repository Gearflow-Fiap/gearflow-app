using Microsoft.AspNetCore.Routing;

namespace Shared.Infrastructure.Endpoints;

public interface IEndpoint
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
