# ADR-004: Escalabilidade no Kubernetes (HPA)

**Status**: Accepted
**Date**: 2026-07-31
**Related**: [ADR-001](adr-001-modular-monolith-bounded-contexts.md), [RFC-002](../rfcs/rfc-002-nuvem-e-api-gateway.md)

## Context

A Fase 3 exige **alta disponibilidade e escalabilidade** no Kubernetes. Precisamos decidir como a
aplicação escala e garantir que o design não impeça escala horizontal.

## Decision

### 1. Escala horizontal via HorizontalPodAutoscaler (HPA)

O `gearflow-api` (e o `gateway`) escalam **horizontalmente** por HPA, mirando **CPU 70%**,
`minReplicas: 1`, `maxReplicas: N` (ajustado pelo baseline de carga). Manifesto no repo
`gearflow-infra-k8s` (`hpa.tf`).

### 2. A aplicação é **stateless** — pré-requisito do HPA

- **Sem sessão em memória**: autenticação é por **JWT** (ADR-003); qualquer réplica valida qualquer token.
- **Estado só no banco** (SQL Server gerenciado) e em serviços externos (SMTP). Réplicas não
  compartilham memória.
- **Eventos in-process** (ADR-002) rodam **na réplica que atendeu a requisição** e só tocam o banco
  compartilhado — não há afinidade de instância nem fila local que quebre com múltiplas réplicas.
  O auto-resume de OS também tem gatilho manual (endpoint de retomar execução), então não depende de
  uma réplica específica.

### 3. Probes de saúde para o Kubernetes

- **Liveness** `/health/live` — o processo está vivo?
- **Readiness** `/health/ready` — pronto para tráfego (checa SQL Server).

O HPA usa métricas de CPU (metrics-server); readiness garante que réplicas novas só recebem tráfego
quando o banco responde.

### 4. Banco **não** escala por HPA

O SQL Server é **gerenciado** (RDS/Azure SQL — ver [RFC-001](../rfcs/rfc-001-escolha-do-banco-de-dados.md)),
com escala vertical/replica de leitura fora do HPA. HPA escala **apenas os pods stateless** da aplicação.

## Consequences

### Positive
- Escala elástica sob carga (picos de abertura de OS) sem intervenção manual.
- Réplica nova entra em segundos; readiness evita servir antes do banco pronto.

### Negative
- Muitas réplicas → mais conexões ao banco: o `NpgsqlDataSource`/pool e o `max pool size` precisam ser
  dimensionados junto do `maxReplicas` para não estourar as conexões do SQL Server gerenciado.
- Eventos in-process não duráveis (ADR-002) permanecem — HPA não muda isso.

### Mitigations
- Definir `maxReplicas` e o pool de conexões de forma coerente com o limite do banco.
- Observabilidade (métricas de CPU/mem, latência, healthchecks) alimenta o dimensionamento — ver
  [OBSERVABILITY.md](OBSERVABILITY.md).

## Alternatives Considered

1. **Escala vertical (pods maiores)** — teto rígido e sem elasticidade; HPA horizontal é o pedido da Fase 3.
2. **KEDA (escala por fila/métrica custom)** — útil se houvesse worker/broker; hoje a carga é HTTP, CPU
   basta. Reavaliar ao extrair um worker.

## Related
- [ADR-001](adr-001-modular-monolith-bounded-contexts.md) · [ADR-002](adr-002-cross-bc-communication.md)
- [OBSERVABILITY.md](OBSERVABILITY.md) — métricas que alimentam o HPA e os dashboards
- `gearflow-infra-k8s` — Terraform do cluster + `hpa.tf`
