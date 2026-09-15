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

- **Latência das APIs** — histograma `http.server.request.duration` (instrumentação ASP.NET Core),
  exportado via OTLP para o New Relic. Alerta (p95 > 1s) e dashboard (p50/p95/p99) em
  `newrelic-alerts.tf`/`newrelic-dashboard.tf` no `gearflow-infra-k8s`.
- **Recursos do Kubernetes (CPU/memória)** — via `K8sContainerSample` do nri-bundle
  (`newrelic-infrastructure`/`kube-state-metrics`, instalado em `monitoring.tf`), não da app. Alerta
  (>80% por pod) e dashboard em `newrelic-alerts.tf`/`newrelic-dashboard.tf`.
- **Healthchecks e uptime** — probes `/health/ready` e `/health/live` (usadas pelos probes do
  Kubernetes) + monitor sintético externo do New Relic batendo em `/health/ready` a cada 5 min
  (`newrelic_synthetics_monitor.healthcheck`), com alerta de indisponibilidade e widget de uptime no
  dashboard.
- **Alertas de falha no processamento de OS** — condição NRQL sobre `integration.errors` (metric
  de negócio abaixo) em `newrelic_nrql_alert_condition.service_order_processing_errors`, notificando
  por e-mail via `newrelic_workflow`/`newrelic_notification_channel`.
- **Logs estruturados (JSON) com correlação** — cada request carrega `TraceId`/`SpanId` (via
  `ExceptionHandlingMiddleware` + `LogContext`), permitindo cruzar log ↔ trace. Em produção, o
  agente APM .NET encaminha os logs ao New Relic (`NEW_RELIC_APPLICATION_LOGGING_FORWARDING_ENABLED`,
  ver `app.tf` no `gearflow-infra-k8s`) — sem Fluent Bit, decorados com `trace.id`/`span.id`.

Todos os alertas (política, canal de e-mail e 5 condições) vivem em
[`gearflow-infra-k8s/terraform/newrelic-alerts.tf`](../../../gearflow-infra-k8s/terraform/newrelic-alerts.tf);
o e-mail de destino é a variável Terraform `newrelic_alert_email` (repository variable
`NEWRELIC_ALERT_EMAIL` no CI).

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
