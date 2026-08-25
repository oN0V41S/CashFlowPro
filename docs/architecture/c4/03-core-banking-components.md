# Nível 3 — Component Diagram: Core Banking Service

Decomposição interna do **Core Banking API** em componentes (classes/serviços principais) e suas responsabilidades.

```mermaid
C4Component
title Component Diagram — Core Banking Service

Container_Boundary(core, "Core Banking API") {
    Component(accCtrl, "AccountController", ".NET Controller", "Endpoints: POST /accounts, GET /accounts/{id}, GET /accounts/{id}/statement")
    Component(txCtrl, "TransactionController", ".NET Controller", "Endpoints: POST /transfers, GET /transactions")
    Component(transferSvc, "TransferService", "Domain Service", "Orquestra transferência: valida saldo, débito/crédito atômico, publica evento")
    Component(accRepo, "AccountRepository", "EF Core Repository", "Persistência: Account, Balance, Overdraft Limit")
    Component(txRepo, "TransactionRepository", "EF Core Repository", "Persistência: Transaction, Ledger Entries")
    Component(eventPub, "DomainEventPublisher", "RabbitMQ Publisher", "Publica: TransferCompleted, AccountCreated, BalanceChanged")
    Component(unitOfWork, "UnitOfWork", "EF Core", "Transação ACID cross-repositories")
}

ContainerDb(postgres, "PostgreSQL", "Database", "Tables: Users, Accounts, Transactions, LedgerEntries")
ContainerQueue(rabbit, "RabbitMQ", "Message Broker", "Exchange: cashflow.events")

Rel(accCtrl, transferSvc, "Chama")
Rel(txCtrl, transferSvc, "Chama")
Rel(transferSvc, accRepo, "Usa")
Rel(transferSvc, txRepo, "Usa")
Rel(transferSvc, unitOfWork, "Gerencia Transação")
Rel(transferSvc, eventPub, "Publica Evento")
Rel(accRepo, postgres, "Read/Write", "EF Core")
Rel(txRepo, postgres, "Read/Write", "EF Core")
Rel(eventPub, rabbit, "Publish", "AMQP")
```

## Componentes — Detalhamento

| Componente | Tipo | Padrão | Responsabilidade |
| :--- | :--- | :--- | :--- |
| **AccountController** | Controller | REST Endpoint | HTTP boundary: cria conta, consulta saldo/extrato |
| **TransactionController** | Controller | REST Endpoint | HTTP boundary: inicia transferência, lista transações |
| **TransferService** | Domain Service | Application Service | Orquestra caso de uso `Transfer`: valida regras, coordena repositórios, emite evento |
| **AccountRepository** | Repository | Repository Pattern | `Account` aggregate persistence: `GetById`, `Save`, `UpdateBalance` |
| **TransactionRepository** | Repository | Repository Pattern | `Transaction` + `LedgerEntry` persistence: `Add`, `GetByAccount` |
| **DomainEventPublisher** | Infrastructure | Event Publisher | Serializa e publica `IDomainEvent` no RabbitMQ (exchange `cashflow.events`) |
| **UnitOfWork** | Infrastructure | Unit of Work | Garante atomicidade: `Account` + `Transaction` + `Ledger` em mesma transação DB |

## Domain Events Publicados

| Evento | Trigger | Payload Principal | Consumers |
| :--- | :--- | :--- | :--- |
| `AccountCreated` | `AccountController.Create` | `AccountId`, `HolderName`, `InitialBalance` | Analytics, Notifications |
| `TransferCompleted` | `TransferService.Execute` | `FromAccountId`, `ToAccountId`, `Amount`, `Timestamp` | Analytics, Notifications |
| `BalanceChanged` | `TransferService.Execute` | `AccountId`, `NewBalance`, `ChangeType` | Analytics |

## Regras de Negócio Encapsuladas no TransferService

1. **Saldo protegido**: `Balance - Amount >= -OverdraftLimit` (default -R$ 500)
2. **Idempotência**: `TransferId` único evita duplicidade
3. **Consistência ACID**: Débito origem + Crédito destino + Ledger em mesma transação
4. **Eventual consistency**: Evento publicado **após** commit da transação DB (Outbox Pattern implícito via EF Core transaction)

## Fluxo de Transferência (Interno)

```mermaid
sequenceDiagram
    participant Ctrl as TransactionController
    participant Svc as TransferService
    participant UoW as UnitOfWork
    participant AccRepo as AccountRepository
    participant TxRepo as TransactionRepository
    participant Pub as DomainEventPublisher
    participant DB as PostgreSQL
    participant RMQ as RabbitMQ

    Ctrl->>Svc: ExecuteTransfer(dto)
    Svc->>AccRepo: GetById(fromAccountId)
    Svc->>AccRepo: GetById(toAccountId)
    Svc->>Svc: ValidateOverdraft(from, amount)
    Svc->>UoW: BeginTransaction()
    Svc->>AccRepo: Debit(from, amount)
    Svc->>AccRepo: Credit(to, amount)
    Svc->>TxRepo: Add(Transaction debit)
    Svc->>TxRepo: Add(Transaction credit)
    Svc->>UoW: Commit()
    Svc->>Pub: Publish(TransferCompleted)
    Pub->>RMQ: AMQP Publish
    Svc-->>Ctrl: TransferResult
```