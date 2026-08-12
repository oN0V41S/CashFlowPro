using CoreBanking.Data;
using CoreBanking.Domain.Accounts;
using CoreBanking.Domain.Transaction;
using CoreBanking.Domain.Transaction.Events;
using CoreBanking.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CoreBanking.Tests.Services;

public class TransferServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task TransferAsync_ShouldTransferAmountAndPublishEvent_WhenValid()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var mockPublisher = new Mock<IEventPublisher>();

        // Create source and destination accounts
        var accountFrom = Account.Open("Origem", AccountType.Checking);
        accountFrom.Credit(500); // Saldo inicial R$ 500

        var accountTo = Account.Open("Destino", AccountType.Checking);
        
        db.Accounts.AddRange(accountFrom, accountTo);
        await db.SaveChangesAsync();

        var service = new TransferService(db, mockPublisher.Object);

        // Act
        await service.TransferAsync(accountFrom.Id, accountTo.Id, 200, "Pix de teste");

        // Assert
        // 1. Validates that balances were updated correctly in the bank
        var updatedFrom = await db.Accounts.FindAsync(accountFrom.Id);
        var updatedTo = await db.Accounts.FindAsync(accountTo.Id);

        Assert.Equal(300, updatedFrom!.Balance); // 500 - 200
        Assert.Equal(200, updatedTo!.Balance);   // 0 + 200

        // 2. Validates whether the transaction has been written to the transaction table
        var transactionCount = await db.Transactions.CountAsync();
        Assert.Equal(1, transactionCount);

        // 3. Validates that the domain event has been published to RabbitMQ
        mockPublisher.Verify(p => p.PublishAsync(
            It.Is<TransferCompleted>(e => e.Amount == 200 && e.FromAccountId == accountFrom.Id && e.ToAccountId == accountTo.Id),
            It.Is<string>(rk => rk == "transfer.completed")
        ), Times.Once);
    }

    [Fact]
    public async Task TransferAsync_ShouldThrowException_WhenAccountNotFound()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var mockPublisher = new Mock<IEventPublisher>();

        var accountFrom = Account.Open("Rafael", AccountType.Checking);
        db.Accounts.Add(accountFrom);
        await db.SaveChangesAsync();

        var service = new TransferService(db, mockPublisher.Object);
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.TransferAsync(accountFrom.Id, nonExistentId, 100)
        );

        Assert.Equal("Conta destino não encontrada", exception.Message);
    }

    [Fact]
    public async Task TransferAsync_ShouldThrowException_WhenInsufficientBalance()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var mockPublisher = new Mock<IEventPublisher>();

        var accountFrom = Account.Open("Rafael", AccountType.Checking); // Balance 0
        var accountTo = Account.Open("Destino", AccountType.Checking);

        db.Accounts.AddRange(accountFrom, accountTo);
        await db.SaveChangesAsync();

        var service = new TransferService(db, mockPublisher.Object);

        // Act and Assert (Tries to transfer 600 exceeding the overdraft of 500)
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.TransferAsync(accountFrom.Id, accountTo.Id, 600)
        );

        Assert.Contains("saldo insuficiente", exception.Message);
    }
}