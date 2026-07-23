# ADR-002: Implementação do Publisher de Eventos RabbitMQ e Estratégia de Testes

## Status
Aceito

## Contexto
O Core Banking precisa publicar eventos de domínio (como `TransferCompleted`) de forma assíncrona para outros microsserviços (Analytics e Notifications) através do RabbitMQ. 
Durante a implementação dos testes unitários com Moq, deparamo-nos com o erro de compilação `CS8640` ao tentar inspecionar o payload `ReadOnlyMemory<byte>` serializado.

## Decisão
1. **Padrão de Mensageria:** Utilizamos RabbitMQ com uma Exchange do tipo `Topic` (`cashflow-exchange`) para desacoplar a publicação dos eventos de domínio.
2. **Abstração:** Criamos a interface `IEventPublisher` para isolar a infraestrutura de mensageria da regra de negócio (DDD / Application Services).
3. **Estratégia de Testes com Moq:** Para validar o conteúdo binário/JSON enviado no payload `ReadOnlyMemory<byte>`, utilizamos `Match.Create<ReadOnlyMemory<byte>>(mem => ...)` em vez de `It.IsAny()`, contornando as limitações de Expression Trees com métodos de extensão/conversão como `.ToArray()`.

## Consequências
- **Positivas:** Desacoplamento perfeito entre Core Banking e RabbitMQ; testes unitários robustos que validam o payload exato sem acoplar a infraestrutura real.
- **Negativas:** Requer atenção ao testar tipos estruturados (`ReadOnlyMemory`) em frameworks de mock baseados em Expression Trees.
