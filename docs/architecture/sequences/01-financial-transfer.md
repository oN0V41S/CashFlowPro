# Sequência 1 — Financial Transfer Flow

Fluxo síncrono completo de transferência entre contas (ACID transaction).

```mermaid
sequenceDiagram
    autonumber
    actor User as Usuário Final (Rafael)
    participant SPA as Angular SPA
    participant GW as API Gateway
    participant CB as Core Banking
    participant DB as PostgreSQL
    participant RMQ as RabbitMQ
    participant AN as Analytics
    participant NT as Notifications

    Note over User,NT: Financial Transfer – ACID Transaction
    
    User->>SPA: Solicita transferência (R$ 150,00)
    SPA->>GW: POST /api/transfers<br/>Authorization: Bearer {JWT with accountId}<br/>{toAccountToken, description}
    
    GW->>GW: Valida JWT (Signature, Expiration)
    GW->>CB: Forward Request (HTTP/REST)
    
    CB->>CB: Extract fromAccountId from JWT Claims
    CB->>CB: Decode toAccountIdToken → toAccountId
    CB->>CB: Validate: from != to, amount > 0, description not empty
    
    rect rgb(248, 242, 240)
        Note right of CB: Transação ACID
    end
    CB->>DB: SELECT Account WHERE Id IN (from, to) FOR UPDATE
    DB-->>CB: Return Account rows (locked)
    
    CB->>CB: Check: from.Balance - Amount >= -OverdraftLimit (-500)
    alt Overdraft OK
        CB->>DB: BEGIN TRANSACTION
        CB->>DB: UPDATE Account SET Balance = Balance - Amount WHERE Id = fromAccountId
        CB->>DB: UPDATE Account SET Balance = Balance + Amount WHERE Id = toAccountId
        CB->>DB: INSERT INTO Transaction (Id, AccountId, Type, Amount, Description, RelatedAccountId) VALUES (...)
        CB->>DB: INSERT INTO LedgerEntry (Id, AccountId, Type, Amount, Balance) VALUES (...)
        CB->>DB: COMMIT
        CB->>RMQ: Publish TransferCompleted Event
    else Overdraft Exceeded
        CB->>DB: ROLLBACK
        CB-->>GW: HTTP 400 (Insufficient funds/overdraft)
    end
    
    RMQ->>AN: Queue: analytics.events<br/>TransferCompleted
    RMQ->>NT: Queue: notifications.events<br/>TransferCompleted
    
    CB-->>GW: HTTP 201 Created<br/>{transactionId, fromBalance, toBalance}
    GW-->>SPA: Response (Transfer concluída)
    SPA->>User: UI: Saldo atualizado, toast sucesso
    
    rect rgb(240, 248, 255)
        Note right of AN: Processamento Async
        AN->>AN: Update aggregations
        AN->>DB: Cache invalidation
        NT->>NT: Prepare WebSocket payload
    end
```

## Endpoints

| Passo | Endpoint | Método | Payload |
| :--- | :--- | :--- | :--- |
| 1 | `/api/transfers` | POST | `{toAccountToken: "acc-xxx", description: "Aluguel"}` |

## Domain Event (TransferCompleted)

```json
{
  "eventId": "uuid-v4",
  "eventType": "TransferCompleted",
  "timestamp": "2024-01-15T10:30:00Z",
  "data": {
    "transactionId": "uuid",
    "fromAccountId": "guid",
    "toAccountId": "guid",
    "amount": 150.00,
    "currency": "BRL",
    "description": "Aluguel"
  }
}
```

## Regras de Negócio

| Regra | Validação | Ação |
| :--- | :--- | :--- |
| **Saldo suficiente** | `Balance - Amount >= -OverdraftLimit` | Rollback + 400 |
| **Mesma conta** | `fromAccountId != toAccountId` | 400 Bad Request |
| **Valor positivo** | `amount > 0` | 400 Bad Request |
| **Idempotência** | Header `Idempotency-Key` | Retorna 200 se já processado |