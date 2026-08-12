# 🅰️ Frontend — Angular 18 SPA

## Stack

- Angular 18 (Standalone Components, Signals)
- Dashboard com gráficos, transferências e insights
- Integração com API Gateway via HTTP

## Como Rodar

```bash
cd src/Frontend && npm start
```

## Integração Angular → Gateway → Serviços

```
Angular SPA → API Gateway (.NET) → Core Banking / Analytics / Notifications
```

- `POST /api/transfers` → transferência síncrona (ACID)
- `GET /api/insights` → dashboard de insights (com cache Redis)
- WebSocket/SignalR → notificações em tempo real sem refresh

## Real-time (SignalR)

O **Notifications Service (.NET + SignalR)** entrega notificações via WebSocket:

- 🔔 "R$ 150 recebidos!"
- Atualização de saldo em tempo real

## Estado com Signals

- Uso de **Signals** (Angular 18) para state management reativo
- Componentes standalone sem NgModules

## Testes

- **Unitário**: Jasmine + Karma (componentes, serviços, signals)
- **E2E**: Playwright (transferência, dashboard, fluxos completos)
