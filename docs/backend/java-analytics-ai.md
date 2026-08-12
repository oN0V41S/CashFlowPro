# ☕ Analytics & AI — Java Spring Boot 3

## Responsabilidade

Insights financeiros, detecção de fraude, health score e cache. Stack: Spring Boot 3, Redis, Gemini API.

## Consumo de Eventos (RabbitMQ)

O serviço **consome eventos do RabbitMQ** publicados pelo Core Banking:

- `TransactionCreated` → atualiza agregações financeiras
- `TransferCompleted` → invalida cache de insights

## Redis (Cache-Aside)

Estratégia **Cache-Aside**:

1. GET /api/insights → verifica cache no Redis
2. **Cache hit** → retorna resposta cacheada
3. **Cache miss** → chama Gemini API, grava no Redis com TTL (ex: 1h)
4. Evento de transferência invalida os insights cacheados

## Gemini API (AI Insights)

- Prompts para insights financeiros e classificação inteligente
- Cache de prompts e respostas LLM no Redis
- Geração de dicas e análise de padrões de gastos

## Como Rodar

```bash
cd src/Analytics && ./mvnw spring-boot:run
```

## Testes

- **Unitário**: JUnit + Mockito (Analytics services, AI adapter)
- **Integração**: TestContainers (Spring Data JPA + PostgreSQL, Redis)
