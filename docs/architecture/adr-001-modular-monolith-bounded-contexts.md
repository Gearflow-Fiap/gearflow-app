# ADR-001: Monólito modular por Bounded Contexts

**Status**: Accepted
**Date**: 2026-07-30
**Decided by**: Time GearFlow (refatoração Fase 3)

## Context

O GearFlow nasceu como uma Clean Architecture em quatro camadas físicas
(`Domain / Application / Infrastructure / Api`) num único conjunto de projetos. À medida que o
domínio cresceu (OS, orçamento, estoque, clientes, catálogo, identidade, notificações), a camada
`Domain` virou um saco único de agregados, e a orquestração entre eles concentrou-se em três
domain services entrelaçados — `ServiceOrderDomainService`, `BudgetDomainService`,
`InventoryService` — onde a reserva de estoque está acoplada diretamente à aprovação do orçamento e
à finalização da OS. Isso dificulta testar, evoluir e raciocinar sobre cada capacidade de forma
isolada.

A Fase 3 do Tech Challenge exige, além disso, elevar a aplicação a operação corporativa: API
Gateway, escalabilidade no Kubernetes (HPA), observabilidade e documentação arquitetural. É o
momento de reorganizar.

Restrições que moldam a decisão:

- **Preservar 100% das regras de negócio** existentes (a máquina de estados da OS, o fluxo
  reserva-na-aprovação/consumo-na-finalização, a validação de CPF/CNPJ do cliente).
- **Uma única unidade de deploy** é desejável para o escopo do desafio — operar 6 serviços
  separados no Kubernetes multiplicaria a complexidade de infra e observabilidade sem ganho
  proporcional.
- Precisamos de **fronteiras explícitas** entre capacidades para poder testá-las isoladamente e,
  se necessário, extrair uma delas para um serviço próprio no futuro.
- Adotar padrões maduros e consolidados (kernel `Shared`, CQRS com MediatR,
  `Result<T>`→ProblemDetails, testes de arquitetura como gate de CI).

## Decision

### 1. Organização por Bounded Context, deploy único (monólito modular)

Cada capacidade de negócio é um **Bounded Context (BC)** com seus próprios projetos
`Domain / Application / Infrastructure`, e **um único host** (`GearFlow.Api`) referencia todos e
expõe os endpoints. Um **API Gateway** (YARP) fica à frente.

```
src/
  Shared/            Shared.Domain | Shared.Infrastructure | Shared.Contracts
  Gateway/           YARP — entrada única, CORS + rate limiting
  Identity/          Domain | Application | Infrastructure
  Customers/         Domain | Application | Infrastructure
  Catalog/           Domain | Application | Infrastructure
  Inventory/         Domain | Application | Infrastructure
  Workshop/          Domain | Application | Infrastructure   (core: ServiceOrder + Budget)
  Notifications/     Domain | Application | Infrastructure
  Host/GearFlow.Api/ único deployável — mapeia os IEndpoint de todos os BCs
```

**Why**: mantém as fronteiras de DDD explícitas no código (cada BC compila isolado, com sua própria
direção de dependência) sem pagar o custo operacional de microserviços. O Gateway satisfaz o
requisito da Fase 3 e centraliza CORS/rate limiting.

### 2. Direção de dependência — nunca violar

```
Api → Application → Domain
Infrastructure → Application → Domain
Domain tem ZERO dependências externas (exceto MediatR.Contracts, ver ADR — desvio consciente)
```

Endpoints são **interface adapters**: traduzem HTTP → command/query (`ISender`) → HTTP
(`ToOk()`/`ToProblem()`). **Não injetam DbContext nem repositório.** Handlers de Application são
`internal sealed`. Regras travadas por testes de arquitetura (ver seção Implementação).

### 3. CQRS + `Result<T>` (sem exceção para regra de negócio)

