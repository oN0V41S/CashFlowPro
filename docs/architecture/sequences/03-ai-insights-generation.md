# Sequência 3 — AI Insights Generation Flow

Fluxo híbrido: Cache-Aside + Gemini API para geração de insights financeiros personalizados.

```mermaid
sequenceDiagram
    autonumber
    actor User as Usuário (Rafael)
    participant SPA as Angular SPA
    participant GW as API Gateway
    participant AC as InsightController
    participant CS as CacheService (Redis)
    participant AD as AggregationData
    participant GA as GeminiAdapter
    participant G as Gemini API
    participant DB as Redis

    Note over User,G: AI Insights – Cache-Aside + LLM

    User->>SPA: Abre aba "Insights Financeiros"
    SPA->>GW: GET /api/insights/{accountId} (Authorization: Bearer {JWT})
    
    GW->>AC: Forward Request (HTTP/gRPC)
    
    AC->>CS: exists("insights:{accountId}")
    alt Cache Hit (TTL não expirou)
        CS-->>AC: RETURN cached JSON
        AC-->>GW: HTTP 200 + cached insights
        GW-->>SPA: Response
        SPA->>User: Exibe insights do cache
    else Cache Miss
        CS-->>AC: NOT FOUND
        AC->>AD: getAggregations(accountId)
        AD->>CS: GET insights_agg:{accountId}:monthly
        alt Aggregation Cache Hit
            CS-->>AD: RETURN aggregations
        else Aggregation Cache Miss
            AD->>DB: Query PostgreSQL (transactions last 90 days)
            DB-->>AD: RETURN aggregated data
            AD->>CS: SET insights_agg:{accountId}:monthly (TTL=2h)
        end
        
        AC->>GA: generateInsightsPrompt(aggregations)
        GA->>G: POST https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent
        
        Note over G: Prompt Template inclui:
        - Gasto total mês
        - Top 3 categorias
        - Saldo atual
        - Health Score
        - Transações recentes
        
        G->>G: LLM Processa
        G-->>GA: {candidates: [{content: {parts: [{text: "{insights": [...]}"]}}}
        
        GA->>GA: Parse JSON response
        GA->>CS: SET "insights:{accountId}" (TTL=1h)
        
        AC->>AC: Transform → InsightDTO
        AC-->>GW: HTTP 200 + fresh insights
        GW-->>SPA: Response
        SPA->>User: Exibe novos insights + gráficos
    end

    rect rgb(240, 248, 255)
        Note bottom of G: Gemini Generation
    end
    
    rect rgb(248, 242, 240)
        Note bottom of SPA: UI Render
    end
```

## Prompt Template (Gemini)

```json
{
  "contents": [
    {
      "role": "system",
      "parts": [
        "Você é um consultor financeiro especializado em produtividade. Baseado nos dados, gere 3 insights acionáveis."
      ]
    },
    {
      "role": "user",
      "parts": [
        "Dados: Saldo: R$ 2.850,00 | Gasto mensal: R$ 1.200,00 | Top Cat: Aluguel (R$ 800) | Saúde Financeira: 87/100 | Txn recentes: [Transfer R$ 150, Pizza R$ 45]"
      ]
    }
  ]
}
```

## Gemini API Response Transformation

**Raw LLM Response:**
```json
{
  "candidates": [{
    "content": {
      "parts": [{
        "text": "{\"insights\": [{\"title\": \"Reduza gastos com aluguel\", \"description\": \"Seu aluguel representa 66% do orçamento\", \"action\": \"Pesquisar imóveis 15% abaixo do atual\", \"priority\": \"high\"}"]}]}"
      }]
    }
  }]
}
```

**Transformed to InsightDTO:**
```csharp
public class InsightDto {
    public string Title { get; set; }
    public string Description { get; set; }
    public string Action { get; set; }
    public PriorityLevel Priority { get; set; } // high, medium, low
    public DateTimeOffset GeneratedAt { get; set; }
}
```

## Cache Keys & TTL

| Key | TTL | Invalidation Trigger |
| :--- | :--- | :--- |
| `insights:{accountId}` | 1h | TransferCompleted, AccountCreated |
| `insights_agg:{accountId}:monthly` | 2h | - |
| `health:{accountId}` | 30min | TransferCompleted |

## Error Handling

| Cenário | Ação | Resposta |
| :--- | :--- | :--- |
| **Gemini Timeout** | Retry 2x + exponential backoff | Return stale cache or generic fallback |
| **Gemini Rate Limit** | Circuit Breaker (5 raios falhos → 30s) | Return cached or error 503 |
| **Invalid JSON** | Log + return 500 | Internal error response |

## Endpoint Specification

**GET** `/api/insights/{accountId}`

```http
Authorization: Bearer <jwt>
Accept: application/json
```

**Response:**
```json
{
  "accountId": "guid-123",
  "generatedAt": "2024-01-15T14:30:00Z",
  "insights": [
    {
      "title": "Economize na categoria Aluguel",
      "description": "Aluguel é 66% de seu orçamento mensal",
      "action": "Pesquisar imóveis 15% abaixo do atual",
      "priority": "high"
    },
    {
      "title": "Controle gastos com delivery",
      "description": "Últimos 30 dias: R$ 340,00 em food delivery",
      "action": "Limitar a R$ 200,00 por mês via metas",
      "priority": "medium"
    }
  ],
  "source": "cache" | "llm"
}
```