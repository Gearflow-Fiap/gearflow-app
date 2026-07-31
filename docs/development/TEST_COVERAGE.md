# Cobertura de testes — GearFlow

Cobertura consolidada com **coverlet** + **ReportGenerator**, com um **gate ratchet** no CI (o piso
sobe a cada merge em `main`, nunca desce).

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

O job `coverage` mescla a cobertura dos jobs de unit + integração, compara com
`docs/coverage/coverage-baseline.txt` (tolerância de 0,5pp) e:

- **barra** o PR se cair abaixo do baseline;
- em push para `main`, **sobe** o baseline e publica o badge.

**Meta**: 100% "significativo". Projeto novo **nasce contribuindo** cobertura, não abaixo do piso.

## Convenções de teste

- Domínio testado com `FluentAssertions`, **sem mocks** (regras puras).
- Application testada por handler; Infrastructure/integração via `WebApplicationFactory` +
  Testcontainers (SQL Server real).
- Testes de arquitetura (`tests/Architecture`) rodam no CI **sem Docker** e barram violação de
  camada/BC.
