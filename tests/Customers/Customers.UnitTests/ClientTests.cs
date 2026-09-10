using Customers.Domain.Aggregates;
using Customers.Domain.Enums;
using Customers.Domain.ValueObjects;
using FluentAssertions;

namespace Customers.UnitTests;

public sealed class ClientTests
{
    private static Address AnAddress() => new("Rua 1", "Cidade", "SP", "Brasil", "01000-000");

    // CPF/CNPJ válidos gerados para teste (dígitos verificadores corretos).
    private const string ValidCpf = "52998224725";
    private const string ValidCnpj = "11444777000161";

    [Fact]
    public void Create_with_valid_cpf_succeeds_and_normalizes_digits()
    {
        var result = Client.Create("529.982.247-25", null, "João", "joao@x.com", "119", AnAddress());

        result.IsSuccess.Should().BeTrue();
        result.Value.Cpf!.Value.Should().Be(ValidCpf);
        result.Value.Cnpj.Should().BeNull();
    }

    [Fact]
    public void Create_with_valid_cnpj_succeeds()
    {
        var result = Client.Create(null, ValidCnpj, "Empresa", "corp@x.com", "119", AnAddress());

        result.IsSuccess.Should().BeTrue();
        result.Value.Cnpj!.Value.Should().Be(ValidCnpj);
    }

    [Fact]
    public void Create_without_cpf_and_cnpj_fails()
    {
        var result = Client.Create(null, null, "Ninguém", "n@x.com", "119", AnAddress());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Client.DocumentRequired");
    }

    [Fact]
    public void Create_with_invalid_cpf_fails()
    {
        var result = Client.Create("111", null, "Zé", "z@x.com", "119", AnAddress());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Cpf.Invalid");
    }

    [Fact]
    public void Create_with_invalid_email_fails()
    {
        var result = Client.Create(ValidCpf, null, "Zé", "sem-arroba", "119", AnAddress());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.Invalid");
    }

    [Fact]
    public void Create_defaults_to_active_status()
    {
        var client = Client.Create(ValidCpf, null, "Zé", "z@x.com", "119", AnAddress()).Value;

        client.Status.Should().Be(ClientStatus.Active);
    }

    [Fact]
    public void Deactivate_then_activate_toggles_status()
    {
        var client = Client.Create(ValidCpf, null, "Zé", "z@x.com", "119", AnAddress()).Value;

        client.Deactivate();
        client.Status.Should().Be(ClientStatus.Inactive);

        client.Activate();
        client.Status.Should().Be(ClientStatus.Active);
    }

    [Fact]
    public void AddVehicle_appends_to_collection()
    {
        var client = Client.Create(ValidCpf, null, "Zé", "z@x.com", "119", AnAddress()).Value;

        client.AddVehicle("ABC1D23", "Fiat", "Uno", "Prata", 2015, 2016, Guid.NewGuid(), DateTime.UtcNow);

        client.Vehicles.Should().HaveCount(1);
        client.Vehicles.First().LicensePlate.Should().Be("ABC1D23");
    }
}
