using Catalog.Domain.Aggregates;
using FluentAssertions;

namespace Catalog.UnitTests;

public sealed class JobTests
{
    private static readonly DateTime Now = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_with_valid_data_succeeds()
    {
        var result = Job.Create("Troca de óleo", "Inclui filtro", 15000, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Troca de óleo");
        result.Value.PriceCents.Should().Be(15000);
        result.Value.CreatedOn.Should().Be(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_without_name_fails_validation(string name)
    {
        var result = Job.Create(name, "desc", 100, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Job.NameRequired");
    }

    [Fact]
    public void Create_with_negative_price_fails_validation()
    {
        var result = Job.Create("Serviço", "desc", -1, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Job.PriceNegative");
    }

    [Fact]
    public void Update_changes_fields_when_valid()
    {
        var job = Job.Create("Antigo", "d", 100, Now).Value;

        var result = job.Update("Novo", "nova desc", 200);

        result.IsSuccess.Should().BeTrue();
        job.Name.Should().Be("Novo");
        job.PriceCents.Should().Be(200);
    }
}
