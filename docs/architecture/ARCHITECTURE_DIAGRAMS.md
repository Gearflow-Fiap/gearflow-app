# Diagramas de arquitetura — GearFlow

Todos os diagramas em **Mermaid** (renderizam no GitHub e na maioria dos viewers de Markdown).
Atende aos itens da Fase 3: **Diagrama de Componentes** e **Diagramas de Sequência** para o fluxo de
**autenticação** e de **abertura de ordem de serviço**.

---

## 1. Diagrama de Componentes (visão de nuvem)

```mermaid
flowchart TB
    subgraph Client["Clientes"]
        Browser["App / Browser (staff)"]
        CustomerApp["Cliente (CPF)"]
    end

    subgraph Cloud["Nuvem (Kubernetes)"]
        APIGW["API Gateway<br/>(YARP / AWS API Gateway)<br/>CORS + rate limiting"]

        subgraph Lambda["Serverless"]
            AuthFn["Auth Function (Lambda)<br/>valida CPF → JWT"]
        end

        subgraph K8s["Cluster Kubernetes (HPA)"]
            App["GearFlow.Api<br/>(monólito modular)<br/>Identity · Customers · Catalog<br/>Inventory · Workshop · Notifications"]
        end

        DB[("SQL Server<br/>gerenciado")]
        SMTP["SMTP / Mailpit<br/>(e-mail)"]
    end

    subgraph Obs["Observabilidade"]
        Metrics["Prometheus /metrics"]
        Traces["OTLP → Datadog/New Relic"]
        Logs["Logs JSON estruturados"]
    end

    Browser -->|HTTPS| APIGW
    CustomerApp -->|CPF| AuthFn
    AuthFn -->|consulta cliente| DB
    AuthFn -->|JWT| CustomerApp
    CustomerApp -->|Bearer JWT| APIGW
    APIGW -->|/api/**| App
    App --> DB
    App --> SMTP
    App -.->|scrape| Metrics
    App -.->|traces| Traces
    App -.->|stdout JSON| Logs
```

**Notas**
- O **API Gateway** é a entrada única; CORS e rate limiting vivem só nele.
- A **Lambda** autentica clientes por CPF (Fase 3) e assina o JWT com o **mesmo** segredo/issuer/
  audience que o `GearFlow.Api` valida — por isso a app confia no token sem chamar a Lambda.
- O `GearFlow.Api` é um **monólito modular**: todos os BCs no mesmo processo, fronteiras no código
  (ver [ADR-001](adr-001-modular-monolith-bounded-contexts.md)).

---

## 2. Componentes internos (Bounded Contexts)

```mermaid
flowchart LR
    subgraph Host["GearFlow.Api (host único)"]
        EP["Endpoints (IEndpoint)<br/>Internal / Public"]
    end

    EP -->|ISender| WApp
    EP -->|ISender| CApp
    EP -->|ISender| CatApp
    EP -->|ISender| IApp
    EP -->|ISender| IdApp

    subgraph Workshop
        WApp["Application<br/>(commands/queries)"] --> WDom["Domain<br/>ServiceOrder · Budget"]
        WApp -.->|IInventoryReservation| IApp
    end
    subgraph Inventory
        IApp["Application"] --> IDom["Domain<br/>Part · Consumable"]
    end
    subgraph Customers
        CApp["Application"] --> CDom["Domain<br/>Client · Vehicle"]
    end
    subgraph Catalog
        CatApp["Application"] --> CatDom["Domain<br/>Job"]
    end
    subgraph Identity
        IdApp["Application"] --> IdDom["Domain<br/>User · RefreshToken"]
    end

    IApp -. "LowStockAlert / PartsReplenished<br/>(eventos in-process)" .-> Notif["Notifications"]
    IApp -. PartsReplenished .-> WApp
```

Regras de dependência (travadas por testes de arquitetura): `Api → Application → Domain`,
`Infrastructure → Application → Domain`, `Domain` sem dependências externas. Cross-BC apenas por
**porta** (`IInventoryReservation`) ou **evento de integração** (`Shared.Contracts`).

