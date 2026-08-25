# Nível 1 — System Context Diagram

Visão de alto nível do **CashFlow Pro** como uma "caixa preta" e seus atores externos.

```mermaid
C4Context
title System Context Diagram — CashFlow Pro

Person(customer, "Usuário Final", "Pessoa física que gerencia finanças e visualiza insights")
System_Boundary(cashflow, "CashFlow Pro") {
    System(spa, "Angular SPA", "Frontend Web — Dashboard, Transferências, Insights")
    System_Ext(gateway, "API Gateway", "Roteamento, JWT, Rate Limiting (.NET/YARP)")
    System_Ext(core, "Core Banking", "Contas, Transferências, Ledger, Eventos (.NET/EF Core)")
    System_Ext(analytics, "Analytics & AI", "Agregações, Fraude, Health Score, Gemini (Java/Spring)")
    System_Ext(notif, "Notifications", "WebSocket SignalR, Presença, Push (.NET)")
}
System_Ext(gemini, "Gemini API", "LLM Google — Geração de insights financeiros")
System_Ext(postgres, "PostgreSQL", "Dados transacionais (Core Banking)")
System_Ext(redis, "Redis", "Cache, Sessões WS, Rate Limit")
System_Ext(rabbit, "RabbitMQ", "Event Bus — Mensageria assíncrona")

Rel(customer, spa, "Usa", "HTTPS")
Rel(spa, gateway, "Chama API", "REST/JSON + JWT")
Rel(gateway, core, "Roteia", "gRPC/HTTP")
Rel(gateway, analytics, "Roteia", "gRPC/HTTP")
Rel(gateway, notif, "Roteia", "WebSocket/SignalR")
Rel(core, postgres, "Lê/Escreve", "SQL (EF Core)")
Rel(analytics, redis, "Cache", "Redis Protocol")
Rel(core, rabbit, "Publica Eventos", "AMQP")
Rel(analytics, rabbit, "Consome Eventos", "AMQP")
Rel(notif, rabbit, "Consome Eventos", "AMQP")
Rel(notif, redis, "Backplane", "Redis Pub/Sub")
Rel(analytics, gemini, "Prompt/Response", "HTTPS/REST")
```

## Legenda

| Símbolo | Significado |
| :--- | :--- |
| `Person` | Ator humano externo ao sistema |
| `System` | Sistema interno (parte do CashFlow Pro) |
| `System_Ext` | Sistema externo (fora do escopo do CashFlow Pro) |
| `Rel` | Relacionamento / fluxo de dados |