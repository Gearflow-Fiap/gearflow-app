# Documentação — GearFlow (gearflow-app)

> **Fonte única de verdade** sobre arquitetura, decisões e convenções. Comece por aqui.
> A visão para agentes/contribuidores vive em [`../CLAUDE.md`](../CLAUDE.md).

O **GearFlow** é a plataforma de gestão de **oficina mecânica**: clientes/veículos, catálogo de
serviços, estoque de peças e insumos, e o ciclo de vida da **Ordem de Serviço (OS)** — do
recebimento à entrega, passando por diagnóstico, orçamento e execução.

Este repositório é a **aplicação principal** (repo #4 da Fase 3), reestruturada de uma Clean
Architecture em camadas para uma organização por **Bounded Contexts** (monólito modular).
Ver [ADR-001](architecture/adr-001-modular-monolith-bounded-contexts.md).

---

## Comece por aqui

| Se você quer… | Leia |
|---|---|
| Entender a arquitetura geral | [ADR-001 — Monólito modular por Bounded Contexts](architecture/adr-001-modular-monolith-bounded-contexts.md) |
| Revisar os Bounded Contexts e como conversam | [BOUNDED_CONTEXTS.md](architecture/BOUNDED_CONTEXTS.md) |
| Ver os diagramas (componentes + sequência) | [ARCHITECTURE_DIAGRAMS.md](architecture/ARCHITECTURE_DIAGRAMS.md) |
| Entender a escolha do banco + modelo ER | [RFC-001 — Escolha do banco de dados](rfcs/rfc-001-escolha-do-banco-de-dados.md) |
| Entender nuvem + gateway | [RFC-002 — Nuvem e API Gateway](rfcs/rfc-002-nuvem-e-api-gateway.md) |
| Entender a estratégia de autenticação | [RFC-003](rfcs/rfc-003-estrategia-de-autenticacao.md) · [ADR-003](architecture/adr-003-authentication-strategy.md) |
| Ler o enunciado oficial da Fase 3 | [tech-challenge/13SOAT-Fase-3-Tech-Challenge.pdf](tech-challenge/13SOAT-Fase-3-Tech-Challenge.pdf) |
| Rodar/testar localmente | [`../README.md`](../README.md) |
| Ver todos os endpoints (paridade com o legado) | [development/API_ENDPOINTS.md](development/API_ENDPOINTS.md) |
| Entender observabilidade (Fase 3) | [OBSERVABILITY.md](architecture/OBSERVABILITY.md) |
| Entender cobertura de testes | [development/TEST_COVERAGE.md](development/TEST_COVERAGE.md) |

---

## Architecture Decision Records (ADRs)

Decisões arquiteturais **permanentes**. Formato em [`architecture/adr-template.md`](architecture/adr-template.md).

| ADR | Título | Status |
|---|---|---|
| [ADR-001](architecture/adr-001-modular-monolith-bounded-contexts.md) | Monólito modular por Bounded Contexts | Accepted |
| [ADR-002](architecture/adr-002-cross-bc-communication.md) | Padrão de comunicação cross-BC (portas + eventos in-process) | Accepted |
| [ADR-003](architecture/adr-003-authentication-strategy.md) | Estratégia de autenticação (staff JWT + Lambda CPF) | Accepted |
| [ADR-004](architecture/adr-004-kubernetes-scalability-hpa.md) | Escalabilidade no Kubernetes (HPA) | Accepted |

## RFCs (Request for Comments)

Discussão de decisões técnicas relevantes (escolha de nuvem, banco, auth). Formato em
[`rfcs/rfc-template.md`](rfcs/rfc-template.md).

| RFC | Título | Status |
|---|---|---|
| [RFC-001](rfcs/rfc-001-escolha-do-banco-de-dados.md) | Escolha do banco de dados (SQL Server gerenciado) + modelo ER | Draft |
| [RFC-002](rfcs/rfc-002-nuvem-e-api-gateway.md) | Escolha da nuvem, implantação e API Gateway (YARP) | Accepted |
| [RFC-003](rfcs/rfc-003-estrategia-de-autenticacao.md) | Estratégia de autenticação (staff JWT + CPF serverless) | Draft |

---

## Bounded Contexts

| BC | Responsabilidade | Agregados |
|---|---|---|
| **Identity** | Autenticação de staff, tokens, rotação de credencial | `User`, `RefreshToken` |
| **Customers** | Clientes (CPF/CNPJ) e seus veículos | `Client`, `Vehicle` |
| **Catalog** | Catálogo de serviços de mão de obra | `Job` (Serviço) |
| **Inventory** | Peças e insumos; reserva/consumo bifásico; alerta de estoque mínimo | `Part`, `Consumable` |
| **Workshop** | Ciclo de vida da OS + orçamento (core do domínio) | `ServiceOrder`, `Budget` |
| **Notifications** | E-mail ao cliente/estoquista; registro de notificações | `Notification` |

Revisão completa (agregados, endpoints, matriz de comunicação, classificação core/supporting/generic):
[BOUNDED_CONTEXTS.md](architecture/BOUNDED_CONTEXTS.md). Fluxo e regras:
[ADR-001](architecture/adr-001-modular-monolith-bounded-contexts.md),
[ADR-002](architecture/adr-002-cross-bc-communication.md) e
[ARCHITECTURE_DIAGRAMS.md](architecture/ARCHITECTURE_DIAGRAMS.md).

---

## Estrutura de pastas de `docs/`

```
docs/
  INDEX.md                     # este arquivo — mapa da documentação
  architecture/
    adr-template.md            # template de ADR
    adr-001..004-*.md          # decisões arquiteturais permanentes
    BOUNDED_CONTEXTS.md        # revisão dos BCs + matriz de comunicação
    ARCHITECTURE_DIAGRAMS.md   # componentes + sequência + estados (Mermaid)
    OBSERVABILITY.md           # métricas, logs, traces, dashboards (Fase 3)
  rfcs/
    rfc-template.md            # template de RFC
    rfc-001..003-*.md          # decisões técnicas em discussão (banco, nuvem, auth)
  development/
    TEST_COVERAGE.md           # modelo de cobertura + gate ratchet
```

## Os 4 repositórios da Fase 3

Este é o repo da **aplicação principal**. Os demais (planejados/scaffold) e seus links de deploy:

| # | Repositório | Propósito |
|---|---|---|
| 1 | `gearflow-auth-lambda` | Function serverless: valida CPF → JWT |
| 2 | `gearflow-infra-k8s` | Terraform do cluster Kubernetes (HPA, observabilidade) |
| 3 | `gearflow-infra-db` | Terraform do banco gerenciado (SQL Server) |
| 4 | `gearflow-app` (**este**) | Aplicação .NET rodando no Kubernetes |
