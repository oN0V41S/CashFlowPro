namespace CoreBanking.Domain.Accounts;

public class Account
{
    public Guid Id { get; private set; }
    public string HolderName { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public AccountType Type { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Account() {}

    public static Account Open(string holderName, AccountType type = AccountType.Checking)
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

    public void Debit(decimal amount)
    {
        if (amount == 0)
            throw new ArgumentException("Valor deve ser diferente de zero");

        if (Balance - amount < -500)
            throw new InvalidOperationException("saldo insuficiente (limite R$ 500 de cheque especial)");

        Balance -= amount;
    }

    public void Credit(decimal amount)
    {
        if (amount == 0)
            throw new ArgumentException("Valor deve ser diferente de zero");
        else if (amount < 0)
            throw new ArgumentException("Valor deve ser positivo");

        Balance += amount;
    }

    public void Close()
    {
        if (Balance != 0)
            throw new InvalidOperationException("Não é possível fechar conta com saldo pendente");
        IsActive = false;
    }
}
