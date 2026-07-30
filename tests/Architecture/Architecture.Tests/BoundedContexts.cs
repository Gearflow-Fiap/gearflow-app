using System.Reflection;

namespace Architecture.Tests;

/// <summary>
/// Lista dos Bounded Contexts sob teste de arquitetura. Ao migrar um BC novo, adicione uma entrada
/// aqui e a ProjectReference no .csproj — os testes passam a cobri-lo automaticamente.
/// </summary>
public sealed record BoundedContext(string Name, Assembly Domain, Assembly Application, Assembly Infrastructure);

public static class BoundedContexts
{
    public static readonly IReadOnlyList<BoundedContext> All =
    [
        new BoundedContext(
            "Catalog",
            typeof(Catalog.Domain.Aggregates.Job).Assembly,
            typeof(Catalog.Application.Abstractions.ICommand).Assembly,
            typeof(Catalog.Infrastructure.DependencyInjection).Assembly),
        new BoundedContext(
            "Customers",
            typeof(Customers.Domain.Aggregates.Client).Assembly,
            typeof(Customers.Application.Abstractions.ICommand).Assembly,
            typeof(Customers.Infrastructure.DependencyInjection).Assembly),
        new BoundedContext(
            "Inventory",
            typeof(Inventory.Domain.Aggregates.Part).Assembly,
            typeof(Inventory.Application.Abstractions.ICommand).Assembly,
            typeof(Inventory.Infrastructure.DependencyInjection).Assembly),
    ];

    public static TheoryData<BoundedContext> AsTheoryData()
    {
        var data = new TheoryData<BoundedContext>();
        foreach (var bc in All) data.Add(bc);
        return data;
    }
}