---

## 3. Sequência — Autenticação por CPF (Lambda → JWT)

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente
    participant L as Auth Lambda
    participant DB as SQL Server
    participant GW as API Gateway
    participant API as GearFlow.Api

    C->>L: POST /auth { cpf }
    L->>L: valida formato do CPF
    alt CPF inválido
        L-->>C: 400 ProblemDetails (Validation)
    else CPF válido
        L->>DB: SELECT cliente por CPF (existência + status)
        alt cliente não encontrado / inativo
            DB-->>L: vazio
            L-->>C: 404 / 403 ProblemDetails
        else cliente ativo
            DB-->>L: { clientId, ativo }
            L->>L: gera JWT (claim customer_id, assinado c/ segredo compartilhado)
            L-->>C: 200 { access_token }
        end
    end

    Note over C,API: Chamadas subsequentes usam o Bearer JWT
    C->>GW: GET /api/customers/{clientId}/serviceOrder/{id}<br/>Authorization: Bearer JWT
    GW->>API: encaminha (valida assinatura/exp no API)
    API->>API: HttpCurrentActor → Actor.Customer(clientId)
    API->>DB: consulta OS do cliente
    API-->>C: 200 { status da OS }
```

O login de **staff** (funcionário) é separado: e-mail/senha no Identity BC, com `SecurityStamp` que
invalida tokens ao trocar a senha. Ver [ADR-003](adr-001-modular-monolith-bounded-contexts.md)
(planejado).

---

## 4. Sequência — Abertura de Ordem de Serviço

```mermaid
sequenceDiagram
    autonumber
    actor S as Staff
    participant GW as API Gateway
    participant EP as Endpoint (Workshop)
    participant H as CreateServiceOrderHandler
    participant Cust as Customers (porta)
    participant Cat as Catalog (porta)
    participant Inv as Inventory (porta)
    participant DB as SQL Server

    S->>GW: POST /api/workshop/service-orders { cliente, veículo, serviços, peças }
    GW->>EP: encaminha (Bearer JWT staff)
    EP->>H: ISender.Send(CreateServiceOrderCommand)
    H->>Cust: garante cliente (cria ou reutiliza) + veículo
    Cust-->>H: clientId, vehicleId
    H->>Cat: valida que os serviços (Job) existem
    Cat-->>H: ok / erro
    H->>Inv: valida que as peças existem
    Inv-->>H: ok / erro
    alt validação falha
        H-->>EP: Result.Failure(Error)
        EP-->>S: 400/404 ProblemDetails
    else ok
        H->>H: new ServiceOrder(...) status=Received<br/>+ ServiceOrderHistory
        H->>DB: SaveChangesAsync (uma transação)
        H-->>EP: Result.Success(dto)
        EP-->>S: 201 Created { OSCode, status: Received }
    end
```

Transições seguintes da OS (`Received → InDiagnostic → AwaitingApproval → InExecution → Finalized →
Delivered`), aprovação de orçamento e o fluxo reserva/consumo de estoque estão descritos em
[ADR-001](adr-001-modular-monolith-bounded-contexts.md) §4 e serão detalhados em ADR-002 (comunicação
cross-BC).

---

## 5. Estados da Ordem de Serviço

```mermaid
stateDiagram-v2
    [*] --> Received
    Received --> InDiagnostic: StartDiagnostic
    InDiagnostic --> AwaitingApproval: FinalizeDiagnostic (cria Budget)
    AwaitingApproval --> InExecution: aprovar orçamento + estoque reservado
    AwaitingApproval --> AwaitingPartsOrConsumables: aprovar + estoque insuficiente
    AwaitingApproval --> Canceled: rejeitar orçamento
    AwaitingPartsOrConsumables --> InExecution: ResumeExecution (peças chegaram)
    InExecution --> Finalized: Finalize (consome estoque)
    Finalized --> Delivered: Deliver
    Received --> [*]: Deactivate (soft-delete)
    InDiagnostic --> [*]: Deactivate (soft-delete)
    Delivered --> [*]
    Canceled --> [*]
```
