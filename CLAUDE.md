# CashFlow Pro — Contexto para Claude

## Projeto

Plataforma fintech **educacional** (laboratório técnico, não produção).
Foco: backend polyglot, event-driven, IA. **Sem frontend** — Angular removido do escopo.

## Desenvolvedor

Rafael Novais · Java intermediário · Stack forte em TypeScript/Next.js.
Faculdade ADS (UNiSA) · Foco de carreira: **backend Java + .NET**.
Bootcamp ativo: DIO × Itaú — Java com IA (prazo 22/11/2026).

## Serviços

| Serviço | Stack | Status |
|---------|-------|--------|
| Core Banking | .NET 8, EF Core, PostgreSQL | ✅ Sprint 1 concluída |
| Analytics & AI | Java Spring Boot 3, Redis, Gemini | ⬜ Sprint 2 em andamento |
| Notifications | .NET 8, SignalR, Redis | ⬜ Sprint 2 pendente |
| API Gateway | .NET 8 (YARP/Ocelot) | ⬜ Sprint 2 pendente |

## Infraestrutura

PostgreSQL 16 · Redis 7 · RabbitMQ 4 · Docker Compose local

## Branch atual

`feature/java-analytics` — Analytics Service Java/Spring Boot 3.
Consumer RabbitMQ → cache Redis → preparação para Gemini API.

## Padrões obrigatórios

- Commits: Conventional Commits (`feat:`, `fix:`, `chore:`)
- Java: Spring Boot 3, JUnit 5 + Mockito, TestContainers para integração
- .NET: xUnit, Moq, EF Core migrations versionadas
- Eventos: `TransactionCreated` e `TransferCompleted` via RabbitMQ
- Cache: Cache-Aside pattern (Redis). TTL padrão insights: 1h
- Sem lógica de negócio nos controllers — vai nos services

## O que NÃO fazer

- Não sugerir Angular, frontend ou Playwright E2E
- Não criar migrações EF Core sem confirmar com o dev
- Não alterar `AGENTS.md` ou `CLAUDE.md` sem pedido explícito
- Não usar `dotnet ef` com `--force` sem avisar

## Refs rápidas

| Área | Documento |
|------|-----------|
| Arquitetura event-driven | `docs/architecture/event-driven.md` |
| Backend .NET | `docs/backend/dotnet-core-banking.md` |
| Backend Java | `docs/backend/java-analytics-ai.md` |
| Testes | `docs/testing/testing-guide.md` |
| Bootcamp DIO Itaú | `docs/bootcamp/dio-itau-path.md` |
| Erros comuns .NET | seção ⚠️ do `AGENTS.md` |
