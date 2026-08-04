# Cobertura de testes — GearFlow

Cobertura consolidada com **coverlet** + **ReportGenerator**, com um **piso obrigatório de 80%** no CI
(o merge é barrado se a cobertura de linha ficar abaixo de 80%). Cobertura atual: **~86%**.

## Como funciona

- `coverlet.collector` está em todo projeto de teste (via `Directory.Packages.props`).
- `coverage.runsettings` (raiz) é a **fonte única de exclusões**: projetos de teste, `Gateway`,
  `*.Contracts`, migrations EF, código gerado e `Program.cs` ficam fora do denominador. **Não**
  excluir `DependencyInjection.cs`/seeders — as fixtures de integração já os cobrem.
- `.config/dotnet-tools.json` fixa o `reportgenerator` (restaurado no CI com `dotnet tool restore`).

## Rodar localmente

```bash
dotnet test GearFlow.slnx --settings coverage.runsettings --collect:"XPlat Code Coverage"
dotnet tool restore
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:"Html;TextSummary"
```

## Piso de cobertura (CI)

Implementado no job `coverage` do [`ci.yml`](../../.github/workflows/ci.yml) (visão geral da esteira
em [CI.md](CI.md)). O job mescla a cobertura dos jobs de unit + integração e:

- **barra** o PR se a cobertura de linha ficar **abaixo do piso** de
  `docs/coverage/coverage-baseline.txt` (**80%**, sem tolerância) e posta um comentário fixo de
  cobertura no PR;
- em push para `main`, publica o badge (`docs/coverage/coverage-badge.json`) com a cobertura **real**
  (o piso é fixo — só muda por edição humana do arquivo).

> O piso é fixo em 80% por decisão de produto. Para elevá-lo no futuro (ratchet manual), edite
> `coverage-baseline.txt`.

**Meta**: 100% "significativo". As exclusões (composition roots, `Gateway`, `*.Contracts`, migrations,
código gerado, `Program.cs`) vivem em `coverage.runsettings` — mantêm o denominador honesto.

## Convenções de teste

- Domínio testado com `FluentAssertions`, **sem mocks** (regras puras).
- Application testada por handler; Infrastructure/integração via `WebApplicationFactory` +
  Testcontainers (SQL Server real).
- Testes de arquitetura (`tests/Architecture`) rodam no CI **sem Docker** e barram violação de
  camada/BC.
