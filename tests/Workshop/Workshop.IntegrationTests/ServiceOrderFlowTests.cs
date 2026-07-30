using Catalog.Application.UseCases.CreateJob;
using Customers.Application.DTOs;
using Customers.Application.UseCases.AddVehicle;
using Customers.Application.UseCases.CreateClient;
using FluentAssertions;
using Identity.Application.UseCases.LoginUser;
using Identity.Application.UseCases.RegisterUser;
using Inventory.Application.UseCases.CreatePart;
using Inventory.Application.UseCases.GetParts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Domain.Aggregates;
using Notifications.Infrastructure.Persistence;
using Shared.Domain.Primitives;
using Workshop.Application.DTOs;
using Workshop.Application.UseCases.GetServiceOrders;
using Workshop.Application.UseCases.ApproveBudget;
using Workshop.Application.UseCases.CreateServiceOrder;
using Workshop.Application.UseCases.DeliverServiceOrder;
using Workshop.Application.UseCases.FinalizeDiagnostic;
using Workshop.Application.UseCases.FinalizeServiceOrder;
using Workshop.Application.UseCases.GetServiceOrderById;
using Workshop.Application.UseCases.StartDiagnostic;
using Workshop.IntegrationTests.Fixtures;

namespace Workshop.IntegrationTests;

[Collection(nameof(GearFlowDatabaseCollection))]
public sealed class ServiceOrderFlowTests
{
    private const string ValidCpf = "52998224725";
    private readonly GearFlowDatabaseFixture _fixture;

    public ServiceOrderFlowTests(GearFlowDatabaseFixture fixture) => _fixture = fixture;

    private async Task<TResult> Send<TResult>(IRequest<TResult> request)
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(request);
    }

    [Fact]
    public async Task Full_service_order_lifecycle_reserves_and_consumes_stock()
    {
        // Catálogo + estoque
        var job = (await Send(new CreateJobCommand("Troca de óleo", "com filtro", 15000))).Value;
        var part = (await Send(new CreatePartCommand("Filtro de óleo", "", "PN-1", "Bosch", 5000, 100))).Value;

        // Cliente + veículo
        var address = new AddressDto("Rua 1", "Cidade", "SP", "Brasil", "01000-000");
        var client = (await Send(new CreateClientCommand(ValidCpf, null, "João", "joao@x.com", "119", address))).Value;
        var vehicle = (await Send(new AddVehicleCommand(client.Id, "ABC1D23", "Fiat", "Uno", "Prata", 2015, 2016))).Value;

        // Abre OS com 1 serviço + 2 unidades da peça
        var created = await Send(new CreateServiceOrderCommand(
            vehicle.Id, new[] { job.Id }, new[] { new RequestedPartInput(part.Id, 2) }));
        created.IsSuccess.Should().BeTrue();
        var osId = created.Value.Id;
        created.Value.Status.Should().Be("Received");

        // Ciclo de vida
        (await Send(new StartDiagnosticCommand(osId))).IsSuccess.Should().BeTrue();
        (await Send(new FinalizeDiagnosticCommand(osId, Array.Empty<DiagnosticConsumableInput>()))).IsSuccess.Should().BeTrue();
        (await Send(new ApproveBudgetCommand(osId))).IsSuccess.Should().BeTrue();   // reserva 2 peças
        (await Send(new FinalizeServiceOrderCommand(osId))).IsSuccess.Should().BeTrue(); // consome
        (await Send(new DeliverServiceOrderCommand(osId))).IsSuccess.Should().BeTrue();

        // Estado final da OS
        var final = (await Send(new GetServiceOrderByIdQuery(osId))).Value;
        final.Status.Should().Be("Delivered");
        final.History.Select(h => h.Status).Should().Contain("Delivered");

        // Estoque: reservou 2 (100→98) e consumiu (baixou a reserva). Disponível=98, reservado=0.
        var parts = (await Send(new GetPartsQuery())).Value;
        var updated = parts.Single(p => p.Id == part.Id);
        updated.Quantity.Should().Be(98);
        updated.ReservedQuantity.Should().Be(0);

        // Notificações in-process foram gravadas (orçamento gerado + aprovado).
        using (var scope = _fixture.CreateScope())
        {
            var ndb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var notifications = await ndb.Notifications.AsNoTracking().ToListAsync();
            notifications.Should().Contain(n => n.Type == NotificationType.Budget);
        }

        // A listagem paginada por prioridade não inclui OS entregues (Delivered).
        var paged = (await Send(new GetServiceOrdersQuery(1, 10))).Value;
        paged.PageSize.Should().Be(10);
        paged.Items.Should().NotContain(o => o.Id == osId);
    }

    [Fact]
    public async Task Approve_budget_without_stock_moves_order_to_awaiting_parts()
    {
        var job = (await Send(new CreateJobCommand("Alinhamento", "", 8000))).Value;
        var part = (await Send(new CreatePartCommand("Peça rara", "", "PN-RARE", "OEM", 20000, 1))).Value; // só 1 em estoque

        var address = new AddressDto("Rua 2", "Cidade", "SP", "Brasil", "02000-000");
        var client = (await Send(new CreateClientCommand(ValidCpf, null, "Maria", "maria@x.com", "119", address))).Value;
        var vehicle = (await Send(new AddVehicleCommand(client.Id, "XYZ9W88", "VW", "Gol", "Preto", 2018, 2019))).Value;

        var osId = (await Send(new CreateServiceOrderCommand(
            vehicle.Id, new[] { job.Id }, new[] { new RequestedPartInput(part.Id, 5) }))).Value.Id; // pede 5, tem 1

        await Send(new StartDiagnosticCommand(osId));
        await Send(new FinalizeDiagnosticCommand(osId, Array.Empty<DiagnosticConsumableInput>()));
        (await Send(new ApproveBudgetCommand(osId))).IsSuccess.Should().BeTrue();

        var final = (await Send(new GetServiceOrderByIdQuery(osId))).Value;
        final.Status.Should().Be("AwaitingPartsOrConsumables");
    }

    [Fact]
    public async Task Register_then_login_succeeds_and_wrong_password_fails()
    {
        (await Send(new RegisterUserCommand("staff@gearflow.com", "staff", "Secret123"))).IsSuccess.Should().BeTrue();

        var login = await Send(new LoginUserCommand("staff@gearflow.com", "Secret123", null));
        login.IsSuccess.Should().BeTrue();
        login.Value.AccessToken.Should().NotBeNullOrEmpty();

        var wrong = await Send(new LoginUserCommand("staff@gearflow.com", "WrongPass", null));
        wrong.IsFailure.Should().BeTrue();
        wrong.Error.Type.Should().Be(ErrorType.Unauthorized);
    }
}
