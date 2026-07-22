using CoreBanking.Domain.Transaction;
using Xunit;

namespace CoreBanking.Tests.Domain.Transactions;

public class TransactionTests
{
    [Fact]
    public void Create_ShouldGenerateId()
    {
        var accountId = Guid.NewGuid();

        var transaction = Transaction.Create(accountId, TransactionType.Deposit, 100);

        Assert.NotEqual(Guid.Empty, transaction.Id);
    }

    [Fact]
    public void Create_ShouldAssignDefaultDescription()
    {
        var accountId = Guid.NewGuid();

        var deposit = Transaction.Create(accountId, TransactionType.Deposit, 100);
        var withdrawal = Transaction.Create(accountId, TransactionType.WithDrawal, 50);
        var transferOut = Transaction.Create(accountId, TransactionType.TransferOut, 30);
        var transferIn = Transaction.Create(accountId, TransactionType.TransferIn, 200);

        Assert.Equal("Depósito", deposit.Description);
        Assert.Equal("Saque", withdrawal.Description);
        Assert.Equal("Transferência Enviada", transferOut.Description);
        Assert.Equal("Transferência Recebida", transferIn.Description);
    }

    [Fact]
    public void Create_ShouldAssignCustomDescription()
    {
        var accountId = Guid.NewGuid();

        var transaction = Transaction.Create(accountId, TransactionType.Deposit, 100, "Pix recebido");

        Assert.Equal("Pix recebido", transaction.Description);
    }
}
