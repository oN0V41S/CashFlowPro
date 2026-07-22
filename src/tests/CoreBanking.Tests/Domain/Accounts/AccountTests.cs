using CoreBanking.Domain.Accounts;
using Xunit;

namespace CoreBanking.Tests.Domain.Accounts;

public class AccountTests
{
    [Fact]
    public void Open_ShouldCreateActiveAccount()
    {
        var account = Account.Open("Rafael", AccountType.Checking);

        Assert.True(account.IsActive);
        Assert.Equal(0, account.Balance);
        Assert.Equal("Rafael", account.HolderName);
        Assert.Equal(AccountType.Checking, account.Type);
        Assert.NotEqual(Guid.Empty, account.Id);
    }

    [Fact]
    public void Credit_ShouldIncreaseBalance()
    {
        var account = Account.Open("Rafael", AccountType.Checking);

        account.Credit(100);

        Assert.Equal(100, account.Balance);
    }

    [Fact]
    public void Credit_ShouldThrowWhenNegative()
    {
        var account = Account.Open("Rafael", AccountType.Checking);

        var exception = Assert.Throws<ArgumentException>(() => account.Credit(-50));
        Assert.Equal("Valor deve ser positivo", exception.Message);
    }

    [Fact]
    public void Debit_ShouldDecreaseBalance()
    {
        var account = Account.Open("Rafael", AccountType.Checking);
        account.Credit(200);

        account.Debit(50);

        Assert.Equal(150, account.Balance);
    }

    [Fact]
    public void Debit_ShouldThrowWhenExceedingOverdraft()
    {
        var account = Account.Open("Rafael", AccountType.Checking);
        account.Credit(100);

        var exception = Assert.Throws<InvalidOperationException>(() => account.Debit(601));
        Assert.Contains("cheque especial", exception.Message);
    }

    [Fact]
    public void Close_ShouldThrowWhenBalanceNotZero()
    {
        var account = Account.Open("Rafael", AccountType.Checking);
        account.Credit(100);

        var exception = Assert.Throws<InvalidOperationException>(() => account.Close());
        Assert.Contains("saldo pendente", exception.Message);
    }
}
