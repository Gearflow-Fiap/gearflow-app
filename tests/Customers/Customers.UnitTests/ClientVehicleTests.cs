using Customers.Domain.Aggregates;
using Customers.Domain.ValueObjects;
using FluentAssertions;

namespace Customers.UnitTests;

/// <summary>
/// Ramos do agregado <see cref="Client"/> não cobertos por ClientTests: CNPJ inválido no Create,
/// UpdateInformations (sucesso/nome/e-mail), RemoveVehicle e Vehicle.UpdateDetails.
/// </summary>
public sealed class ClientVehicleTests
{
    private static Address AnAddress() => new("Rua 1", "Cidade", "SP", "Brasil", "01000-000");
    private const string ValidCpf = "52998224725";

    private static Client AClient() =>
        Client.Create(ValidCpf, null, "Zé", "z@x.com", "119", AnAddress()).Value;

    [Fact]
    public void Create_with_invalid_cnpj_fails() =>
        Client.Create(null, "111", "Empresa", "corp@x.com", "119", AnAddress()).Error.Code.Should().Be("Cnpj.Invalid");

    [Fact]
    public void Create_without_name_fails() =>
        Client.Create(ValidCpf, null, "  ", "z@x.com", "119", AnAddress()).Error.Code.Should().Be("Client.NameRequired");

    [Fact]
    public void UpdateInformations_updates_on_success()
    {
        var client = AClient();
        var newAddress = new Address("Rua 2", "Outra", "RJ", "Brasil", "20000-000");

        var result = client.UpdateInformations("Maria", "maria@x.com", "219", newAddress);

        result.IsSuccess.Should().BeTrue();
        client.Name.Should().Be("Maria");
        client.Email.Value.Should().Be("maria@x.com");
        client.Address.Should().Be(newAddress);
    }

    [Fact]
    public void UpdateInformations_without_name_fails() =>
        AClient().UpdateInformations(" ", "maria@x.com", "219", AnAddress()).Error.Code.Should().Be("Client.NameRequired");

    [Fact]
    public void UpdateInformations_with_invalid_email_fails() =>
        AClient().UpdateInformations("Maria", "sem-arroba", "219", AnAddress()).Error.Code.Should().Be("Email.Invalid");

    [Fact]
    public void RemoveVehicle_removes_matching_and_is_noop_when_absent()
    {
        var client = AClient();
        var vehicle = client.AddVehicle("ABC1D23", "Fiat", "Uno", "Prata", 2015, 2016, Guid.NewGuid(), DateTime.UtcNow);

        client.RemoveVehicle(VehicleId.New());   // inexistente → no-op
        client.Vehicles.Should().HaveCount(1);

        client.RemoveVehicle(vehicle.Id);        // existente → remove
        client.Vehicles.Should().BeEmpty();
    }

    [Fact]
    public void Vehicle_UpdateDetails_overwrites_fields()
    {
        var vehicle = AClient().AddVehicle("ABC1D23", "Fiat", "Uno", "Prata", 2015, 2016, Guid.NewGuid(), DateTime.UtcNow);

        vehicle.UpdateDetails("XYZ9W88", "VW", "Gol", "Preto", 2018, 2019);

        vehicle.LicensePlate.Should().Be("XYZ9W88");
        vehicle.Mark.Should().Be("VW");
        vehicle.YearModel.Should().Be(2019);
    }
}
