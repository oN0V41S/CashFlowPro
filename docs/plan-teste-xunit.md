# Plano: Ambiente de Testes xUnit — CashFlow Pro (CoreBanking)

> **Demanda:** Configurar ambiente de testes unitários com xUnit para o serviço CoreBanking do projeto CashFlowPro, validando as regras de negócio do Domínio (DDD).
>
> **Status:** 📋 Rascunho para revisão — nenhuma alteração foi aplicada ainda.

---

## Estágio 0: SPEC (Especificação)

### Requisitos Funcionais

| REQ-ID | Descrição | Critério de Aceitação |
|--------|-----------|----------------------|
| REQ-TEST-001 | Criar projeto de testes `CoreBanking.Tests` | Projeto compila e é reconhecido pelo `dotnet test` |
| REQ-TEST-002 | Configurar xUnit + Moq + Coverlet | `dotnet test` executa sem erros |
| REQ-TEST-003 | Testar entidade `Account` (regras de negócio + validação) | 11 casos de teste cobrindo crédito, débito, fechamento, validação de entrada |
| REQ-TEST-004 | Testar entidade `Transaction` (criação + lógica + validação) | 7 casos de teste cobrindo geração de Id, descrição, validação de amount |
| REQ-TEST-005 | Testar Value Object `Money` | 5 casos de teste cobrindo construtor, Add, igualdade, moeda diferente |
| REQ-TEST-006 | Gerar relatório de cobertura | Comando `dotnet test /p:CollectCoverage=true` funciona |

### Não-Funcionais

- Testes devem ser isolados (sem dependência de banco/infra)
- Tempo de execução < 5s para a suíte completa
- Cobertura mínima de 80% nas classes de Domínio

---

## Estágio 1: PLAN (Decomposição)

| Tarefa | REQ | Dependência | Arquivo |
|--------|-----|-------------|---------|
| 1. Criar estrutura de pastas `tests/CoreBanking.Tests/` | REQ-TEST-001 | — | `mkdir -p tests/CoreBanking.Tests/Domain/{Accounts,Transactions}` |
| 2. Criar `.csproj` do projeto de teste | REQ-TEST-001, 002 | Tarefa 1 | `tests/CoreBanking.Tests/CoreBanking.Tests.csproj` |
| 3. Adicionar `ProjectReference` ao CoreBanking | REQ-TEST-001 | Tarefa 2 | `CoreBanking.Tests.csproj` |
| 4. Criar `Usings.cs` com imports globais | REQ-TEST-002 | Tarefa 2 | `tests/CoreBanking.Tests/Usings.cs` |
| 5. Implementar `AccountTests.cs` | REQ-TEST-003 | Tarefa 3 | `tests/CoreBanking.Tests/Domain/Accounts/AccountTests.cs` |
| 6. Implementar `TransactionTests.cs` | REQ-TEST-004 | Tarefa 3 | `tests/CoreBanking.Tests/Domain/Transactions/TransactionTests.cs` |
| 7. Implementar `MoneyTests.cs` | REQ-TEST-005 | Tarefa 3 | `tests/CoreBanking.Tests/Domain/Transactions/MoneyTests.cs` |
| 8. Registrar projeto na Solution | REQ-TEST-001 | Tarefa 2 | `CashFlowPro.sln` |
| 9. Executar suíte e validar cobertura | REQ-TEST-006 | Tarefas 5-7 | Terminal |

---

## Estágio 2: ANALYZE (Análise)

### Arquivos a Analisar

| Arquivo | O que verificar |
|---------|-----------------|
| `src/CoreBanking/CoreBanking.csproj` | TargetFramework (net8.0), ImplicitUsings, Nullable |
| `src/CoreBanking/Domain/Accounts/Account.cs` | Métodos `Open`, `Credit`, `Debit`, `Close` e regras |
| `src/CoreBanking/Domain/Accounts/AccountType.cs` | Enum para testes de tipo |
| `src/CoreBanking/Domain/Transaction/Transaction.cs` | Método `Create` e lógica de `Description` |
| `src/CoreBanking/Domain/Transaction/Money.cs` | `readonly record struct Money(decimal Amount, string Currency)`, método `Add(Money)`, validação de moeda |

### Riscos de Segurança
- **Baixo**: Testes unitários não expõem superfície de ataque. Nenhum dado sensível envolvido.
- **Atenção**: Evitar commit de `coverage/` ou `TestResults/` no git (já coberto pelo `.gitignore`?).

