# ⚙️ Core Banking — .NET (DDD + EF Core)

## Responsabilidade

Contas, transferências, ledger e eventos de domínio (DDD). Stack: ASP.NET Core 8+, EF Core, PostgreSQL.

## Estrutura (DDD)

```
src/CoreBanking/
├── Controllers/        # REST endpoints
├── Data/               # EF Core DbContext, Migrations
├── Domain/
│   ├── Accounts/       # Account aggregate
│   └── Transaction/    # Transaction value objects
├── Events/             # Domain events + RabbitMQ publishing
├── Models/             # DTOs, ViewModels
├── Services/           # Application services
└── Dockerfile
```

## Regras de Negócio (Account)

- Débito reduz saldo; saldo - valor < -500 → exceção (overdraft)
- Crédito aumenta saldo; valor <= 0 → exceção
- Fechamento só permitido com saldo zero
- Nova conta começa ativa com saldo 0

## Endpoints REST

- Criar conta
- Transferência
- Extrato
- Swagger/OpenAPI documentation

## Convenções .NET Obrigatórias

| Regra | Exemplo Correto | Exemplo Errado |
|-------|----------------|----------------|
| DbSet sempre no **plural** | `DbSet<Transaction> Transactions` | `DbSet<Transaction> Transaction` |
| Propriedade `Id` (não `ID`) | `public Guid Id` | `public Guid ID` |
| Métodos começam com **maiúscula** | `.HasMaxLength()`, `.IsRequired()` | `.hasMaxLength()`, `.isRequired()` |
| Namespace = caminho da pasta | `Domain.Transaction` | inconsistente |
| `Guid` (não `Gui` ou `GUID`) | `public Guid Id` | `public Gui Id` |

## Erros de Compilação .NET — Referência Rápida

| Erro | Causa | Correção |
|------|-------|----------|
| `MSB1003` | `.csproj`/`.sln` deletado ou movido | Recriar com `dotnet new` ou restaurar do git |
| `The type or namespace name 'X' could not be found` | Faltou `using` | Adicionar `using CoreBanking.Domain.X;` |
| `CS0117` | Nome de propriedade errado (`AccountID` vs `AccountId`) | Verificar se a propriedade existe |
| `CS1001` | Erro de sintaxe (`Gui` em vez de `Guid`) | Corrigir o tipo |

## Erros de Referência entre Arquivos

| Arquivo | Erro Comum | Correção |
|---------|-----------|----------|
| `TransferService.cs` | Usar `_db.Transaction` (singular) | DbSet é `_db.Transactions` (plural) |
| `TransferService.cs` | Faltam `using` para `Domain.Transaction` | Adicionar `using CoreBanking.Domain.Transaction;` |
| `TransactionConfiguration.cs` | `.hasMaxLength` (minúsculo) | C# é case-sensitive: `.HasMaxLength` |
| `TransactionConfiguration.cs` | Faltam `using` para `Account` | Adicionar `using CoreBanking.Domain.Accounts;` |
| `TransferCompleted.cs` | Faltou `namespace` | Adicionar `namespace CoreBanking.Domain.Transaction.Events;` |

## Erros de Lógica (compilam, mas funcionam errado)

| Erro | Problema | Correção |
|------|----------|----------|
| `TransferOut` duplicado no switch | Segundo caso deveria ser `TransferIn` | Trocar para `TransactionType.TransferIn => "Transferência Recebida"` |
| `Guid ID` e `Id = Guid.NewGuid()` | Propriedade (`ID`) diferente do uso (`Id`) | Padronizar: usar `Id` em ambos |
| `Gui AccountID` | Tipo `Gui` não existe | Usar `Guid AccountId` |

## Fluxo de Diagnóstico Autônomo

```
1. dotnet build src/CoreBanking
   ↓ erro?
2. Ler a PRIMEIRA linha do erro (a causa real)
   ↓ não entendeu?
3. Verificar:
   - O arquivo .csproj existe?
   - Os namespaces batem com os usings?
   - Os nomes das propriedades estão corretos?
   - C# é case-sensitive?
   ↓ ainda com erro?
4. Perguntar colando a MENSAGEM DO ERRO completa
```

## Implementação Sprint 1 (Completo)

### Domain Entities

```
src/CoreBanking/Domain/
├── Accounts/
│   ├── Account.cs          # Aggregate Root com factory method Open()
│   └── AccountType.cs      # Enum: Checking, Savings
├── Transaction/
│   ├── Transaction.cs      # Entity com factory method Create()
│   ├── Money.cs            # Value Object
│   └── Events/
│       └── TransferCompleted.cs  # Domain Event
└── User/
    └── User.cs             # Entity com factory method Register(accountId, ...)
```

### Entity Framework - Migrations

| Migration | Descrição |
|-----------|-----------|
| `InitialCreate` | Cria tabelas Accounts, Transactions |
| `AddAccountIdToUser` | Adiciona AccountId na tabela User (relacionamento 1:1) |

### API Endpoints

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/api/auth/register` | Cria User + Account, retorna JWT |
| POST | `/api/auth/login` | Autentica, retorna JWT |
| POST | `/api/transfers` | Realiza transferência entre contas |
| GET | `/api/accounts/{id}/statement` | Extrato da conta |

### Autenticação JWT

```json
// Claims incluídas no token
{
  "sub": "user-id",
  "email": "user@email.com",
  "role": "User",
  "accountId": "account-id",  // ← Identifica conta de origem
  "jti": "unique-token-id"
}
```

### Validação de Segurança (Transferência)

| Origem | Destino |
|--------|---------|
| `accountId` extraído do JWT (claim personalizado) | `toAccountToken` decodificado do body |
| Nunca confia no cliente | Valida assinatura JWT do token |

### Infraestrutura Docker

| Serviço | Porta | Container |
|---------|-------|-----------|
| PostgreSQL | 5432 | cashflow-postgres |
| Redis | 6379 | cashflow-redis |
| RabbitMQ | 5672/15672 | cashflow-rabbitmq |
| Core Banking | 5000 | cashflow-core-banking |
| Adminer | 8080 | cashflow-adminer |
