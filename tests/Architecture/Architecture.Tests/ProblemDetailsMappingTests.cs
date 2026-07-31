using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Domain.Primitives;
using Shared.Infrastructure.Http;

namespace Architecture.Tests;

/// <summary>
/// Contrato de erro (RFC 9457): o mapa central <c>Result</c>/<c>Error</c> → HTTP em
/// <c>ResultExtensions</c> produz o status certo por <see cref="ErrorType"/> e, para os erros legados
/// (Type = Failure), infere pelo sufixo do <c>Code</c>. Roda no CI sem Docker.
/// </summary>
public sealed class ProblemDetailsMappingTests
{
    private static int? Status(Error error) => ((ProblemHttpResult)error.ToProblem()).StatusCode;

    [Fact]
    public void NotFound_maps_to_404() => Status(Error.NotFound("X.NotFound", "d")).Should().Be(404);

    [Fact]
    public void Conflict_maps_to_409() => Status(Error.Conflict("X.Conflict", "d")).Should().Be(409);

    [Fact]
    public void Unauthorized_maps_to_401() => Status(Error.Unauthorized("X.Denied", "d")).Should().Be(401);

    [Fact]
    public void Forbidden_maps_to_403() => Status(Error.Forbidden("X.Denied", "d")).Should().Be(403);

    [Fact]
    public void Validation_maps_to_400_validation_problem()
    {
        var result = Error.Validation("X.Invalid", "campo inválido", "name").ToProblem();

        var problem = result.Should().BeOfType<ValidationProblem>().Subject;
        problem.StatusCode.Should().Be(400);
        problem.ProblemDetails.Errors.Should().ContainKey("name");
    }

    [Theory]
    [InlineData("Order.NotFound", 404)]
    [InlineData("Email.AlreadyExists", 409)]
    [InlineData("Coupon.Conflict", 409)]
    [InlineData("Access.Forbidden", 403)]
    [InlineData("Token.Unauthorized", 401)]
    [InlineData("Something.Failed", 400)]
    public void Legacy_failure_error_infers_status_from_code_suffix(string code, int expected) =>
        Status(new Error(code, "d")).Should().Be(expected);   // Type = Failure (default)

    [Fact]
    public void Problem_body_carries_the_error_code()
    {
        var problem = (ProblemHttpResult)Error.NotFound("Job.NotFound", "não achou").ToProblem();

        problem.ProblemDetails.Extensions.Should().ContainKey("code");
        problem.ProblemDetails.Extensions["code"].Should().Be("Job.NotFound");
    }

    [Fact]
    public void Success_result_maps_to_200()
    {
        Result.Success(42).ToOk().Should().BeOfType<Ok<int>>().Which.StatusCode.Should().Be(200);
    }
}
