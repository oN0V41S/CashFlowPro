# Sequência 8 — Health Score Calculation Flow

Cálculo e exposição do Health Score financeiro baseado em eventos de transação.

```mermaid
sequenceDiagram
    autonumber
    actor User as Usuário (Rafael)
    participant SPA as Angular SPA
    participant GW as API Gateway
    participant IC as InsightController
    participant CSC as CacheService (Redis)
    participant ASC as AggregationService
    participant RMQ as RabbitMQ
    participant CB as Core Banking
    participant DB as PostgreSQL

    Note over User,DB: Health Score – Event-Driven Aggregation

    rect rgb(248, 242, 240)
        Note left of CB: Event Publishing
    end
    
    CB->>RMQ: Publish TransferCompleted Event
    
    rect rgb(240, 248, 255)
        Note right of RMQ: Health Score Update
    end
    
    RMQ->>ASC: Queue: analytics.events<br/>TransferCompleted
    ASC->>ASC: Deserialize Event
    ASC->>ASC: Calculate:
        - Savings Rate (income - expenses) / income
        - Debt-to-Income Ratio
        - Spending Volatility (stdDev of daily amounts)
        - Recent Transfer Frequency
    ASC->>ASC: Weighted Formula:
        HS = 0.4*Savings + 0.3*Liquidity + 0.2*Stability + 0.1*Consistency
    ASC->>CSC: SET health:{accountId} = {score, factors, generatedAt}
    CSC-->>ASC: OK (TTL = 1h)
    
    rect rgb(255, 248, 240)
        Note bottom of ASC: Score Calculation Rules
    end
    
    asc->>DB: Query Account Balance
    asc->>DB: Query Monthly Transactions
    asc->>DB: Query Recurring Expenses
    asc->>DB: Query Income (if available)
    asc->>DB: Calculate Metrics
    
    rect rgb(248, 242, 240)
        Note left of IC: API Request Path
    end
    
    User->>SPA: Opens Health Dashboard
    SPA->>GW: GET /api/health-score/{accountId}<br/>Authorization: Bearer {JWT}
    
    GW->>IC: Forward Request
    IC->>CSC: GET health:{accountId}
    
    alt Cache HIT (fresh, TTL not expired)
        CSC-->>IC: RETURN cached HealthScore
        IC-->>GW: HTTP 200 + cached data
        GW-->>SPA: Response
        SPA->>SPA: Render Health Dashboard (gauge 87/100)
    else Cache MISS
        CSC-->>IC: NOT FOUND
        IC->>ASC: recalculateHealthScore(accountId)
        ASC->>DB: Complex aggregation query
        ASC-->>IC: HealthScore {score, factors[], suggestions[]}
        IC->>CSC: SET health:{accountId} = HealthScore (TTL = 1h)
        IC-->>GW: HTTP 200 + fresh HealthScore
        GW-->>SPA: Response
        SPA->>SPA: Render Health Dashboard
    end

    rect rgb(240, 248, 255)
        Note bottom of SPA: UI Visualization
    end
    
    SPA->>SPA: Display Circular Gauge (87/100)
    SPA->>SPA: Factor Cards:
        - Savings Rate: 25%
        - Liquidity: 45 days
        - Score: 87 (Excelente)
    SPA->>SPA: Suggestions List:
        - "Aumente sua reserva de emergência para 3-6 meses"
        - "Reduza gastos com delivery em 15%"
    SPA->>User: Show Dashboard
```

## Health Score Calculation Algorithm

### Input Data
1. **Account Balance** - saldo atual
2. **Monthly Transactions** - todas as transações dos últimos 30 dias
3. **Recurring Expenses** - categorias fixas (aluguel, contas, assinaturas)
4. **Income Data** (opcional) - rendimento mensal

### Scoring Factors (0-100 each)

| Fator | Peso | Fórmula | Descrição |
| :--- | :--- | :--- | :--- |
| **Poupança** | 40% | `(income - expenses) / income * 100` | Percentual do que você pouca |
| **Liquidez** | 30% | `Balance / Average Daily Expenses` | Dias de gasto com o que tem |
| **Estabilidade** | 20% | `1 - (stdDev(transactions) / mean)` | Previsibilidade dos gastos |
| **Consistência** | 10% | `Streak days with on-time payments` | Regularidade em pagamentos |

### Health Score Output

```json
{
  "score": 87,
  "grade": "Excelente",
  "factors": {
    "savingsRate": {
      "value": 25.0,
      "weight": 0.4,
      "contribution": 10.0,
      "max": 40.0
    },
    "liquidity": {
      "value": 45.0,
      "weight": 0.3,
      "contribution": 13.5,
      "max": 30.0
    },
    "stability": {
      "value": 88.0,
      "weight": 0.2,
      "contribution": 17.6,
      "max": 20.0
    },
    "consistency": {
      "value": 90.0,
      "weight": 0.1,
      "contribution": 9.0,
      "max": 10.0
    }
  },
  "suggestions": [
    "Sua reserva de emergência é ótima",
    "Considere investir em renda fixa"
  ],
  "generatedAt": "2024-01-15T15:00:00Z",
  "validUntil": "2024-01-15T16:00:00Z"
}
```

## Redis Cache Structure

```redis
# Health Score
SET health:{accountId} "{json}" EX 3600

# Aggregations for recalculation
HSET agg:monthly:{accountId}:{yyyy-MM}
  - income: 5000.00
  - expenses: 3750.00
  - transaction_count: 42
  - avg_daily: 124.50
  - std_dev: 45.30

# Category breakdown
HMSET cat:food:{accountId} total:150.00 count:8
HMSET cat:transport:{accountId} total:200.00 count:12
```

## API Endpoint

**GET** `/api/health-score/{accountId}`

### Response
```json
{
  "score": 87,
  "grade": "Excelente",
  "factors": [...],
  "suggestions": [...],
  "lastUpdated": "2024-01-15T15:00:00Z"
}
```

### Grades Mapping
| Score | Grade |
| :--- | :--- |
| 80-100 | Excelente |
| 65-79 | Bom |
| 50-64 | Regular |
| 0-49 | Crítico |