using FluentAssertions;
using NetArchTest.Rules;

namespace Architecture.Tests;

/// <summary>
/// Handlers de use case são <c>internal sealed</c> (CLAUDE.md / ADR-001) — superfície pública só nas
/// interfaces de Domain e nos commands/queries de Application.
/// </summary>
public sealed class HandlerConventionTests
{
    public static TheoryData<BoundedContext> Contexts => BoundedContexts.AsTheoryData();

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Handlers_should_be_sealed_and_not_public(BoundedContext bc)
    {
        var handlers = Types.InAssembly(bc.Application)
            .That()
            .HaveNameEndingWith("Handler")
            .And().ResideInNamespaceContaining("UseCases");

        var sealedResult = handlers.Should().BeSealed().GetResult();
        sealedResult.IsSuccessful.Should().BeTrue(
            $"{bc.Name}: handlers devem ser sealed. Violações: "
            + string.Join(", ", sealedResult.FailingTypeNames ?? []));

        var notPublicResult = handlers.Should().NotBePublic().GetResult();
        notPublicResult.IsSuccessful.Should().BeTrue(
            $"{bc.Name}: handlers devem ser internal. Violações: "
            + string.Join(", ", notPublicResult.FailingTypeNames ?? []));
    }
}
