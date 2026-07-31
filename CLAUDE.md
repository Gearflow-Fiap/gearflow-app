# GearFlow — Contexto para agentes de IA e contribuidores

> **Fonte única de verdade** sobre arquitetura, layout e convenções. Ao mudar uma convenção,
> atualize **este** arquivo e o ADR correspondente no mesmo commit.

## Visão geral

Plataforma de gestão de **oficina mecânica**: clientes/veículos, catálogo de serviços, estoque de
peças/insumos e o ciclo de vida da **Ordem de Serviço (OS)**. Stack: **ASP.NET Core 10 / .NET 10**,
**SQL Server**, EF Core 10, MediatR (CQRS), xUnit.

Este é o repo da **aplicação principal** (repo #4 da Fase 3), reorganizado de Clean Architecture em
camadas para **Bounded Contexts** (monólito modular).
Comece por [`docs/INDEX.md`](docs/INDEX.md) e [ADR-001](docs/architecture/adr-001-modular-monolith-bounded-contexts.md).

## Layout do repositório

```
src/
  Shared/
    Shared.Domain/          # primitivas puras: Entity, AggregateRoot, ValueObject, Result, IDomainEvent, Actor
    Shared.Infrastructure/  # cross-cutting (EF-free): ProblemDetails, IEndpoint, JWT, observabilidade, Serilog, health
    Shared.Contracts/       # contratos de integration events entre BCs (in-process)
  Gateway/                  # YARP — entrada única, CORS + rate limiting
  Identity/                 # BC: User + RefreshToken, login staff, JWT
  Customers/                # BC: Client (CPF/CNPJ) + Vehicle
  Catalog/                  # BC: Job (serviços de mão de obra)
  Inventory/                # BC: Part + Consumable, estoque bifásico, low-stock
  Workshop/                 # BC (core): ServiceOrder + Budget — máquina de estados
  Notifications/            # BC: Notification + e-mail
  Host/GearFlow.Api/        # host único — mapeia os IEndpoint de todos os BCs
tests/
  <BC>/<BC>.UnitTests
  Workshop/Workshop.IntegrationTests   # WebApplicationFactory + SQL Server (Testcontainers)
  Architecture/Architecture.Tests      # regras de camada/BC (NetArchTest) — gate de CI
```

## Regras de arquitetura (fazer valer)

### Direção de dependência — nunca violar
```
Api → Application → Domain
Infrastructure → Application → Domain
Domain: ZERO dependências externas (exceto MediatR.Contracts — desvio consciente para IDomainEvent)
```

### Primitivas de Shared.Domain
- Entidades estendem `Entity<TId>`; aggregates estendem `AggregateRoot<TId>`.
- Value objects estendem `ValueObject` (igualdade por componentes, imutável).
- Falha de regra usa `Result<T>`/`Error` — **nunca** lançar exceção para regra de negócio.
- Ator de uma ação: `Actor` (System/Staff/Customer) via `ICurrentActor` em handler HTTP.

### CQRS
- Commands (`ICommand<T>` → `ICommandHandler`) para escrita; Queries para leitura.
- Handlers em `*.Application/UseCases/<Feature>/` e **`internal sealed`** — nunca public.
- **Endpoint não injeta DbContext nem repositório** — sempre `ISender` + use case. O endpoint só
  traduz HTTP → command/query → HTTP (`ToOk()`/`ToProblem()`).
- **Todo endpoint declara `.WithSummary(...)` e `.WithDescription(...)`** (documentação como contrato).
  Travado por `EndpointDocumentationTests` (Architecture.Tests) — sem summary/description o CI quebra.

### Erro HTTP — ProblemDetails centralizado (RFC 9457)
- **Nunca** montar `Results.BadRequest/NotFound` à mão. O endpoint faz `return result.ToOk();`.
- Status vem do `ErrorType` (ou inferido pelo sufixo do `Code`). Ver
  `Shared.Infrastructure/Http/ResultExtensions.cs`.

### Comunicação cross-BC
- Leitura/ação em outro BC: **porta de Application** (ex.: `IInventoryReservation`), impl no BC dono,
  registrada na DI do host.
- Reação assíncrona: **evento de integração in-process** (MediatR), contrato em `Shared.Contracts`.
- Preservar a **máquina de estados da OS** e o fluxo **reserva-na-aprovação / consumo-na-finalização**
  exatamente como no GearFlow original.

### Persistência
- **SQL Server**, single-tenant. Cada BC tem seu `DbContext`. `MigrateAsync()` no startup (idempotente).
- Dinheiro em inteiro de centavos onde aplicável; preços **snapshotted** em orçamento/OS.

### Gateway & observabilidade
- CORS e rate limiting vivem **só** no Gateway.
- `AddObservability`/`MapObservability` + `AddSerilogLogging` no `Program.cs`. Ver
  [OBSERVABILITY.md](docs/architecture/OBSERVABILITY.md).

## Convenções
- **Central Package Management**: versões só em `Directory.Packages.props`.
- `TreatWarningsAsErrors=true` — warnings CS são erros. Conserte.
- Handlers e impls de Infrastructure são `internal sealed`.
- Documentação no mesmo commit: mudança estrutural ou que implemente/contradiga um ADR atualiza o
  ADR, este arquivo e o `README.md`.

## Sempre / nunca
**Sempre**: consultar este arquivo e `docs/INDEX.md`; preservar as regras de negócio; testar domínio
com FluentAssertions sem mocks.
**Nunca**: importar Infrastructure no Domain; tornar handler public; lançar exceção para regra de
negócio; adicionar CORS/rate limiting por BC.

**Ver também**: [`docs/INDEX.md`](docs/INDEX.md) · [ADR-001](docs/architecture/adr-001-modular-monolith-bounded-contexts.md) · [ARCHITECTURE_DIAGRAMS](docs/architecture/ARCHITECTURE_DIAGRAMS.md) · [RFC-001](docs/rfcs/rfc-001-escolha-do-banco-de-dados.md)
