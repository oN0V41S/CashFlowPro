# 🧪 Estratégia de Testes — CashFlow Pro

## Cobertura

Cobertura mínima de **80%** nas camadas de domínio e aplicação.

## Matriz de Testes

| Camada | Framework | O que testar |
|--------|-----------|-------------|
| .NET Unit | xUnit + Moq | Domain aggregates, services, eventos |
| .NET Integration | TestContainers | EF Core + PostgreSQL, RabbitMQ |
| Java Unit | JUnit + Mockito | Analytics services, AI adapter |
| Java Integration | TestContainers | Spring Data JPA + PostgreSQL, Redis |
| Angular | Jasmine + Karma | Componentes, serviços, signals |
| E2E | Playwright | Fluxos completos (transferência, dashboard) |

## Estrutura de Testes (.NET)

```
tests/
└── CoreBanking.Tests/
    ├── Domain/
    │   ├── Accounts/
    │   │   ├── AccountTests.cs        — Regras de negócio
    │   │   └── MoneyTests.cs          — Value Object
    │   └── Transactions/
    │       └── TransactionTests.cs    — Criação e lógica
    └── CoreBanking.Tests.csproj       — Projeto xUnit
```

## Casos de Teste — Account

| # | Teste | Regra |
|---|---|---|
| 1 | `Debit_ShouldDecreaseBalance` | Débito reduz saldo corretamente |
| 2 | `Debit_ShouldThrowWhenExceedingOverdraft` | Saldo - valor < -500 → exceção |
| 3 | `Credit_ShouldIncreaseBalance` | Crédito aumenta saldo corretamente |
| 4 | `Credit_ShouldThrowWhenNegative` | Valor <= 0 → exceção |
| 5 | `Close_ShouldThrowWhenBalanceNotZero` | Saldo pendente impede fechamento |
| 6 | `Open_ShouldCreateActiveAccount` | Nova conta começa ativa com saldo 0 |

## Casos de Teste — Transaction

| # | Teste | Regra |
|---|---|---|
| 1 | `Create_ShouldGenerateId` | Transaction.Create gera Id válido |
| 2 | `Create_ShouldAssignDefaultDescription` | Se description null, usa lógica do switch |
| 3 | `Create_ShouldAssignCustomDescription` | Se description fornecida, usa o valor |

## Regra de Ouro

Após cada implementação de regra de negócio, service, aggregate, value object ou endpoint, criar testes **de sucesso e de falha** (ex: `Debit_ShouldThrowWhenExceedingOverdraft`).

## Comandos

```bash
# .NET
dotnet test
dotnet test --logger "console;verbosity=detailed"
dotnet test /p:CollectCoverage=true /p:CoverletOutput=../coverage/
dotnet test tests/CoreBanking.Tests/

# Java
./mvnw test

# Angular
ng test
```
