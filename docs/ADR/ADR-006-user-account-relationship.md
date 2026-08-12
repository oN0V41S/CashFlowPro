# ADR-006: Relacionamento User-Account 1:1

**Data:** 2026-08-11  
**Status:** Aceito  
**Decisores:** Rafael Augusto Nascimento Novais

## Contexto

No início do projeto, as entidades `User` e `Account` existiam sem qualquer relacionamento. O token JWT carregava apenas o `User.Id`, tornando impossível identificar de forma segura qual conta bancária pertencia ao usuário autenticado.

## Decisão

Adotar relacionamento **1:1 entre User e Account**, onde:
- Cada `User` possui exatamente uma `Account`
- A `Account` é criada automaticamente durante o registro do `User`
- O `AccountId` é incluído como claim no JWT para identificação segura

## Consequências

### Positivas
- **Simplicidade:** Sem complexidade de múltiplas contas por usuário
- **Segurança:** Token JWT identifica diretamente a conta de origem
- **Atomicidade:** User e Account criados na mesma transação
- **Performance:** Uma query resolve User + Account

### Negativas
- **Limitação:** Não suporta múltiplas contas (ex: Conta PJ)
- **Migração futura:** Se necessário múltiplas contas, requer refatoração

## Alternativas Consideradas

| Abordagem | Descartada porque |
|-----------|-------------------|
| 1:N (User → Múltiplas Accounts) | MVP não requer múltiplas conta |
| Account como Aggregate Root | Aumenta complexidade desnecessariamente |
| Tabela separada UserAccount | Over-engineering para o MVP |

## Implementação

```csharp
// Domain/User/User.cs
public class User
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; } // FK para Account
    // ...
    public static User Register(Guid accountId, string email, string passwordHash)
}

// Services/AuthService.cs
public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
{
    using var transaction = await _db.Database.BeginTransactionAsync();
    var account = Account.Open(request.Email.Split('@')[0]);
    _db.Accounts.Add(account);
    await _db.SaveChangesAsync();
    
    var user = User.Register(account.Id, request.Email, passwordHash);
    _db.Users.Add(user);
    await _db.SaveChangesAsync();
    
    await transaction.CommitAsync();
    return GenerateJwtToken(user);
}
```

## Referências

- [AGENTS.md](../../AGENTS.md) - Sprint 1
- [System Design](../architecture/system-design.md)
