# Sequência 7 — Fraud Detection Alert Flow

Fluxo de detecção de fraude em tempo real baseado em eventos de transferência.

```mermaid
sequenceDiagram
    autonumber
    participant CB as Core Banking
    participant RMQ as RabbitMQ
    participant FT as Fraud Service
    participant HC as History Cache (Redis)
    participant DB as PostgreSQL
    participant NT as Notifications Consumer
    participant HUB as NotificationHub
    participant SPA as Angular SPA

    Note over CB,SPA: Fraud Detection – Event-Driven Real-time

    rect rgb(255, 248, 240)
        Note left of CB: Event Publishing
    end
    
    CB->>RMQ: Publish TransferCompleted Event
    RMQ->>FT: Queue: fraud.events
    
    rect rgb(240, 248, 255)
        Note right of FT: Fraud Analysis Engine
    end
    
    FT->>HC: GET transaction_velocity:{accountId} (last 5 min)
    HC-->>FT: [tx1, tx2, ..., txN]
    
    alt Velocity > 10 txns in 5min?
        alt FRAUD DETECTED
            FT->>FT: Flag Transaction as FRAUD
            FT->>DB: UPDATE Transaction SET status='flagged' WHERE id IN (...)
            FT->>FT: Create FraudAlert Event
            FT->>RMQ: Publish FraudAlert (high priority)
        else Normal Velocity
            FT->>HC: GET geo_history:{accountId}
        end
    else Geo Anomaly
        FT->>HC: Deserialize geo_history:{accountId}
        alt Country changed vs last 30 days?
            FT->>FT: Flag as GEO-ANOMALY
            FT->>FT: Create FraudAlert Event
            FT->>RMQ: Publish FraudAlert
        else Normal Geo
            FT->>HC: GET amount_history:{accountId}
        end
    else Amount Outlier
        FT->>HC: Get historical amounts (mean, stdDev)
        alt Amount > mean + 3*stdDev?
            FT->>FT: Flag as AMOUNT-OUTLIER
            FT->>FT: Create FraudAlert Event
            FT->>RMQ: Publish FraudAlert
        else Normal Amount
            FT->>FT: No fraud detected
        end
    end
    
    rect rgb(248, 242, 240)
        Note bottom of RMQ: Alert Notification Path
    end
    
    RMQ->>NT: Queue: notifications.events<br/>FraudAlert
    NT->>NT: Build FraudAlert DTO
    NT->>HUB: Clients.Group(accountId).SendAsync("FraudAlert", dto)
    HUB->>SPA: WebSocket: FraudAlert event
    
    rect rgb(225, 245, 255)
        Note bottom of SPA: UI Response
    end
    
    SPA->>SPA: Show modal: "Alerta de Segurança"
    SPA->>SPA: Disable debit card (via API call)
    SPA->>SPA: Show contact support button
```

## Fraud Service Implementation

```java
@Service
public class FraudDetectionService {
    
    @RabbitListener(queues = "fraud.events")
    public void analyzeTransfer(TransferCompletedEvent evt) {
        var accountId = evt.getToAccountId();
        
        // 1. Velocity Check
        int recentCount = redisTemplate.opsForList()
            .leftPush("velocity:" + accountId, UUID.randomUUID().toString())
            .size();
        
        if (recentCount > 10) {
            emitFraudAlert(accountId, "VELOCITY", recentCount);
            return;
        }
        
        // 2. Geo Anomaly
        var geo = redisTemplate.opsForValue().get("geo:" + accountId);
        if (isGeoAnomaly(geo, evt)) {
            emitFraudAlert(accountId, "GEO_ANOMALY", geo);
        }
        
        // 3. Amount Outlier
        var stats = redisTemplate.opsForValue().get("stats:" + accountId);
        if (isAmountOutlier(evt.getAmount(), stats)) {
            emitFraudAlert(accountId, "AMOUNT_OUTLIER", evt.getAmount());
        }
    }
}
```

## Fraud Alert Event DTO

```json
{
  "eventId": "uuid-v4",
  "eventType": "FraudAlert",
  "timestamp": "2024-01-15T14:30:00Z",
  "severity": "high",
  "data": {
    "accountId": "guid-123",
    "transactionId": "guid-456",
    "alertType": "VELOCITY|GEO_ANOMALY|AMOUNT_OUTLIER",
    "details": {
      "count": 12,
      "threshold": 10,
      "windowMinutes": 5
    }
  }
}
```

## Fraud Types & Actions

| Type | Detection | Auto Action |
| :--- | :--- | :--- |
| **VELOCITY** | > 10 txns/5min | Flag all recent txns, freeze account |
| **GEO_ANOMALY** | Country change vs 30d | Send alert, request PIN verification |
| **AMOUNT_OUTLIER** | > 3σ from mean | Flag txn, notify user |

## Notification Template (FraudAlert)

| Field | Value |
| :--- | :--- |
| Type | `FraudAlert` |
| Title | `Alerta de Segurança` |
| Message | `Atividade suspeita detectada em sua conta` |
| Priority | `high` |
| Action Link | `/security/fraud-review?alertId=xxx` |
| TTL | 24 hours |