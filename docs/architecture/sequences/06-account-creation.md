# Sequência 6 — Account Creation Flow

Fluxo de criação de usuário e conta com transação atômica.

```mermaid
sequenceDiagram
    autonumber
    actor User as Usuário (Rafael)
    participant SPA as Angular SPA
    participant GW as API Gateway
    participant Auth as AuthService
    participant CB as Core Banking
    participant DB as PostgreSQL
    participant RMQ as RabbitMQ
    participant AN as Analytics Consumer
    participant NT as Notifications Consumer

    Note over User,NT: Account Creation – Atomic User + Account

    rect rgb(248, 242, 240)
        Note left of Auth: Registration Phase
    end
    
    User->>SPA: Acessa /register
    SPA->>SPA: Exibe formulário (name, email, password, phone)
    
    User->>SPA: Submete formulário
    SPA->>GW: POST /api/auth/register<br/>{name, email, password, phone}
    
    GW->>Auth: Forward request
    Auth->>Auth: Validar:
        - Email único
        - Senha forte (min 8 chars, uppercase, number)
    
    rect rgb(240, 248, 255)
        Note right of CB: Atomic Transaction
    end
    
    Auth->>CB: Initiate Registration
    CB->>DB: BEGIN TRANSACTION
    CB->>DB: INSERT INTO [User] (Id, Email, PasswordHash, Name, Role, CreatedAt)
    CB->>DB: INSERT INTO [Account] (Id, UserId, HolderName, Balance=0, Type="checking", IsActive=true, CreatedAt)
    CB->>DB: UPDATE User SET AccountId = NEW_ACCOUNT.Id (FK + UNIQUE constraint)
    CB->>DB: COMMIT
    CB->>CB: Generate JWT (accountId = new account)
    
    alt Transaction Success
        CB->>RMQ: Publish AccountCreated Event
        CB-->>Auth: Return User + Account + JWT
        Auth-->>GW: HTTP 201 Created<br/>{user, account, accessToken}
        GW-->>SPA: Response
        SPA->>SPA: Store JWT, redirect to dashboard
    else Transaction Failed
        CB->>DB: ROLLBACK
        CB-->>Auth: HTTP 500
        Auth-->>GW: HTTP 500
        GW-->>SPA: {error: "Falha ao criar conta"}
    end

    rect rgb(248, 242, 240)
        Note left of RMQ: Event Processing (Async)
    end
    
    RMQ->>AN: Queue: analytics.events<br/>AccountCreated
    RMQ->>NT: Queue: notifications.events<br/>AccountCreated
    
    AN->>AN: Initialize default aggregations for accountId
    CS->>DB: SET health:{accountId} = 100 (baseline)
    
    NT->>SPA: WebSocket: "Bem-vindo ao CashFlow Pro!" (optional)
    
    rect rgb(255, 248, 240)
        Note right of SPA: Welcome Flow
    end
    
    SPA->>SPA: Show welcome modal/tutorial
    SPA->>User: UI: "Conta criada com sucesso!"
```

## Entity Schema

### User Table
```sql
CREATE TABLE "User" (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Name VARCHAR(255) NOT NULL,
    Phone VARCHAR(20),
    Role VARCHAR(20) DEFAULT 'user',
    AccountId UUID UNIQUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW()
);
```

### Account Table
```sql
CREATE TABLE "Account" (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId UUID NOT NULL REFERENCES "User"(Id),
    HolderName VARCHAR(255) NOT NULL,
    Type VARCHAR(20) DEFAULT 'checking',
    Balance DECIMAL(18,2) DEFAULT 0.00,
    OverdraftLimit DECIMAL(18,2) DEFAULT -500.00,
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW()
);

ALTER TABLE "User" ADD CONSTRAINT FK_User_Account_Id FOREIGN KEY (AccountId) REFERENCES "Account"(Id);
```

## Domain Model (DDD)

```csharp
// User.cs (Aggregate Root - Email Agency)
public class User : AggregateRoot {
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Name { get; private; set; }
    public AccountId AccountId { get; private set; }
    
    public void LinkAccount(AccountId accountId) {
        AccountId = accountId;
    }
}

// Account.cs (Aggregate Root)
public class Account : AggregateRoot {
    public AccountId Id { get; private set; }
    public UserId UserId { get; private set; }
    public string HolderName { get; private set; }
    public Money Balance { get; private set; }
    public Money OverdraftLimit { get; private set; }
    
    public void Apply(DomainEvent @event) {
        // Apply changes
    }
}
```

## Event Published (AccountCreated)

```json
{
  "eventId": "uuid-v4",
  "eventType": "AccountCreated",
  "timestamp": "2024-01-15T11:00:00Z",
  "data": {
    "accountId": "uuid-account-id",
    "userId": "uuid-user-id",
    "holderName": "Rafael Novais",
    "initialBalance": 0,
    "type": "checking"
  }
}
```

## Consumers Reaction

| Consumer | Ação | Resultado |
| :--- | :--- | :--- |
| **Analytics** | Initializa `health:{accountId} = 100`, `insights:{accountId} = {}` | Conta pronta para insights |
| **Notifications** | Envia "Welcome" toast | Experiência amigável |

## Validation Rules

| Regra | Código HTTP | Mensagem |
| :--- | :--- | :--- |
| Email já cadastrado | 409 Conflict | "Email já existe" |
| Senha fraca (< 8 chars) | 400 Bad Request | "Senha deve ter no mínimo 8 caracteres" |
| Domínio inválido | 400 Bad Request | "Formato de email inválido" |

## Service Layer

```csharp
// AuthService.cs
public class AuthService {
    private readonly IUserRepository _userRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req) {
        using var tx = await _dbContext.BeginTransactionAsync();
        try {
            var user = new User(req.Email, req.Name, req.Password);
            await _userRepo.AddAsync(user);
            
            var account = new Account(user.Id, req.HolderName ?? req.Name);
            await _accountRepo.AddAsync(account);
            
            user.LinkAccount(account.Id);
            await _userRepo.UpdateAsync(user);
            
            await _dbContext.CommitAsync(tx);
            
            var token = _tokenService.Generate(user.Id, account.Id);
            
            // Publish event (after commit for consistency)
            await _eventPublisher.Publish(new AccountCreatedEvent(...));
            
            return new AuthResponse(user, account, token);
        } catch (Exception) {
            await tx.RollbackAsync();
            throw;
        }
    }
}
```