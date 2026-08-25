# Sequência 4 — Real-time Notification Delivery Flow

Fluxo de entrega de notificações em tempo real via SignalR WebSocket.

```mermaid
sequenceDiagram
    autonumber
    participant CB as Core Banking
    participant RMQ as RabbitMQ
    participant NC as Notifications Consumer
    local: NotificationHub (SignalR)
    participant RB as Redis Backplane
    participant SPA as Angular SPA
    participant PG as PostgreSQL (Notification History)

    Note over CB,SPA: Real-time Notification – WebSocket SignalR

    rect rgb(255, 248, 240)
        Note left of CB: Event Publishing (Async)
    end
    
    CB->>RMQ: Publish TransferCompleted (AMQP)
    RMQ->>RMQ: Route to: notifications.events
    
    rect rgb(240, 248, 255)
        Note right of RMQ: Message Delivery
    end
    
    RMQ->>NC: Delivery to Queue: notifications.events
    
    rect rgb(248, 242, 240)
        Note over NC: Notification Processing
    end
    
    NC->>NC: Deserialize TransferCompletedEvent
    NC->>NC: Build NotificationDto:
        {
          "type": "TransferReceived",
          "title": "R$ 150,00 recebidos!",
          "message": "De: João Silva",
          "payload": {"amount": 150.00, "fromName": "João Silva"},
          "priority": "high"
        }
    
    alt Single Instance (no scaling)
        NC->>Hub: Clients.Group(toAccountId).SendAsync(dto)
    else Multiple Instances (scaled)
        NC->>RB: PUBSUB: cashflow:signalr:channel (Redis)
        RB-->>Hub: Message broadcast to all instances
    end
    
    Hub->>SPA: WebSocket: ReceiveNotification(dto)
    
    SPA->>SPA: Receive callback
    SPA->>SPA: toastService.show(dto.title, dto.message)
    SPA->>SPA: notificationService.add(dto)
    SPA->>SPA: Update notification badge count
    SPA->>User: Snackbar aparece no canto superior direito
    
    alt Persist notification (optional)
        SPA->>PG: INSERT NotificationHistory
        PG-->>SP: Confirmation
    end

    rect rgb(225, 245, 255)
        Note top of SPA: Low Latency Path
    end
```

## SignalR Hub Implementation (.NET)

```csharp
[Authorize]
public class NotificationHub : Hub {
    public async Task JoinAccountGroup(string accountId) {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"account:{accountId}");
    }
    
    public async Task LeaveAccountGroup(string accountId) {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"account:{accountId}");
    }
    
    public override async Task OnConnectedAsync() {
        var accountId = Context.User.FindFirst("accountId")?.Value;
        if (!string.IsNullOrEmpty(accountId)) {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"account:{accountId}");
            // Broadcast presence
            await Clients.All.SendAsync("userPresence", accountId, "online");
        }
        await base.OnConnectedAsync();
    }
}
```

## Connection Mapping (Redis)

```csharp
service {
    "connectionId": "hub-abc123",
    "accountId": "guid-123",
    "userId": "user-xyz",
    "connectedAt": "2024-01-15T10:00:00Z",
    "ttl": 30  // seconds (heartbeat)
}
```

**Key Pattern:** `cashflow:connections:{accountId} → Set{connectionIds}`

## Frontend Angular Integration

```typescript
// notification.service.ts
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private hubConnection: HubConnection;
  private _notifications = new BehaviorSubject<NotificationDto[]>([]);
  
  constructor() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/notifications`, {
        accessTokenFactory: () => this.authService.getToken()
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (context) => Math.min(1000 * 2 ** context.retryCount, 30000)
      })
      .build();
    
    this.hubConnection.on('ReceiveNotification', (dto) => {
      const current = this._notifications.getValue();
      this._notifications.next([dto, ...current]);
      this.showToast(dto);
    });
  }
  
  async connect(accountId: string) {
    await this.hubConnection.start();
    await this.hubConnection.invoke('JoinAccountGroup', accountId);
  }
}

// component.ts
notifications$ = this.notificationService.notifications.asObservable();
```

## Scale-out Architecture

```mermaid
graph LR
    A[Instance-1 (Port 5001)] --> B[Redis Channel]
    C[Instance-2 (Port 5002)] --> B
    D[Instance-N (Port 500N)] --> B
    E[Angular - User 1] --> A
    F[Angular - User 2] --> C
    G[Angular - User N] --> D
```

## Performance Targets

| Métrica | Meta | Atual |
| :--- | :--- | :--- |
| **Latência P99** | < 150ms (local) / < 300ms (cross-region) | - |
| **Throughput** | 100 req/s por instância | - |
| **Reconnection** | < 3s após disconnect | - |
| **Connections/Instância** | 2000 simultâneos | - |

## WebSocket Configuration

```json
// appsettings.json
"SignalR": {
  "HandshakeTimeout": "5s",
  "KeepAliveInterval": "10s",
  "ClientTimeoutInterval": "30s",
  "Transport": "WebSockets"
}
```

## Notification Templates

| Type | Title | Message Template | Priority |
| :--- | :--- | :--- | :--- |
| `TransferReceived` | "{amount:N2} recebidos!" | "De: {fromName}" | high |
| `TransferSent` | "Transferência enviada" | "Para: {toName}, {amount:N2}" | high |
| `FraudAlert` | "Alerta de Segurança" | "Atividade suspeita detectada" | high |
| `SystemAnnouncement` | "Manutenção Programada" | "Serviço em manutenção às {time}" | medium |