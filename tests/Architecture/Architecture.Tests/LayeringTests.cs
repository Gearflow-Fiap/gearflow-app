using FluentAssertions;
using NetArchTest.Rules;

namespace Architecture.Tests;

/// <summary>
/// Faz valer a direção de dependência por BC (ADR-001): Api → Application → Domain,
/// Infrastructure → Application → Domain, Domain sem dependências de infraestrutura.
/// </summary>
public sealed class LayeringTests
{
    public static TheoryData<BoundedContext> Contexts => BoundedContexts.AsTheoryData();

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Domain_should_not_depend_on_Application_or_Infrastructure(BoundedContext bc)
    {
        var result = Types.InAssembly(bc.Domain)
            .Should()
            .NotHaveDependencyOnAny(
                bc.Application.GetName().Name,
                bc.Infrastructure.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{bc.Name}.Domain não pode depender de Application/Infrastructure. Violações: "
            + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Domain_should_not_depend_on_infrastructure_frameworks(BoundedContext bc)
    {
        var result = Types.InAssembly(bc.Domain)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Microsoft.Data.SqlClient",
                "Dapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{bc.Name}.Domain deve ser puro. Violações: "
            + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Application_should_not_depend_on_Infrastructure(BoundedContext bc)
    {
        var result = Types.InAssembly(bc.Application)
            .Should()
            .NotHaveDependencyOn(bc.Infrastructure.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{bc.Name}.Application não pode depender de Infrastructure. Violações: "
            + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
