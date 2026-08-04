# Cobertura de testes — GearFlow

Herdado do `delivery-app-backend`: cobertura consolidada com **coverlet** + **ReportGenerator**, com
um **gate ratchet** no CI (o piso sobe a cada merge em `main`, nunca desce).

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

## Gate ratchet (CI)

Implementado no job `coverage` do [`ci.yml`](../../.github/workflows/ci.yml) (visão geral da esteira
em [CI.md](CI.md)). O job mescla a cobertura dos jobs de unit + integração, compara com
`docs/coverage/coverage-baseline.txt` (tolerância de 0,5pp) e:

- **barra** o PR se a cobertura de linha cair mais de 0,5pp abaixo do baseline (e posta um comentário
  fixo de cobertura no PR);
- em push para `main`, **sobe** o baseline e publica o badge (`docs/coverage/coverage-badge.json`),
  commitando com `[skip ci]`.

> O baseline nasce em `0`; o primeiro push em `main` estabelece o piso real e, a partir daí, só sobe.

**Meta**: 100% "significativo". Projeto novo **nasce contribuindo** cobertura, não abaixo do piso.

## Convenções de teste

- Domínio testado com `FluentAssertions`, **sem mocks** (regras puras).
- Application testada por handler; Infrastructure/integração via `WebApplicationFactory` +
  Testcontainers (SQL Server real).
- Testes de arquitetura (`tests/Architecture`) rodam no CI **sem Docker** e barram violação de
  camada/BC.
