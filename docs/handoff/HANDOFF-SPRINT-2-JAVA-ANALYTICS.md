# Handoff — Sprint 2: feature/java-analytics

**Data:** 2026-08-11  
**Autor:** Rafael Augusto Nascimento Novais  
**Branch destino:** `feature/java-analytics`

---

## 1. Contexto da Sprint 1 (Concluida)

O **Core Banking** em .NET esta funcional com:
- Autenticacao JWT com User-Account 1:1
- Transferencias seguras (origem via JWT, destino via token)
- Publicacao de eventos `TransferCompleted` no RabbitMQ
- 21 testes unitarios passando
- Docker Compose com PostgreSQL, Redis, RabbitMQ, Adminer

---

## 2. Objetivo da Branch `feature/java-analytics`

Criar o **Analytics Service** em Java Spring Boot 3 que:
1. **Consome eventos** `transfer.completed` do RabbitMQ
2. **Calcula agregacoes financeiras** (total transferido, volume por tipo, etc.)
3. **Armazena em cache** Redis com padrao Cache-Aside (TTL 5 min)
4. **Expo API REST** para consulta de insights: `GET /api/insights/{accountId}`

---

## 3. Estado Atual do RabbitMQ

### Evento Publicado (Sprint 1)

**Exchange:** `cashflow.events`  
**Routing Key:** `transfer.completed`  
**Payload (JSON):**
```json
{
  "fromAccountId": "uuid",
  "toAccountId": "uuid",
  "amount": 150.00
}
```

**Classe .NET de referencia:** `CoreBanking.Domain.Transaction.Events.TransferCompleted`

---

## 4. Estrutura Esperada do Projeto

```
src/Analytics/
├── src/main/java/com/cashflow/analytics/
│   ├── AnalyticsApplication.java          # @SpringBootApplication
│   ├── config/
│   │   └── RabbitMQConfig.java           # Filas, exchanges, listeners
│   ├── consumer/
│   │   └── TransferCompletedConsumer.java # @RabbitListener
│   ├── model/
│   │   ├── TransferEvent.java            # DTO do evento
│   │   └── AccountAnalytics.java         # Agregacoes por conta
│   ├── repository/
│   │   └── AnalyticsRepository.java      # Spring Data Redis
│   ├── service/
│   │   ├── AnalyticsService.java         # Logica de agregacao
│   │   └── CacheService.java             # Cache-Aside pattern
│   └── controller/
│       └── InsightsController.java       # REST endpoints
│   └── resources/
│       └── application.yml               # Configuracao
├── pom.xml                                # Dependencias
└── Dockerfile                             # Container
```

---

## 5. Configuracoes Necessarias

### 5.1. `application.yml`

```yaml
server:
  port: 5001

spring:
  application:
    name: analytics-service
  
  rabbitmq:
    host: ${RABBITMQ_HOST:localhost}
    port: 5672
    username: ${RABBITMQ_USER:cashflow}
    password: ${RABBITMQ_PASS:cashflow_pass}
  
  data:
    redis:
      host: ${REDIS_HOST:localhost}
      port: 6379

analytics:
  cache:
    ttl-minutes: 5
  queue:
    transfer-completed: cashflow.transfer.completed
  exchange:
    events: cashflow.events
```

### 5.2. `pom.xml` — Dependencias Principais

```xml
<dependencies>
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-web</artifactId>
    </dependency>
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-amqp</artifactId>
    </dependency>
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-data-redis</artifactId>
    </dependency>
    <dependency>
        <groupId>com.fasterxml.jackson.core</groupId>
        <artifactId>jackson-databind</artifactId>
    </dependency>
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-test</artifactId>
        <scope>test</scope>
    </dependency>
</dependencies>
```

---

## 6. Implementacao Esperada

### 6.1. Consumer RabbitMQ

```java
@Component
public class TransferCompletedConsumer {
    
    private final AnalyticsService analyticsService;
    
    @RabbitListener(queues = "${analytics.queue.transfer-completed}")
    public void handleTransferCompleted(TransferEvent event) {
        analyticsService.processTransfer(event);
    }
}
```

### 6.2. DTO do Evento

```java
public record TransferEvent(
    UUID fromAccountId,
    UUID toAccountId,
    BigDecimal amount
) {}
```

### 6.3. Servico de Agregacao

