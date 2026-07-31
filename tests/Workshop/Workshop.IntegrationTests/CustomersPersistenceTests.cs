using Customers.Application.DTOs;
using Customers.Application.UseCases.AddVehicle;
using Customers.Application.UseCases.CreateClient;
using Customers.Application.UseCases.GetClientById;
using Customers.Application.UseCases.UpdateVehicle;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Workshop.IntegrationTests.Fixtures;

namespace Workshop.IntegrationTests;

/// <summary>
/// Persistência do Customers contra o SQL Server real: cliente + veículo (filho de um agregado já
/// persistido) e atualização. Cobre a mesma classe de bug de child-key (INSERT tratado como UPDATE)
/// que apareceu no <c>Vehicle</c>.
/// </summary>
[Collection(nameof(GearFlowDatabaseCollection))]
public sealed class CustomersPersistenceTests
{
    private const string ValidCpf = "52998224725";
    private readonly GearFlowDatabaseFixture _fixture;

    public CustomersPersistenceTests(GearFlowDatabaseFixture fixture) => _fixture = fixture;

    private async Task<TResult> Send<TResult>(IRequest<TResult> request)
    {
        using var scope = _fixture.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    [Fact]
    public async Task Client_and_vehicle_persist_and_can_be_updated()
    {
        var address = new AddressDto("Rua A", "Cidade", "SP", "Brasil", "01000-000");
        var client = (await Send(new CreateClientCommand(ValidCpf, null, "Ana", "ana@x.com", "119", address))).Value;

        // Adiciona um veículo a um cliente JÁ persistido — o INSERT do filho apareceria como
        // DbUpdateException ("0 rows") se a chave do filho não estivesse configurada corretamente.
        var vehicle = (await Send(new AddVehicleCommand(client.Id, "ABC1D23", "Fiat", "Uno", "Prata", 2015, 2016))).Value;

        var loaded = (await Send(new GetClientByIdQuery(client.Id))).Value;
        loaded.Vehicles.Should().ContainSingle(v => v.Id == vehicle.Id && v.LicensePlate == "ABC1D23");

        // Atualiza o veículo e relê do banco.
        (await Send(new UpdateVehicleCommand(vehicle.Id, "XYZ9W88", "Fiat", "Uno", "Preto", 2015, 2016)))
            .IsSuccess.Should().BeTrue();

        var reloaded = (await Send(new GetClientByIdQuery(client.Id))).Value;
        reloaded.Vehicles.Single().Color.Should().Be("Preto");
        reloaded.Vehicles.Single().LicensePlate.Should().Be("XYZ9W88");
    }
}
