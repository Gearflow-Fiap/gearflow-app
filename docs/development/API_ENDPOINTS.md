# Referência de endpoints — GearFlow

Superfície HTTP completa, organizada por **Bounded Context**, com o mapeamento a partir dos
controllers do **projeto legado** (paridade). Todos passam pelo Gateway em `/api/**`. Endpoints de
staff exigem JWT; os marcados **(público)** são anônimos.

> Erros seguem ProblemDetails (RFC 9457). Dinheiro em centavos. Ver
> [ARCHITECTURE_DIAGRAMS](../architecture/ARCHITECTURE_DIAGRAMS.md) para os fluxos.

## Identity (`AuthController` legado)

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/identity/auth/register` (público) | Registra staff |
| POST | `/api/identity/auth/login` (público) | Login (lockout por tentativas) → JWT + refresh |
| POST | `/api/identity/auth/refresh-token` (público) | Rotaciona o par de tokens |
| POST | `/api/identity/auth/revoke-token` (público) | Revoga um refresh token (logout) |

## Customers (`ClientController` legado)

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/customers/clients` | Lista clientes (com veículos) |
| GET | `/api/customers/clients/{id}` | Cliente por id |
| POST | `/api/customers/clients` | Cria cliente (CPF **ou** CNPJ) |
| PUT | `/api/customers/clients/{id}` | Atualiza cliente |
| DELETE | `/api/customers/clients/{id}` | Remove cliente (+ veículos) |
| GET | `/api/customers/clients/{id}/vehicles` | Veículos do cliente |
| POST | `/api/customers/clients/{id}/vehicles` | Adiciona veículo |
| GET | `/api/customers/clients/vehicles/{id}` | Veículo por id |
| PUT | `/api/customers/clients/vehicles/{id}` | Atualiza veículo |
| DELETE | `/api/customers/clients/vehicles/{id}` | Remove veículo |

## Catalog (`JobController` legado)

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/catalog/jobs` | Lista serviços |
| GET | `/api/catalog/jobs/{id}` | Serviço por id |
| POST | `/api/catalog/jobs` | Cria serviço |
| PUT | `/api/catalog/jobs/{id}` | Atualiza serviço |
| DELETE | `/api/catalog/jobs/{id}` | Remove serviço |

## Inventory (`PartController` + `ConsumableController` legados)

| Método | Rota | Descrição |
|---|---|---|
| GET/POST | `/api/inventory/parts` · `/consumables` | Lista / cria |
| GET/PUT/DELETE | `/api/inventory/parts/{id}` · `/consumables/{id}` | Consulta / atualiza / remove |
| PATCH | `/api/inventory/parts/{id}/stock` · `/consumables/{id}/stock` | Repõe estoque (evento de auto-resume) |

## Workshop — Ordens de Serviço (`ServiceOrdersController` legado)

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/workshop/service-orders` | Lista OS ativas |
| GET | `/api/workshop/service-orders/monitoring-average` | Tempo médio por fase (dashboard Fase 3) |
| GET | `/api/workshop/service-orders/{id}` | OS por id (com histórico) |
| GET | `/api/workshop/service-orders/{id}/details` | OS + orçamento |
| POST | `/api/workshop/service-orders` | Abre OS |
| PUT | `/api/workshop/service-orders/{id}` | Troca veículo (antes da aprovação) |
| PUT | `/api/workshop/service-orders/{id}/in-diagnostic` | Inicia diagnóstico |
| PUT | `/api/workshop/service-orders/{id}/finalize-diagnostic` | Gera orçamento |
| PUT | `/api/workshop/service-orders/{id}/approve-budget` | Aprova (por OS) + reserva estoque |
| PUT | `/api/workshop/service-orders/{id}/reject-budget` | Rejeita (por OS) + cancela |
| PUT | `/api/workshop/service-orders/{id}/execute-job` | Marca serviço do orçamento executado |
| PUT | `/api/workshop/service-orders/{id}/resume-execution-from-waiting-parts` | Retoma (re-tenta reserva) |
| PUT | `/api/workshop/service-orders/{id}/finalize` | Finaliza + consome estoque |
| PUT | `/api/workshop/service-orders/{id}/deliver` | Entrega |
| DELETE | `/api/workshop/service-orders/{id}` | Desativa (soft-delete) |

## Workshop — Orçamentos (`BudgetsController` legado)

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/workshop/budgets/{id}` | Orçamento por id |
| GET | `/api/workshop/budgets/{id}/approve` (público) | Aprova via **link do e-mail** → página HTML de confirmação |
| GET | `/api/workshop/budgets/{id}/reject` (público) | Rejeita via **link do e-mail** → página HTML de confirmação |
| PUT | `/api/workshop/budgets/{id}/approve` (público) | Cliente aprova (por orçamento) — para o app/API |
| PUT | `/api/workshop/budgets/{id}/reject` (público) | Cliente rejeita (por orçamento) — para o app/API |

## Externals (`ExternalsController` legado)

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/externals/{clientId}/serviceOrder/{id}` (público) | Status da OS para o cliente |

## Diferenças conscientes vs. legado

- **Aprovar/rejeitar orçamento** existe em duas formas: por **OS** (staff, em `service-orders`) e por
  **orçamento** (cliente/público, em `budgets`) — o legado só tinha a segunda.
- As **páginas HTML** de aprovação por link (`GET budgets/{id}/approve|reject`) do legado foram
  portadas (executam a ação e devolvem uma página de confirmação) **e** também há os `PUT`
  equivalentes para consumo por app/API.
- A validação de posse `clientId`↔OS no endpoint público de Externals é um **TODO** documentado
  (join cross-BC OS→veículo→cliente).
- **CPF do cliente**: a autenticação por CPF (Fase 3) fica na Lambda (`gearflow-auth-lambda`), fora
  deste serviço; aqui só validamos o JWT resultante.
