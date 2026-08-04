# CI — GearFlow (gearflow-app)

> Esteira de **integração contínua + publicação de imagem** do repo #4 (aplicação principal). O
> **deploy** (Terraform/Kubernetes) **não vive aqui** — fica nos repos de infra
> (`gearflow-infra-k8s` / `gearflow-infra-db`), conforme o split da Fase 3. Ver a seção
> [Handoff de deploy](#handoff-de-deploy).

## Workflows

| Arquivo | Dispara em | O que faz |
|---|---|---|
| [`ci.yml`](../../.github/workflows/ci.yml) | push/PR em `main`,`develop` + manual | Build Release, testes unitários + **arquitetura** (NetArchTest), **integração** (Testcontainers/SQL Server), **segurança** (deps vulneráveis/deprecadas) e **cobertura** com gate ratchet + comentário no PR |
| [`codeql.yml`](../../.github/workflows/codeql.yml) | push/PR em `main`,`develop` + semanal | SAST C# (GitHub CodeQL) → alertas em *Security → Code scanning* |
| [`docker-publish.yml`](../../.github/workflows/docker-publish.yml) | push em `main`,`develop`, tags `v*`, manual | Build & push das **2 imagens** para o GHCR (não roda em PR) |
| [`dependabot.yml`](../../.github/dependabot.yml) | agendado | PRs de atualização de NuGet + GitHub Actions |

## Jobs do `ci.yml`

```
build-and-unit ─┐
integration ────┤            (paralelos)
security ───────┘
                 └── coverage (needs: build-and-unit + integration) → ratchet + PR comment
```

- **build-and-unit** — `dotnet build -c Release` (com `TreatWarningsAsErrors=true`, o build já é o
  linter CS) + testes `!~IntegrationTests` (unit + arquitetura) com cobertura.
- **integration** — testes `~IntegrationTests` (Testcontainers sobe SQL Server real).
- **security** — `dotnet list package --vulnerable --include-transitive` (**quebra** se houver CVE) e
  `--deprecated` (informativo no resumo).
- **coverage** — mescla a cobertura (unit + integração) com ReportGenerator, aplica o
  [gate ratchet](#gate-ratchet-de-cobertura) e comenta no PR.

## Piso de cobertura (80%)

- Fonte de exclusões: [`coverage.runsettings`](../../coverage.runsettings) (raiz) + `[ExcludeFromCodeCoverage]`
  no bootstrap/DL. Detalhe conceitual em [TEST_COVERAGE.md](TEST_COVERAGE.md).
- Piso versionado em [`docs/coverage/coverage-baseline.txt`](../coverage/coverage-baseline.txt) (**80**).
- **PR/push**: falha (barra o merge) se a cobertura de linha ficar **abaixo de 80%** (sem tolerância).
- **push em `main`**: publica o badge (`coverage-badge.json`) com a cobertura **real**; o piso é fixo
  e só muda por edição humana do arquivo.

> Cobertura atual ~85%. Para elevar o piso no futuro, edite `coverage-baseline.txt`.

## Imagens (GHCR)

| Imagem | Dockerfile |
|---|---|
| `ghcr.io/gearflow-fiap/gearflow-api` | `src/Host/GearFlow.Api/Dockerfile` |
| `ghcr.io/gearflow-fiap/gearflow-gateway` | `src/Gateway/Dockerfile` |

Tags: `sha-<commit>`, nome da branch, `latest` (só em `main`) e `semver` (em tags `v*`).

## Secrets / configuração

- **Nenhum secret a configurar** para o CI: o push no GHCR usa o `GITHUB_TOKEN` nativo
  (`permissions: packages: write`).
- Os pacotes GHCR nascem privados. Para consumo público/externo, torne-os públicos ou dê acesso ao
  repo de infra em *Org → Packages → gearflow-api/gearflow-gateway → Package settings*.
- O passo "Bump baseline" faz `git push` em `main` com o `GITHUB_TOKEN`. Se houver **branch
  protection** exigindo PR, permita o bot (`github-actions[bot]`) ou um bypass para esse push; caso
  contrário o passo falha (o gate de PR continua funcionando normalmente).

## Handoff de deploy

O `gearflow-app` termina em **imagem publicada no GHCR**. O deploy é responsabilidade dos repos de
infra. Quando eles existirem, escolher **uma** das opções (a decidir):

1. **Push-based** — ao final do `docker-publish.yml`, disparar `repository_dispatch` para
   `gearflow-infra-k8s` com a tag `sha-<commit>`, que roda o `terraform apply` com a nova imagem.
2. **Pull-based (GitOps)** — o repo de infra referencia `:latest` (ou uma tag fixada) e reconcilia no
   seu próprio ciclo.

Enquanto os repos de infra não existem, o deploy é manual a partir da imagem publicada.

## Branch protection recomendada

Em *Settings → Branches* (`main` e `develop`), exigir como checks obrigatórios:
`Build + Unit + Architecture`, `Integration (Testcontainers / SQL Server)`,
`Security (vulnerable / deprecated deps)`, `Coverage (floor 80%)` e `Analyze (csharp)`.
