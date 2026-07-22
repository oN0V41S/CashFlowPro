# ADR-002: Arquitetura Orientada a Eventos com RabbitMQ para o Core Banking

## Status
Aceito

## Contexto
O projeto **CashFlow Pro** simula um ambiente fintech enterprise onde múltiplos microsserviços precisam interagir entre si (ex: Core Banking, Analytics, Notifications). 
Precisávamos decidir como o **Core Banking (.NET)** comunicaria eventos de negócio críticos (como transferências concluídas ou contas criadas) para os demais microsserviços sem criar acoplamento síncrono e garantindo resiliência caso um serviço consumidor esteja temporariamente indisponível.

## Decisão
Adotamos uma **Arquitetura Orientada a Eventos (EDA)** utilizando o **RabbitMQ** como message broker (Event Bus). 
- O **Core Banking** atua como *Publisher*, publicando eventos de domínio padronizados (ex: `TransferCompleted`) em uma Exchange do tipo *Topic* (`cashflow-exchange`) logo após a consolidação ACID da transação no PostgreSQL.
- Os serviços consumidores (**Analytics em Java** e **Notifications em .NET + SignalR**) atuam como *Subscribers*, escutando filas dedicadas de forma assíncrona.

## Consequências
### Positivas:
- **Desacoplamento:** O Core Banking não conhece os serviços de Analytics ou Notificação, focando exclusivamente na sua regra de negócio core.
- **Resiliência:** Se o microsserviço de notificações cair, as mensagens ficam seguras nas filas duráveis do RabbitMQ até que o serviço se recupere.
- **Escalabilidade:** Permite processamento em background sem travar as requisições HTTP síncronas dos clientes (Angular).

### Negativas / Desafios:
- **Consistência eventual:** Consumidores processam os eventos de forma assíncrona, exigindo tratamento de idempotência e compensação quando necessário.
- **Complexidade operacional:** Adiciona a necessidade de gerenciar um broker de mensagens no ambiente local (via Docker Compose) e em produção.