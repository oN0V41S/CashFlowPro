namespace CoreBanking.Domain.Transaction;

// readonly → imutablee
// record → compare by value (100 == 100)
// struct → ligth type, allocated on the stack
public readonly record struct Money (decimal Amount, string Currency)
{
    // Methods that return new instances (never modify older)
    public Money Add(Money other)
    {
        if(Currency != other.Currency)
            throw new InvalidOperationException("Moedas insuficientes");
        return new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"R$ {Amount:F2}";
}