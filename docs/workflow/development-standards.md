# 📋 Workflow de Desenvolvimento — Padrões

## Conventional Commits

Commits descritivos seguindo [Conventional Commits](https://www.conventionalcommits.org/):

```bash
feat(core-banking): adiciona endpoint de transferência
fix(analytics): corrige cache invalidation no Redis
docs(ADR): registra decisão de API Gateway
test(core-banking): adiciona testes de Account
```

Exemplos de tipo: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `perf`.

## Workflow por Sprint

1. Cada sprint tem seu próprio checkpoint/branch
2. Testes antes de implementar (TDD quando possível)
3. Documentar decisões em ADRs
4. Ao final de cada sprint, revisar e atualizar o AGENTS.md

## Verificação de Dependências (Checklist)

Sempre que propuser/utilizar uma nova dependência (NuGet, Maven, npm, Docker), verificar:

1. A dependência já existe no projeto (`.csproj`, `pom.xml`, `package.json`, `docker-compose.yml`)?
2. Versão compatível com o stack (`.NET 8`, `Java 21`, `Node 20`, `Angular 18`, `PostgreSQL 16`)?
3. Licença (MIT, Apache-2.0) e vulnerabilidades conhecidas (CVEs)?
4. Registrar no lockfile correspondente (`packages.lock.json`, `pom.xml`, `package-lock.json`, `docker-compose.lock`)?
5. Atualizar documentação (README/ADR) se introduz capacidade arquitetural?

## ADRs (Architecture Decision Records)

Toda decisão arquitetural relevante (nova dependência, padrão de comunicação, escolha de tecnologia, mudança de estrutura) deve ser registrada em `docs/ADR/ADR-XXX.md`:

**Template:** Título · Contexto · Decisão · Consequências

### ADRs Existentes

- **ADR-001**: Polyglot microservices (.NET + Java) com RabbitMQ
- **ADR-002**: Event-Driven Architecture para comunicação assíncrona
- **ADR-003**: Redis como cache distribuído + backplane WebSocket + rate limiting
- **ADR-004**: API Gateway como único entry point (roteamento, auth, rate limiting)
- **ADR-005**: OpenTelemetry para observabilidade unificada

## Indexação do AGENTS.md

Este repositório mantém o `AGENTS.md` como visão geral. O detalhamento por especialidade fica em `docs/`:

- **Back-End**: `docs/backend/dotnet-core-banking.md`, `docs/backend/java-analytics-ai.md`
- **Front-End**: `docs/frontend/angular-spa.md`
- **Microserviço / System Design**: `docs/architecture/system-design.md`, `docs/architecture/event-driven.md`
- **Testes**: `docs/testing/testing-guide.md`
- **Workflow**: `docs/workflow/development-standards.md`
