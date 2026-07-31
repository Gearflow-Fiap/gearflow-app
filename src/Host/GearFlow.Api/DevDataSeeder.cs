using Catalog.Domain.Aggregates;
using Catalog.Infrastructure.Persistence;
using Customers.Domain.Aggregates;
using Customers.Domain.ValueObjects;
using Customers.Infrastructure.Persistence;
using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Security;
using Workshop.Domain.Aggregates.BudgetModel;
using Workshop.Domain.Aggregates.ServiceOrderModel;
using Workshop.Infrastructure.Persistence;

namespace GearFlow.Api;

/// <summary>
/// Massa de dados fictícios (realistas) para desenvolvimento — serviços, estoque, clientes/veículos e
/// algumas ordens de serviço em estados variados. Idempotente: só semeia se o catálogo estiver vazio.
/// Roda apenas em Development.
/// </summary>
public static class DevDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var now = sp.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        var catalog = sp.GetRequiredService<CatalogDbContext>();
        if (await catalog.Jobs.AnyAsync(ct)) return; // já semeado

        var inventory = sp.GetRequiredService<InventoryDbContext>();
        var customers = sp.GetRequiredService<CustomersDbContext>();
        var workshop = sp.GetRequiredService<WorkshopDbContext>();

        // ---------- Catálogo de serviços ----------
        var jobs = new[]
        {
            Job.Create("Troca de óleo e filtro", "Óleo sintético + filtro de óleo", 12000, now).Value,
            Job.Create("Alinhamento e balanceamento", "Geometria completa 4 rodas", 15000, now).Value,
            Job.Create("Revisão de freios", "Inspeção e regulagem do sistema de freios", 9000, now).Value,
            Job.Create("Troca de pastilhas de freio", "Dianteiras, mão de obra", 18000, now).Value,
            Job.Create("Troca de correia dentada", "Kit correia + tensor", 45000, now).Value,
            Job.Create("Diagnóstico eletrônico", "Leitura de códigos via scanner", 8000, now).Value,
            Job.Create("Troca de bateria", "Substituição e teste de carga", 5000, now).Value,
            Job.Create("Higienização do ar-condicionado", "Limpeza e troca de filtro de cabine", 16000, now).Value,
            Job.Create("Troca de amortecedores", "Par dianteiro, mão de obra", 38000, now).Value,
            Job.Create("Revisão completa 60.000 km", "Revisão de itens de manutenção programada", 55000, now).Value,
            Job.Create("Troca de embreagem", "Kit embreagem, mão de obra", 90000, now).Value,
            Job.Create("Troca de velas de ignição", "Jogo de velas, mão de obra", 12000, now).Value,
        };
        catalog.Jobs.AddRange(jobs);
        await catalog.SaveChangesAsync(ct);

        // ---------- Estoque: peças ----------
        var parts = new[]
        {
            Part.Create("Filtro de óleo", "Motores 1.0 a 1.6", "FL-1234", "Fram", 3500, 40).Value,
            Part.Create("Pastilha de freio dianteira", "HB20 / Onix / Gol", "PF-2201", "Bosch", 12000, 25).Value,
            Part.Create("Kit correia dentada", "Com tensor e polia", "CT-908", "Gates", 22000, 12).Value,
            Part.Create("Bateria 60Ah", "Livre de manutenção", "M60GD", "Moura", 42000, 15).Value,
            Part.Create("Amortecedor dianteiro", "Gol / Fox / Voyage", "AM-330", "Cofap", 24000, 8).Value,
            Part.Create("Vela de ignição", "Iridium", "VI-6", "NGK", 4500, 60).Value,
            Part.Create("Filtro de ar", "Elemento filtrante", "FA-77", "Tecfil", 5500, 30).Value,
            Part.Create("Disco de freio ventilado", "Par dianteiro", "DF-410", "Fremax", 28000, 10).Value,
            Part.Create("Correia do alternador", "Poly-V", "CA-55", "Dayco", 3800, 20).Value,
            Part.Create("Jogo de palhetas", "Par dianteiro", "PL-22", "Bosch", 4000, 4).Value, // abaixo do mínimo (5)
        };
        inventory.Parts.AddRange(parts);

        // ---------- Estoque: insumos ----------
        var consumables = new[]
        {
            Consumable.Create("Óleo 5W30 sintético (L)", 4500, 80m).Value,
            Consumable.Create("Óleo 15W40 semissintético (L)", 3200, 60m).Value,
            Consumable.Create("Fluido de freio DOT4 (500ml)", 2500, 30m).Value,
            Consumable.Create("Aditivo para radiador (L)", 2800, 25m).Value,
            Consumable.Create("Graxa multiuso (kg)", 3500, 15m).Value,
            Consumable.Create("Limpa contatos (spray)", 1900, 20m).Value,
            Consumable.Create("Água desmineralizada (L)", 800, 4m).Value, // abaixo do mínimo (5)
        };
        inventory.Consumables.AddRange(consumables);
        await inventory.SaveChangesAsync(ct);

        // ---------- Clientes + veículos ----------
        var seedBy = Guid.Empty;
        Client Pf(string cpf, string name, string email, string phone, Address addr) =>
            Client.Create(cpf, null, name, email, phone, addr).Value;
        Client Pj(string cnpj, string name, string email, string phone, Address addr) =>
            Client.Create(null, cnpj, name, email, phone, addr).Value;

        var c1 = Pf("75368845081", "João Pedro Almeida", "joao.almeida@email.com", "(11) 98877-1234",
            new Address("Rua das Palmeiras, 120", "São Paulo", "SP", "Brasil", "01234-000"));
        c1.AddVehicle("RIO2A18", "Fiat", "Uno Attractive", "Prata", 2016, 2017, seedBy, now);
        c1.AddVehicle("FGH3D45", "Honda", "Civic EXL", "Preto", 2019, 2019, seedBy, now);

        var c2 = Pf("17926431958", "Mariana Costa Ribeiro", "mariana.ribeiro@email.com", "(21) 99123-4567",
            new Address("Av. Atlântica, 900", "Rio de Janeiro", "RJ", "Brasil", "22010-000"));
        c2.AddVehicle("KLM4E67", "Chevrolet", "Onix LT", "Branco", 2021, 2022, seedBy, now);

        var c3 = Pf("64304488198", "Carlos Eduardo Souza", "carlos.souza@email.com", "(31) 98765-4321",
            new Address("Rua da Bahia, 45", "Belo Horizonte", "MG", "Brasil", "30160-010"));
        c3.AddVehicle("PQR5F89", "Hyundai", "HB20 Comfort", "Vermelho", 2018, 2019, seedBy, now);
        c3.AddVehicle("STU6G01", "Toyota", "Corolla XEI", "Prata", 2020, 2021, seedBy, now);

        var c4 = Pf("28571590800", "Ana Beatriz Fernandes", "ana.fernandes@email.com", "(41) 99988-7766",
            new Address("Rua XV de Novembro, 300", "Curitiba", "PR", "Brasil", "80020-310"));
        c4.AddVehicle("VWX7H23", "Jeep", "Compass Longitude", "Cinza", 2022, 2022, seedBy, now);

        var c5 = Pj("79385786000162", "Transportadora Rota Sul LTDA", "frota@rotasul.com.br", "(51) 3344-5566",
            new Address("Rod. BR-116, km 20", "Porto Alegre", "RS", "Brasil", "91150-000"));
        c5.AddVehicle("YZA8I45", "Fiat", "Strada Working", "Branco", 2021, 2021, seedBy, now);
        c5.AddVehicle("BCD9J67", "Volkswagen", "Saveiro Robust", "Branco", 2020, 2020, seedBy, now);

        var c6 = Pj("55724193000115", "Padaria Pão Quente ME", "contato@paoquente.com.br", "(11) 2233-4455",
            new Address("Rua do Comércio, 88", "Campinas", "SP", "Brasil", "13010-100"));
        c6.AddVehicle("EFG1K89", "Renault", "Kangoo Express", "Prata", 2017, 2018, seedBy, now);

        var clients = new[] { c1, c2, c3, c4, c5, c6 };
        customers.Clients.AddRange(clients);
        await customers.SaveChangesAsync(ct);

        // ---------- Ordens de serviço (estados variados) ----------
        var sys = Actor.System;
        var vehicles = clients.SelectMany(c => c.Vehicles).ToList();

        // Helper: cria uma OS (create + peças/serviços) em memória.
        ServiceOrder NewOrder(Guid vehicleId, Guid[] jobIds, (Guid partId, int qty)[] partsReq, DateTime createdAt)
        {
            var soJobs = jobIds.Select(j => new ServiceOrderJob(j, createdAt));
            var soParts = partsReq.Select(p => new ServiceOrderPart(p.partId, p.qty, createdAt));
            return ServiceOrder.Create(vehicleId, sys, soJobs, soParts, createdAt);
        }

        // 2 recebidas
        var os1 = NewOrder(vehicles[0].Id.Value, [jobs[0].Id.Value, jobs[5].Id.Value], [(parts[0].Id.Value, 1)], now.AddHours(-2));
        var os2 = NewOrder(vehicles[2].Id.Value, [jobs[2].Id.Value], [], now.AddHours(-1));

        // 1 em diagnóstico
        var os3 = NewOrder(vehicles[3].Id.Value, [jobs[3].Id.Value], [(parts[1].Id.Value, 1)], now.AddHours(-5));
        os3.StartDiagnostic(sys, now.AddHours(-5).AddMinutes(10));

        // 1 aguardando aprovação (com orçamento)
        var os4 = NewOrder(vehicles[4].Id.Value, [jobs[1].Id.Value, jobs[8].Id.Value], [(parts[4].Id.Value, 2)], now.AddHours(-8));
        os4.StartDiagnostic(sys, now.AddHours(-8).AddMinutes(15));
        os4.FinalizeDiagnostic(sys, now.AddHours(-8).AddMinutes(40));
        var budget4 = Budget.Create(os4.Id,
            [new BudgetJob(jobs[1].Id.Value, jobs[1].PriceCents, now), new BudgetJob(jobs[8].Id.Value, jobs[8].PriceCents, now)],
            [new BudgetPart(parts[4].Id.Value, parts[4].PriceCents, 2, now)],
            [], now.AddHours(-8).AddMinutes(40));

        // 1 entregue (ciclo completo, com marcos de tempo para o dashboard)
        var t0 = now.AddDays(-1);
        var os5 = NewOrder(vehicles[1].Id.Value, [jobs[0].Id.Value], [(parts[0].Id.Value, 1)], t0);
        os5.StartDiagnostic(sys, t0.AddMinutes(5));
        os5.FinalizeDiagnostic(sys, t0.AddMinutes(25));
        os5.StartExecution(sys, t0.AddMinutes(30));
        os5.Finalize(sys, t0.AddMinutes(95));
        os5.Deliver(sys, t0.AddMinutes(110));
        var budget5 = Budget.Create(os5.Id,
            [new BudgetJob(jobs[0].Id.Value, jobs[0].PriceCents, t0)],
            [new BudgetPart(parts[0].Id.Value, parts[0].PriceCents, 1, t0)],
            [], t0.AddMinutes(25));

        workshop.ServiceOrders.AddRange(os1, os2, os3, os4, os5);
        workshop.Budgets.AddRange(budget4, budget5);
        await workshop.SaveChangesAsync(ct);
    }
}
