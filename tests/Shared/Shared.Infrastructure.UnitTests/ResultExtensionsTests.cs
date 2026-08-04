using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Domain.Primitives;
using Shared.Infrastructure.Http;

namespace Shared.Infrastructure.UnitTests;

/// <summary>
/// Mapeamento central <see cref="Result"/>/<see cref="Error"/> → HTTP (ProblemDetails, RFC 9457).
/// Cobre cada ramo de status: por <see cref="ErrorType"/> e por inferência do sufixo do Code.
/// </summary>
public sealed class ResultExtensionsTests
{
    private static int? Status(IResult result) => ((IStatusCodeHttpResult)result).StatusCode;

    // ---- Sucessos -----------------------------------------------------------

    [Fact]
    public void ToOk_of_value_returns_200_with_body()
    {
        var result = Result.Success(42).ToOk();

        result.Should().BeOfType<Ok<int>>();
        ((Ok<int>)result).Value.Should().Be(42);
        Status(result).Should().Be(200);
    }

    [Fact]
    public void ToOk_without_body_returns_200() => Status(Result.Success().ToOk()).Should().Be(200);

    [Fact]
    public void ToNoContent_returns_204() => Status(Result.Success().ToNoContent()).Should().Be(204);

    [Fact]
    public void ToCreated_returns_201_with_location()
    {
        var result = Result.Success(7).ToCreated("/parts/7");

        result.Should().BeOfType<Created<int>>();
        ((Created<int>)result).Location.Should().Be("/parts/7");
        Status(result).Should().Be(201);
    }

    // ---- Falha por ErrorType ------------------------------------------------

    [Theory]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.Unauthorized, 401)]
    public void ToProblem_maps_error_type_to_status(ErrorType type, int expected)
    {
        var error = type switch
        {
            ErrorType.NotFound => Error.NotFound("X.NF", "não achou"),
            ErrorType.Conflict => Error.Conflict("X.C", "conflito"),
            ErrorType.Forbidden => Error.Forbidden("X.F", "proibido"),
            ErrorType.Unauthorized => Error.Unauthorized("X.U", "sem auth"),
            _ => Error.Failure("X.G", "genérico"),
        };

        Status(error.ToProblem()).Should().Be(expected);
    }

    [Fact]
    public void ToOk_of_value_on_failure_returns_problem() =>
        Status(Result.Failure<int>(Error.NotFound("X.NF", "sumiu")).ToOk()).Should().Be(404);

    [Fact]
    public void Validation_error_produces_validation_problem_400()
    {
        var result = Error.Validation("X.Bad", "campo1; campo2", "email").ToProblem();

        result.Should().BeOfType<ValidationProblem>();
        Status(result).Should().Be(400);
    }

    // ---- Status inferido pelo sufixo do Code (Type = Failure) ---------------

    [Theory]
    [InlineData("Order.NotFound", 404)]
    [InlineData("User.Forbidden", 403)]
    [InlineData("Auth.Unauthorized", 401)]
    [InlineData("Stock.Conflict", 409)]
    [InlineData("Email.AlreadyExists", 409)]
    [InlineData("Plate.AlreadyInUse", 409)]
    [InlineData("Validation.Field", 400)]
    [InlineData("Something.Weird", 400)]
    public void ToProblem_infers_status_from_code_suffix(string code, int expected) =>
        Status(new Error(code, "desc").ToProblem()).Should().Be(expected);

    // ---- Atalhos Problems.* -------------------------------------------------

    [Fact]
    public void Problems_shortcuts_map_to_expected_status()
    {
        Status(Problems.Unauthorized()).Should().Be(401);
        Status(Problems.Forbidden()).Should().Be(403);
        Status(Problems.NotFound("X.NF", "d")).Should().Be(404);
        Status(Problems.Validation("X.V", "d")).Should().Be(400);
        Status(Problems.Conflict("X.C", "d")).Should().Be(409);
        Status(Problems.UnprocessableEntity("X.UE", "d")).Should().Be(422);
        Status(Problems.ServiceUnavailable("X.SU", "d")).Should().Be(503);
        Status(Problems.InternalServerError()).Should().Be(500);
    }
}
