namespace CoreBanking.Domain.Accounts;

public class Account
{
    // Entity -> Had ID (PK)
    public Guid Id { get; private set; }
    public string HolderName { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public AccountType Type { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt {get; private set; }

    // ⚠️ Private Constructor - Nobody create Account without pass by Factory method
    private Account() {}

    public static Account Open(string holderName, AccountType type)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            HolderName = holderName,
            Type = type,
            Balance = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ✅ Business Behavior - Debit
    public void Debit (decimal amount)
    {
        // Rule: amount should not be 0.
        if (amount == 0)
            throw new ArgumentException("Valor deve ser Diferente de Zero");

        // Rule: It is not possible to withdraw more than the available balance + overdraft protection.
        if(Balance - amount < -500)
            throw new InvalidOperationException("Saldo Insuficiente (limite R$ 500 de cheque especial)");
        
        Balance -= amount;
    }

    // ✅ Business Behavior - Credit
    public void Credit(decimal amount)
    {
        // Rule: credit amount must be positive (> 0)
        if (amount == 0)
            throw new ArgumentException("Valor deve ser Diferente de Zero");
        else if (amount < 0)
            throw new ArgumentException("Valor deve ser Positivo");
        Balance += amount;
    }

    public void Close()
    {
        if (Balance != 0)
            throw new InvalidOperationException("Não é possível fechar conta com saldo pendente");
        IsActive = false;
    }


}