# Sequência 2 — Event Processing Flow

Fluxo assíncrono de processamento de eventos do RabbitMQ por Analytics e Notifications.

```mermaid
sequenceDiagram
    autonumber
    participant CB as Core Banking
    participant RMQ as RabbitMQ
    participant AN as Analytics Consumer
    participant ACS as AggregationService
    participant CS as CacheService
    participant DB as Redis
    participant NT as Notifications Consumer
    participant HUB as NotificationHub
    participant SPA as Angular SPA

    Note over CB,SPA: Event Processing – Async Event-Driven

    rect rgb(255, 248, 240)
        Note left of CB: Event Publishing
    end
    
    CB->>RMQ: Publish TransferCompleted (Exchange: cashflow.events)
    RMQ->>RMQ: Route to: analytics.events (routing key: transfer.completed)
    RMQ->>RMQ: Route to: notifications.events (routing key: transfer.completed)
    
    rect rgb(240, 248, 255)
        Note right of RMQ: Async Consumers
    end
    
    RMQ->>AN: Delivery to Queue: analytics.events
    AN->>AN: Deserialize message → TransferCompletedEvent
    AN->>ACS: processTransfer(event)
    
    ACS->>CS: updateAggregations(accountId, amount)
    CS->>DB: PUT insights:{accountId} (async)
    CS->>DB: DEL health:{accountId} (invalidate)
    CS->>DB: DEL agg:monthly:{accountId}:{month} (invalidate)
    
    RMQ->>NT: Delivery to Queue: notifications.events
    NT->>NT: Deserialize message
    NT->>NT: Map to NotificationDto:
        {
          type: "TransferReceived",
          title: "Recebido R$ 150,00",
          message: "João Silva enviou para você",
          payload: { amount: 150.00, fromAccount: "João Silva" }
        }
    NT->>HUB: Clients.Group(toAccountId).SendAsync("ReceiveNotification", dto)
    
    HUB->>DB: PUBSUB: cashflow:signalr:publish (backplane to other instances)
    HUB->>SPA: WebSocket: ReceiveNotification (via SignalR)
    
    rect rgb(248, 242, 240)
        Note over SPA: Real-time UI Update
    end
    
    SPA->>SPA: Append notification to list (toast)
    SPA->>SPA: Update balances (transfer)
    SPA->>SPA: Check for stale cache → Trigger refresh
```

## Consumer Setup (Spring AMQP)

```java
@Service
public class AnalyticsConsumer {
    @RabbitListener(queues = "analytics.events")
    public void handleTransferCompleted(TransferCompletedEvent event) {
        aggregationService.processTransfer(event);
        cacheService.invalidateRelatedCaches(event.getToAccountId());
    }
}
```

## Notification Consumer (.NET)

```csharp
public class RabbitMQConsumer : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken ct) {
        await _channel.BasicConsumeAsync("notifications.events", processEventAsync, ct);
    }
    
    private async Task processEventAsync(BasicDeliverEventArgs args) {
        var evt = JsonSerializer.Deserialize<TransferCompletedEvent>(args.Body);
        var dto = MapToNotification(evt);
        await _hubContext.Clients.Group(evt.ToAccountId.ToString())
            .SendAsync("ReceiveNotification", dto);
    }
}
```

## Cache Invalidation Keys

| Key Pattern | Action | Trigger |
| :--- | :--- | :--- |
| `insights:{accountId}` | DEL | TransferCompleted, AccountCreated |
| `health:{accountId}` | DEL | TransferCompleted |
| `agg:monthly:{accountId}:{yyyy-MM}` | DEL | TransferCompleted |
| `presence:{accountId}` | EXPIRE (TTL 30s) | `Ping` messages via SignalR |

## RabbitMQ Configuration

```yaml
rabbitmq:
  exchanges:
    - name: cashflow.events
      type: topic
  queues:
    - name: analytics.events
      bindings:
        - routingKey: transfer.completed
          route: cashflow.events
    - name: notifications.events
      bindings:
        - routingKey: transfer.completed
          route: cashflow.events
```