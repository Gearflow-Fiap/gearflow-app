# ADR-002: Padrão de comunicação entre Bounded Contexts

**Status**: Accepted
**Date**: 2026-07-31
**Related**: [ADR-001](adr-001-modular-monolith-bounded-contexts.md)

## Context

No monólito modular (ADR-001) os BCs vivem no mesmo processo e no mesmo banco (um schema por BC).
Precisamos de uma forma de um BC **ler** dado de outro e **reagir** a fatos de outro sem:

- acoplar os projetos (`Workshop.Application` não pode referenciar `Inventory.*`), o que reabriria o
  entrelaçamento que a refatoração desfez;
- introduzir HTTP interno (latência e complexidade desnecessárias num único processo);
- perder a possibilidade de extrair um BC para um serviço próprio no futuro.

Os pontos de integração reais são: aprovar orçamento → **reservar estoque**; finalizar OS →
**consumir estoque**; gerar orçamento → **e-mail ao cliente** (precisa do contato, que é do Customers);
repor estoque → **retomar OS** parada; e vários **avisos** (orçamento gerado/aprovado, falta de
estoque, estoque mínimo).

## Decision

### 1. Leitura/ação síncrona cross-BC → porta de Application + SQL cru (padrão ADR-011)

Quando um BC precisa **ler** ou **agir** sobre dado de outro de forma síncrona, ele declara uma
**porta** (interface) na sua própria `Application/Abstractions/`; a implementação vive na sua
`Infrastructure` e acessa o schema do outro BC por **SQL cru** (mesmo banco), sem referência de projeto.

| Porta (em `Workshop.Application`) | Implementação | Lê/escreve |
|---|---|---|
| `IInventoryReservation` | `SqlInventoryReservation` | `inventory.parts` / `inventory.consumables` (reserva/consumo transacional) |
| `IPricingReader` | `SqlPricingReader` | `catalog.jobs` / `inventory.*` (preço p/ snapshot do orçamento) |
| `ICustomerContactReader` | `SqlCustomerContactReader` | `customers.vehicles` → `customers.clients` (contato p/ e-mail) |

**Why**: o BC dono continua dono das suas tabelas; o consumidor depende de uma abstração pequena e
testável. É o mesmo padrão de leitura cross-BC do `delivery-app` (ADR-011), estendido a uma escrita
controlada (reserva/consumo de estoque) que é a única ação cross-BC transacional.

### 2. Reação assíncrona cross-BC → evento de integração in-process (MediatR)

Fatos que geram reação em outro BC são **eventos de integração** — contratos em `Shared.Contracts`
(`IIntegrationEvent : INotification`), publicados via `IPublisher` (MediatR) e despachados
**in-process** para os handlers registrados no host.

| Evento (`Shared.Contracts`) | Publicado por | Consumido por | Efeito |
|---|---|---|---|
| `BudgetGeneratedIntegrationEvent` | Workshop (finaliza diagnóstico) | Notifications | E-mail ao cliente + registro |
| `BudgetApprovedIntegrationEvent` | Workshop (aprova orçamento) | Notifications | Aviso à oficina |
| `StockMissingIntegrationEvent` | Workshop (aprova sem estoque) | Notifications | Aviso ao estoquista |
| `LowStockAlertIntegrationEvent` | Workshop (consumo na finalização) | Notifications | Aviso de estoque mínimo |
| `PartsReplenishedIntegrationEvent` | Inventory (reposição de estoque) | Workshop | Auto-resume de OS aguardando peças |

Cada `AddXxxInfrastructure` registra o assembly do BC no MediatR; o host acumula todos, então o
publisher alcança handlers de qualquer BC. Vocabulário compartilhado (ex.: `InventoryItemType`) vive
em `Shared.Contracts`.

**Why**: desacopla o publicador do consumidor. O contrato já está isolado em `Shared.Contracts`; se um
BC virar serviço próprio, troca-se o transporte (in-process → broker) sem mudar publicador/handler.

### 3. Nada de HTTP entre BCs; nada de referência de projeto cross-BC

`{BC}.Domain`/`{BC}.Application` de um BC **nunca** referenciam outro BC. Travado pelos testes de
arquitetura (`LayeringTests`).

## Consequences

### Positive
- Fronteiras explícitas e testáveis; cada BC evolui isolado.
- Sem latência/serialização de HTTP interno; transação local quando preciso (reserva de estoque).
- Caminho de extração de um BC preservado (contratos em `Shared.Contracts`).

### Negative
- Eventos in-process **não são duráveis**: se o processo cair no meio, o handler não roda de novo
  (não há fila). Aceitável no escopo; ver Mitigações.
- Escrita cross-BC por SQL cru (reserva/consumo) acopla o Workshop ao **schema** do Inventory (não ao
  código) — uma mudança de coluna do Inventory exige atenção ao reader.

### Mitigations
- Efeitos críticos e transacionais (reserva/consumo) são **síncronos via porta**, na mesma requisição
  — não dependem de evento. Os eventos cobrem só reações não-críticas (avisos, auto-resume, que também
  pode ser disparado manualmente pelo endpoint de retomar execução).
- Ao extrair um BC, o outbox/broker entra no lugar do dispatch in-process (padrão já conhecido do
  `delivery-app`).

## Alternatives Considered

1. **HTTP entre BCs** — latência e complexidade num único processo; rejeitado.
2. **Referência direta de projeto** — reintroduz o acoplamento que a refatoração removeu; barrado por teste.
3. **Broker (RabbitMQ) já agora** — durabilidade real, mas infra extra sem necessidade no monólito;
   fica como evolução natural ao extrair um BC.

## Related
- [ADR-001](adr-001-modular-monolith-bounded-contexts.md) — Monólito modular
- [BOUNDED_CONTEXTS.md](BOUNDED_CONTEXTS.md) — matriz de comunicação por BC
- [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md) §5 — diagrama de eventos