```java
@Service
public class AnalyticsService {
    
    public void processTransfer(TransferEvent event) {
        // 1. atualizar agregacoes da conta de origem
        // 2. atualizar agregacoes da conta de destino
        // 3. invalidar cache das duas contas
    }
    
    public AccountInsights getInsights(UUID accountId) {
        // buscar do cache ou calcular
    }
}
```

### 6.4. Cache-Aside Pattern

```java
@Service
public class CacheService {
    
    private final RedisTemplate<String, Object> redis;
    private static final Duration TTL = Duration.ofMinutes(5);
    
    public Object getOrCompute(UUID accountId, Supplier<Object> supplier) {
        String key = "insights:" + accountId;
        Object cached = redis.opsForValue().get(key);
        if (cached != null) return cached;
        
        Object computed = supplier.get();
        redis.opsForValue().set(key, computed, TTL);
        return computed;
    }
    
    public void invalidate(UUID accountId) {
        redis.delete("insights:" + accountId);
    }
}
```

### 6.5. Controller REST

```java
@RestController
@RequestMapping("/api/insights")
public class InsightsController {
    
    @GetMapping("/{accountId}")
    public ResponseEntity<AccountInsights> getInsights(@PathVariable UUID accountId) {
        return ResponseEntity.ok(analyticsService.getInsights(accountId));
    }
}
```

---

## 7. Criterios de Aceite

| # | Criterio | Como Verificar |
|---|----------|----------------|
| 1 | Consumer conecta ao RabbitMQ | Logs mostram conexao bem-sucedida |
| 2 | Evento e consumido | Transferencia no Core Banking gera log no Analytics |
| 3 | Agregacoes calculadas corretamente | Verificar valores apos multiplas transferencias |
| 4 | Cache Redis funciona (TTL 5 min) | Segunda request e mais rapida; expira apos 5 min |
| 5 | Cache invalidado em novo evento | Apos nova transferencia, cache eh recarregado |
| 6 | Endpoint retorna 200 | Teste no Postman/Scalar |
| 7 | Testes unitarios passando | `mvn test` → todos passando |

---

## 8. Configuracao Docker Compose

Adicionar ao `docker-compose.yml`:

```yaml
  analytics:
    build:
      context: .
      dockerfile: src/Analytics/Dockerfile
    container_name: cashflow-analytics
    environment:
      SPRING_RABBITMQ_HOST: rabbitmq
      SPRING_RABBITMQ_USERNAME: cashflow
      SPRING_RABBITMQ_PASSWORD: cashflow_pass
      SPRING_DATA_REDIS_HOST: redis
    ports:
      - "5001:5001"
    depends_on:
      rabbitmq:
        condition: service_healthy
      redis:
        condition: service_healthy
```

---

## 9. Testes Obrigatorios (JUnit)

```java
@SpringBootTest
class AnalyticsServiceTest {
    
    @Test
    void processTransfer_ShouldUpdateSenderAggregations() { }
    
    @Test
    void processTransfer_ShouldUpdateReceiverAggregations() { }
    
    @Test
    void getInsights_ShouldReturnFromCache_WhenExists() { }
    
    @Test
    void getInsights_ShouldCompute_WhenCacheMiss() { }
    
    @Test
    void cache_ShouldExpireAfterTTL() { }
}
```

---

## 10. Comandos para Iniciar

```bash
# 1. Criar branch a partir do master
git checkout master
git checkout -b feature/java-analytics

# 2. Verificar infraestrutura rodando
docker compose ps

# 3. Build e run do Analytics (apos implementar)
cd src/Analytics && ./mvnw spring-boot:run

# 4. Testar endpoint
curl http://localhost:5001/api/insights/{accountId}
```

---

## 11. Referencias

| Documento | Caminho |
|-----------|---------|
| System Design | `docs/architecture/system-design.md` |
| Event-Driven Architecture | `docs/architecture/event-driven.md` |
| ADR-002 (RabbitMQ) | `docs/ADR/ADR-002-rabbitmq-event-publisher.md` |
| Java Backend Guide | `docs/backend/java-analytics-ai.md` |
| Development Standards | `docs/workflow/development-standards.md` |
| AGENTS.md (visao geral) | `AGENTS.md` |

---

## 12. Proximos Passos (Apos esta Branch)

1. **`feature/redis-cache`** — Cache mais sofisticado (sliding window)
2. **`feature/notifications-signalr`** — WebSocket em tempo real
3. **`feature/angular-frontend`** — Dashboard Angular
4. **`feature/e2e-playwright`** — Testes end-to-end

---

**Bom trabalho na Sprint 2!**
