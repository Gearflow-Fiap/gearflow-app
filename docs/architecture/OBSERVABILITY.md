# Observabilidade — GearFlow (Fase 3)

Atende ao requisito de **Monitoramento e Observabilidade** da Fase 3. A instrumentação vive no
kernel `Shared.Infrastructure` (`AddObservability` / `MapObservability`, `AddSerilogLogging`) e é
ligada uma vez no `Program.cs` do `GearFlow.Api`.

## Pilares

| Pilar | Como | Onde |
|---|---|---|
| **Métricas** | OpenTelemetry → Prometheus scraping em `/metrics` | `ObservabilityExtensions` |
| **Traces** | OpenTelemetry (ASP.NET Core + HttpClient) → OTLP | export quando `Otlp:Endpoint` configurado |
| **Logs** | Serilog estruturado (**JSON** em produção) com `TraceId`/`SpanId` | `SerilogConfiguration` |
| **Health** | `/health/live`, `/health/ready` (checa SQL Server), `/health` | `HealthCheck*Extensions` |

O `Otlp:Endpoint` aponta para o coletor da ferramenta escolhida (**Datadog** ou **New Relic** via
OTLP, ou um OpenTelemetry Collector no cluster). Sem endpoint, os traces ficam in-process (dev).

## O que monitorar (requisito Fase 3)

- **Latência das APIs** — histograma `http.server.request.duration` (instrumentação ASP.NET Core).
- **Recursos do Kubernetes (CPU/memória)** — via métricas do cluster (kube-state-metrics / agente
  Datadog), não da app.
- **Healthchecks e uptime** — probes `/health/ready` e `/health/live` (Kubernetes + monitor externo).
- **Alertas de falha no processamento de OS** — regra sobre a métrica de negócio de erros/OS.
- **Logs estruturados (JSON) com correlação** — cada request carrega `TraceId`/`SpanId` (via
  `ExceptionHandlingMiddleware` + `LogContext`), permitindo cruzar log ↔ trace.

## Métricas de negócio (dashboards exigidos)

Cada BC define instrumentos sobre o `Meter` compartilhado
`ObservabilityExtensions.BusinessMeterName` (`"GearFlow.Business"`). Dashboards a expor:

| Dashboard | Métrica | Fonte |
|---|---|---|
| **Volume diário de OS** | contador `serviceorders.created` | Workshop, no `CreateServiceOrderHandler` |
| **Tempo médio de execução por status** | histograma `serviceorder.status.duration{status}` (Diagnóstico, Execução, Finalização) | Workshop, na transição de status (usa `ServiceOrderHistory`) |
| **Erros/falhas nas integrações** | contador `integration.errors{source}` | onde a porta/evento cross-BC falha |

## Wiring (referência)

```csharp
// Program.cs (GearFlow.Api)
builder.AddSerilogLogging("GearFlow.Api");
builder.AddObservability("GearFlow.Api");
builder.Services.AddSharedErrorHandling();
builder.Services.AddCustomHealthChecks(connectionString, "GearFlow.Api");
// ...
app.UseExceptionHandling();
app.MapObservability();       // GET /metrics
app.MapCustomHealthChecks();  // /health, /health/live, /health/ready
```

## Stack local (dev)

Prometheus + Grafana + um coletor OTLP (ex.: Jaeger/Tempo) via overlay de compose. `/metrics` é
raspado pelo Prometheus; os traces vão ao coletor quando `Otlp__Endpoint` está setado.
