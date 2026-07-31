# ADR-003: Estratégia de autenticação e autorização

**Status**: Accepted
**Date**: 2026-07-31
**Related**: [RFC-003](../rfcs/rfc-003-estrategia-de-autenticacao.md)

## Context

A aplicação tem dois tipos de usuário e alguns fluxos anônimos:

- **Staff** (funcionário da oficina) — opera o sistema (OS, clientes, estoque, catálogo).
- **Cliente** — na Fase 3, autentica por **CPF** para consumir rotas protegidas (função serverless).
- **Fluxos anônimos** — aprovar/rejeitar orçamento por link de e-mail e consulta pública de status.

O legado não tinha modelo de papéis (apenas autenticado vs. anônimo). Precisamos de uma decisão
permanente sobre **como** autenticar e autorizar, que valha tanto no monólito quanto ao extrair BCs.

## Decision

### 1. JWT (Bearer) HMAC-SHA256 como credencial única

Todo acesso protegido usa **JWT Bearer**. A validação (assinatura, issuer, audience, expiração) é
**centralizada** em `Shared.Infrastructure/Auth/JwtAuthenticationExtensions` — `secret`/`issuer`/
`audience` vêm de `JwtOptions` (seção `Jwt`), validados no boot (`ValidateOnStart`).

### 2. Login de staff no Identity BC

`Identity` emite o par **access token + refresh token**:
- senha via `PasswordHasher` (ASP.NET Core Identity); **lockout** após N tentativas;
- claims: `sub`/`NameIdentifier`, `email`, `unique_name`, `jti`, **`security_stamp`**;
- **rotação de `security_stamp`** ao trocar a senha invalida os JWTs em circulação — revalidado a cada
  request no `OnTokenValidated` (`ISecurityStampValidator`, `AddStaffSecurityStampValidation`);
- refresh token: hash SHA-256 persistido, **rotacionável**, rastreado por IP.

### 3. Autenticação de cliente por CPF na borda serverless (Fase 3)

O **`gearflow-auth-lambda`** valida o CPF, consulta existência/status do cliente e emite um JWT com
claim `customer_id`, **assinado com o mesmo `secret`/`issuer`/`audience`** que a API valida. A API
**não** chama a Lambda — apenas valida o token. O ator do request é resolvido dos claims por
`ICurrentActor` (`Actor` System/Staff/Customer).

### 4. Autorização = autenticado vs. anônimo (sem papéis)

Não há RBAC — como no legado. Rotas de staff usam `RequireAuthorization()`; fluxos do cliente são
**anônimos por design**:
- aprovar/rejeitar orçamento (`GET/PUT /workshop/budgets/{id}/approve|reject`) — o "segredo" é
  conhecer o id do orçamento (vem no link do e-mail);
- consulta pública de status (`GET /externals/{clientId}/serviceOrder/{id}`).

### 5. CORS e rate limiting só no Gateway

Autenticação acontece na API; **CORS e rate limiting** (janela apertada em `/login`, generosa no
resto) vivem só no **Gateway** (ADR-001 / [RFC-002](../rfcs/rfc-002-nuvem-e-api-gateway.md)).

## Consequences

### Positive
- Uma credencial (JWT) para staff e cliente; API stateless → escala horizontal (ADR-004).
- Revogação real de sessão de staff (security_stamp) sem estado de sessão no servidor.
- CPF isolado na Lambda: escala independente e mantém a regra de validação fora do caminho quente.

### Negative
- **Secret compartilhado (HMAC)**: a Lambda e a API compartilham o segredo de assinatura. Rotacionar o
  segredo exige coordenar os dois. Alternativa assimétrica (JWKS) discutida na [RFC-003](../rfcs/rfc-003-estrategia-de-autenticacao.md).
- Fluxos anônimos por link são "segurança por posse do id" — aceitável para aprovar orçamento/consultar
  status; ver o TODO de validar posse `clientId`↔OS no Externals.

### Mitigations
- `security_stamp` cobre revogação de staff; refresh token rotacionável reduz janela de token vazado.
- Migração para JWKS (chave assimétrica) é possível sem mudar os consumidores (ver RFC-003).

## Alternatives Considered

1. **Sessão server-side (cookie)** — exige estado compartilhado, atrapalha HPA; rejeitado.
2. **RBAC com papéis** — o legado não tinha e não há requisito; evita complexidade sem valor agora.
3. **CPF autenticado dentro da API** (sem Lambda) — a Fase 3 pede função serverless; e isolar o CPF
   dá escala independente. Ver [RFC-003](../rfcs/rfc-003-estrategia-de-autenticacao.md).

## Related
- [RFC-003](../rfcs/rfc-003-estrategia-de-autenticacao.md) — discussão da estratégia de auth
- [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md) §3 — sequência de autenticação por CPF
