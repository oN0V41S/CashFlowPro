namespace CoreBanking.Domain.Transaction;

public enum TransactionType
{
    Deposit = 1,
    WithDrawal = 2,
    TransferOut = 3, // Transfer Send
    TransferIn = 4   // Transfer received
}

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public Guid? ToAccountId { get; private set; }

    private Transaction() {}

    public static Transaction Create(
        Guid accountId,
        TransactionType type,
        decimal amount,
        string? description = null,
        Guid? toAccountId = null)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Type = type,
            Amount = amount,
            Description = description ?? type switch
            {
                TransactionType.Deposit     => "Depósito",
                TransactionType.WithDrawal  => "Saque",
                TransactionType.TransferOut => "Transferência Enviada",
                TransactionType.TransferIn  => "Transferência Recebida",
                _                           => "Transação"
            },
            CreatedAt = DateTime.UtcNow,
            ToAccountId = toAccountId
        };
    }
}