using FluentAssertions;
using Shared.Domain.Primitives;

namespace Shared.Domain.UnitTests;

public sealed class ResultAndErrorTests
{
    [Fact]
    public void Success_is_success_and_has_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_carries_the_error()
    {
        var error = Error.Validation("X.Bad", "ruim", "campo");

        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Generic_success_exposes_value()
    {
        Result.Success(42).Value.Should().Be(42);
    }

    [Fact]
    public void Accessing_value_of_failure_throws()
    {
        var result = Result.Failure<int>(Error.NotFound("X.NF", "sumiu"));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Success_with_error_throws()
    {
        var act = () => Result.Failure(Error.None);   // !isSuccess mas Error.None => inválido

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Forbidden)]
    [InlineData(ErrorType.Unauthorized)]
    public void Error_factories_set_the_expected_type(ErrorType type)
    {
        Error error = type switch
        {
            ErrorType.Failure => Error.Failure("c", "d"),
            ErrorType.NotFound => Error.NotFound("c", "d"),
            ErrorType.Validation => Error.Validation("c", "d"),
            ErrorType.Conflict => Error.Conflict("c", "d"),
            ErrorType.Forbidden => Error.Forbidden("c", "d"),
            ErrorType.Unauthorized => Error.Unauthorized("c", "d"),
            _ => Error.None,
        };

        error.Type.Should().Be(type);
        error.Code.Should().Be("c");
    }

    [Fact]
    public void Validation_carries_optional_field()
    {
        Error.Validation("c", "d", "email").Field.Should().Be("email");
    }
}
