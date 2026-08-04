<!--
  Título do PR: use conventional commit — feat|fix|refactor|test|ci|docs|chore(escopo): resumo
  Fluxo: feature → develop → main. PRs de mudança vão para `develop`; só a promoção usa base `main`.
-->

## O quê e por quê

<!-- O que este PR muda e qual problema/necessidade resolve (o "porquê", não só o "o quê"). -->



## Tipo de mudança

- [ ] `feat` — nova funcionalidade
- [ ] `fix` — correção de bug
- [ ] `refactor` — mudança interna sem alterar comportamento
- [ ] `test` — testes
- [ ] `ci` / `chore` — esteira, build, deps, configuração
- [ ] `docs` — documentação

## Bounded Contexts afetados

<!-- Marque os BCs tocados (ver docs/architecture/BOUNDED_CONTEXTS.md). -->

- [ ] Identity · [ ] Customers · [ ] Catalog · [ ] Inventory · [ ] Workshop · [ ] Notifications
- [ ] Shared (Domain/Infrastructure/Contracts) · [ ] Gateway · [ ] Host (GearFlow.Api) · [ ] Infra/CI

## Checklist

- [ ] Segue as regras de arquitetura: `Domain` sem dependências externas, handlers `internal sealed`,
      endpoint só traduz HTTP (via `ISender` + use case), sem CORS/rate limiting por BC.
- [ ] Regras de negócio via `Result<T>`/`Error` (nunca exceção) e erros HTTP via ProblemDetails
      (`ToOk()`/`ToProblem()`), não `Results.BadRequest/NotFound` à mão.
- [ ] Testes adicionados/atualizados (domínio com FluentAssertions **sem mocks**; handlers/integração
      quando aplicável).
- [ ] Cobertura mantém os pisos: **linha ≥ 80% e branch ≥ 80%** (o job `Coverage (floor 80%)` barra
      abaixo disso).
- [ ] Endpoints novos declaram `.WithSummary(...)` e `.WithDescription(...)`.
- [ ] Versões de pacote só em `Directory.Packages.props` (Central Package Management); build limpo
      (`TreatWarningsAsErrors`).
- [ ] Documentação atualizada no mesmo commit quando a mudança é estrutural ou mexe num ADR
      (CLAUDE.md / docs / README).

## Como testar / verificar

<!--
  Passos para validar localmente. Ex.:
  dotnet test GearFlow.slnx
  docker compose up -d && curl .../health/ready
-->



## Notas / issues relacionadas

<!-- Prints, contexto extra, "Closes #123". -->