### Impacto Arquitetural
- Nenhum impacto no código de produção (CoreBanking.csproj não é alterado).
- Apenas adiciona projeto irmão em `tests/`.

---

## Estágio 3: BUILD (Implementação)

### Ordem de Criação

1. **Estrutura**: `tests/CoreBanking.Tests/` + subpastas
2. **CoreBanking.Tests.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\CoreBanking\CoreBanking.csproj" />
  </ItemGroup>
</Project>
```
3. **Usings.cs**:
```csharp
global using Xunit;
global using FluentAssertions;
global using CoreBanking.Domain.Accounts;
global using CoreBanking.Domain.Transaction;
```
4. **AccountTests.cs** — 11 testes (Open válido, Open nulo/vazio, Credit +ve, Credit -ve/zero, Debit, Debit overdraft, Debit limite exato, Debit valor inválido, Close, Close com saldo, Credit/Debit após Close)
5. **TransactionTests.cs** — 7 testes (Create Id, Description default por tipo, Description custom, Description string vazia, ToAccountId transfer, ToAccountId null, Amount inválido)
6. **MoneyTests.cs** — 5 testes (construtor com moeda, Add, Add moeda diferente, igualdade record, ToString)
7. **Solution**: `dotnet sln add tests/CoreBanking.Tests/CoreBanking.Tests.csproj`

---

## Estágio 4: REVIEW (Validação)

| Validação | Comando | Esperado |
|-----------|---------|----------|
| Compilação | `dotnet build tests/CoreBanking.Tests` | 0 erros |
| Execução | `dotnet test` | 23 testes passando |
| Verbose | `dotnet test --logger "console;verbosity=detailed"` | Saída por teste |
| Cobertura | `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Threshold=80` | % exibido, >80% no Domínio, falha se abaixo |

### Cobertura de Requisitos

| REQ | Teste Correspondente | Status |
|-----|---------------------|--------|
| REQ-TEST-001 | Projeto criado e compila | ⬜ |
| REQ-TEST-002 | xUnit executa | ⬜ |
| REQ-TEST-003 | AccountTests (11) | ⬜ |
| REQ-TEST-004 | TransactionTests (7) | ⬜ |
| REQ-TEST-005 | MoneyTests (5) | ⬜ |
| REQ-TEST-006 | Coverlet report com threshold | ⬜ |

---

## Estágio 5: DOCUMENT (Documentação)

| Arquivo | Atualização |
|---------|-------------|
| `CashFlowPro/AGENTS.md` | Seção "🧪 Ambiente de Testes" já existe — confirmar que comandos batem |
| Notion "BootCamp CashFlow Pro" | Seção já adicionada na sessão anterior |
| `docs/plan-teste-xunit.md` | Este arquivo — marcar como concluído |

---

## Sugestões de Correção (Revisão do Plano)

1. **FluentAssertions**: Adicionado ao `.csproj` (v6.12.0) e `Usings.cs`.
2. **Money.cs — CORRIGIDO**: A classe `Money` é um `readonly record struct` com construtor `Money(decimal Amount, string Currency)` (dois parâmetros). Usa método `Add()`, não operador `+`. Testes atualizados.
3. **AccountType**: Confirmado — enum tem `Checking=1`, `Savings=2`.
4. **Overdraft**: Limite -500 confirmado em `Account.Debit`. Teste `Debit_ShouldAllowWithinOverdraftLimit` valida valor exato -500.
5. **Validação de Entrada (NOVO)**: Adicionados testes para `holderName` nulo/vazio, `amount <= 0` em Credit/Debit/Transaction, `description` string vazia vs null.
6. **Threshold de Cobertura (NOVO)**: Comando `dotnet test` agora inclui `/p:Threshold=80` para falhar se cobertura < 80%.
7. **Convenção de Nomenclatura (NOVO)**: Padrão `Metodo_Cenario_ResultadoEsperado` (ex: `Debit_WhenAmountExceedsOverdraft_ThrowsInvalidOperationException`).

---

## Próximos Passos

1. Usuário aprova este plano
2. Executar estágios 3-5 (BUILD → REVIEW → DOCUMENT)
3. Reportar resultado dos 23 testes (11 Account + 7 Transaction + 5 Money)

**Aguardando aprovação antes de aplicar qualquer alteração.**