- Commands (`ICommand<T>` → `ICommandHandler`) para escrita; Queries para leitura.
- Falha de regra retorna `Result<T>`/`Error`; a tradução para HTTP (RFC 9457 ProblemDetails) é
  central em `Shared.Infrastructure/Http/ResultExtensions.cs`.
- Substitui o padrão anterior de `IXxxUseCase` manuais + exceções de domínio mapeadas por filtro.

**Why**: torna o fluxo de erro previsível e tipado, e alinha com o kernel `Shared` reaproveitado.

### 4. Comunicação cross-BC: portas + eventos in-process

A tríade entrelaçada é desacoplada assim:

- **Workshop é dono** de `ServiceOrder` e `Budget`. Para reservar/consumir estoque, chama uma
  **porta de Application** (`IInventoryReservation`, interface em `Workshop.Application/Abstractions/`);
  a implementação vive em `Inventory` e é registrada na DI do host.
- **Eventos de integração in-process** (dispatch após `SaveChanges`, via MediatR) cobrem o que é
  reação assíncrona: `LowStockAlertIntegrationEvent` (Inventory → Notifications) e
  `PartsReplenishedIntegrationEvent` (Inventory → Workshop, para auto-resume de OS em
  `AwaitingPartsOrConsumables`). Contratos em `Shared.Contracts`.

**Why**: preserva exatamente as regras do GearFlow, mas remove o acoplamento direto entre serviços
de domínio de BCs diferentes. Como os contratos já existem em `Shared.Contracts`, extrair um BC
para um serviço próprio depois é trocar o transporte (in-process → broker), não reescrever o
publisher/subscriber.

### 5. Persistência: SQL Server, single-tenant

O banco continua **SQL Server** (ver [RFC-001](../rfcs/rfc-001-escolha-do-banco-de-dados.md)). Cada
BC tem seu `DbContext` na sua `Infrastructure`. **Não** há multi-tenancy: "múltiplas unidades" da
oficina é tratado como escala de infra (réplicas/HPA), não isolamento de dados.

## Consequences

### Positive
- Fronteiras de BC explícitas e testáveis; cada capacidade evolui isolada.
- Uma unidade de deploy simples de operar no Kubernetes.
- Regras de negócio preservadas, agora desacopladas via portas/eventos.
- Kernel e convenções maduros já consolidados (menos decisão do zero).

### Negative
- Um monólito ainda compartilha processo e banco: uma migração descuidada pode reacoplar BCs.
- Eventos in-process não têm durabilidade de um broker (aceitável no escopo; ver ADR-002 planejado).

### Mitigations
- Testes de arquitetura (`NetArchTest`) barram violação de camada/BC no CI.
- Contratos de integração isolados em `Shared.Contracts` para permitir extração futura
  (detalhado em [ADR-002](adr-002-cross-bc-communication.md)).

## Alternatives Considered

1. **Microserviços por BC (um Api por BC + YARP)** — isolamento máximo por BC, mas 6+
   serviços para deployar, observar e versionar. Custo desproporcional ao escopo do desafio.
2. **Manter a Clean Architecture em camadas atual** — não resolve o entrelaçamento dos domain
   services nem cria fronteiras de capacidade; contraria o objetivo da refatoração.
3. **Multi-tenant por unidade (schema/coluna)** — mudaria modelo e queries de forma invasiva sem
   requisito de isolamento de dados real.

## Related

- [RFC-001 — Escolha do banco de dados](../rfcs/rfc-001-escolha-do-banco-de-dados.md)
- [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md)
- [BOUNDED_CONTEXTS.md](BOUNDED_CONTEXTS.md) — revisão dos BCs + matriz de comunicação
- [ADR-002](adr-002-cross-bc-communication.md) — Padrão de comunicação cross-BC
- [ADR-003](adr-003-authentication-strategy.md) — Estratégia de autenticação (staff JWT + Lambda CPF)
- [ADR-004](adr-004-kubernetes-scalability-hpa.md) — Escalabilidade no Kubernetes (HPA)
