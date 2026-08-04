using Catalog.Domain.Aggregates;
using FluentAssertions;

namespace Catalog.UnitTests;

public sealed class JobUpdateTests
{
    private static readonly DateTime Now = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);

    private static Job AJob() => Job.Create("Troca de óleo", "com filtro", 15000, Now).Value;

    [Fact]
    public void Create_with_null_description_defaults_to_empty()
    {
        Job.Create("Alinhamento", null!, 8000, Now).Value.Description.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_without_name_fails(string name)
    {
        AJob().Update(name, "d", 1000).Error.Code.Should().Be("Job.NameRequired");
    }

    [Fact]
    public void Update_with_negative_price_fails()
    {
        AJob().Update("Serviço", "d", -1).Error.Code.Should().Be("Job.PriceNegative");
    }

    [Fact]
    public void Update_trims_and_defaults_null_description()
    {
        var job = AJob();

        var result = job.Update("  Balanceamento  ", null!, 9000);

        result.IsSuccess.Should().BeTrue();
        job.Name.Should().Be("Balanceamento");
        job.Description.Should().BeEmpty();
        job.PriceCents.Should().Be(9000);
    }
}
