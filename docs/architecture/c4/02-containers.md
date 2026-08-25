# Nível 2 — Container Diagram

Zoom no **CashFlow Pro** mostrando suas aplicações (containers executáveis) e armazenamentos de dados.

```mermaid
C4Container
title Container Diagram — CashFlow Pro

Person(user, "Usuário Final")
System_Ext(spa, "Angular SPA", "Angular 18, Standalone, Signals", "Frontend Web")

System_Boundary(cashflow, "CashFlow Pro") {
    Container(gateway, "API Gateway", ".NET 8, YARP", "Roteamento, Auth (JWT), Rate Limiting, Terminação TLS")
    Container(core, "Core Banking API", ".NET 8, EF Core, DDD", "Contas, Transferências, Ledger, Domain Events")
    Container(analytics, "Analytics & AI API", "Java 21, Spring Boot 3", "Agregações, Detecção Fraude, Health Score, AI Insights")
    Container(notif, "Notifications Service", ".NET 8, SignalR", "WebSocket Real-time, Presença, Notificações Push")
    
    ContainerDb(postgres, "PostgreSQL", "PostgreSQL 16", "Dados Transacionais: Users, Accounts, Transactions, LedgerEntries")
    ContainerDb(redis, "Redis", "Redis 7", "Cache Distribuído, Sessões SignalR, Rate Limiting (Sliding Window)")
    ContainerQueue(rabbit, "RabbitMQ", "RabbitMQ 4", "Event Bus: TransferCompleted, AccountCreated, etc.")
}

System_Ext(gemini, "Gemini API", "Google Generative AI", "LLM para Insights Financeiros")

Rel(user, spa, "Navega/Interage", "HTTPS")
Rel(spa, gateway, "API Calls", "REST/JSON + JWT")
Rel(gateway, core, "Encaminha", "HTTP/gRPC")
Rel(gateway, analytics, "Encaminha", "HTTP/gRPC")
Rel(gateway, notif, "WebSocket Upgrade", "SignalR/WS")
Rel(core, postgres, "Read/Write", "SQL (EF Core)")
Rel(analytics, postgres, "Read (Analytics)", "SQL (Spring Data)")
Rel(analytics, redis, "Cache-Aside", "Redis Protocol")
Rel(core, rabbit, "Publish Events", "AMQP 0.9.1")
Rel(analytics, rabbit, "Consume Events", "AMQP 0.9.1")
Rel(notif, rabbit, "Consume Events", "AMQP 0.9.1")
Rel(notif, redis, "SignalR Backplane", "Redis Pub/Sub")
Rel(analytics, gemini, "Generate Insights", "HTTPS/REST")
```

## Containers — Resumo

| Container | Tipo | Tecnologia | Responsabilidade Principal |
| :--- | :--- | :--- | :--- |
| **API Gateway** | Application | .NET 8 + YARP | Single entry point: routing, JWT validation, rate limiting, TLS termination |
| **Core Banking API** | Application | .NET 8 + EF Core | Domain-driven: Accounts, Transfers, Ledger, Domain Events publishing |
| **Analytics & AI API** | Application | Java 21 + Spring Boot 3 | Event-driven aggregations, fraud detection, health score, Gemini integration |
| **Notifications Service** | Application | .NET 8 + SignalR | Real-time WebSocket, presence tracking, push notifications |
| **Angular SPA** | Application (Ext) | Angular 18 + Signals | Dashboard, transfer forms, insights visualization |
| **PostgreSQL** | Database | PostgreSQL 16 | Relational storage for Core Banking (ACID) |
| **Redis** | Database | Redis 7 | Distributed cache, SignalR backplane, rate limiting |
| **RabbitMQ** | Queue | RabbitMQ 4 | Async event bus between services |
| **Gemini API** | System (Ext) | Google GenAI | LLM for financial insight generation |

## Padrões de Comunicação

| Fluxo | Protocolo | Padrão |
| :--- | :--- | :--- |
| SPA → Gateway | HTTPS/REST + JWT | Request/Response (Síncrono) |
| Gateway → Core/Analytics | HTTP/gRPC | Request/Response (Síncrono) |
| Gateway → Notifications | WebSocket/SignalR | Persistent Connection (Real-time) |
| Core → RabbitMQ | AMQP 0.9.1 | Event Publishing (Assíncrono) |
| Analytics/Notif → RabbitMQ | AMQP 0.9.1 | Event Consumption (Assíncrono) |
| Analytics ↔ Redis | RESP | Cache-Aside (Read-Through/Write-Through) |
| Notifications ↔ Redis | Redis Pub/Sub | SignalR Scale-out Backplane |
| Analytics → Gemini | HTTPS/REST | Request/Response (Síncrono, LLM) |