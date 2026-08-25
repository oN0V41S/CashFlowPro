# Diagramas de Sequência — CashFlow Pro

Este diretório contém diagramas de sequência Mermaid para os fluxos-chave da arquitetura CashFlow Pro.

## Índice

| Arquivo | Título | Descrição |
| :--- | :--- | :--- |
| **[01-financial-transfer.md](01-financial-transfer.md)** | Financial Transfer Flow | Fluxo síncrono de transferência bancária com transação ACID |
| **[02-event-processing.md](02-event-processing.md)** | Event Processing Flow | Processamento assíncrono de eventos via RabbitMQ |
| **[03-ai-insights-generation.md](03-ai-insights-generation.md)** | AI Insights Generation | Cache-Aside + integração com Gemini API |
| **[04-realtime-notification-delivery.md](04-realtime-notification-delivery.md)** | Real-time Notification Delivery | WebSocket SignalR para notificações em tempo real |
| **[05-user-authentication.md](05-user-authentication.md)** | User Authentication Flow | Fluxo JWT de login, validação e refresh |
| **[06-account-creation.md](06-account-creation.md)** | Account Creation Flow | Criação atômica de usuário + conta |
| **[07-fraud-detection-alert.md](07-fraud-detection-alert.md)** | Fraud Detection Alert | Detecção de fraude em tempo real (velocidade, geo, valor) |
| **[08-health-score-calculation.md](08-health-score-calculation.md)** | Health Score Calculation | Cálculo do Health Score financeiro e exposição via API |

## Como Visualizar

### Opção 1 — GitHub/GitLab
Abra qualquer um dos arquivos `.md` diretamente no repositório. O GitHub/GitLab renderiza automaticamente o Mermaid.

### Opção 2 — Mermaid Live Editor
1. Acesse https://mermaid.live/
2. Copie o conteúdo dos blocos ```mermaid ``` de um arquivo
3. Cole no editor e visualize

### Opção 3 — VS Code
Instale a extensão **"Markdown Preview Mermaid Support"** e use `Ctrl+Shift+V` para preview.

## Convenções

| Convenção | Aplicação |
| :--- | :--- |
| **Autonumber** | Numeração automática de passos |
| **Actor** | Usuário final (interações diretas) |
| **Participant** | Serviços, bancos, infraestrutura |
| **Rect blocks** | Agrupam por fase (Async/Sync/Real-time) |
| **Alt blocks** | Decisões condicionais (if/else) |

## Atualizações

Os diagramas podem ser atualizados conforme:
- Novos eventos são adicionados ao sistema
- Mudanças nas regras de negócio
- Novas integrações (ex: Stripe, Pix)

> **Dica**: Mantenha os diagramas sincronizados com a documentação de ADRs em `docs/ADR/`