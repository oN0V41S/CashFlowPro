using CoreBanking.Controllers;
using CoreBanking.Data;
using CoreBanking.Models.Accounts;
using CoreBanking.Domain.Accounts;
using CoreBanking.Domain.Transaction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreBanking.Tests.Controllers;

public class AccountsControllerTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task OpenAccount_ShouldCreateAccountAndReturn201()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var controller = new AccountsController(db);
        var request = new OpenAccountRequest("Rafael", AccountType.Checking);

        // Act
        var result = await controller.OpenAccount(request);

        // Assert
        var createdResult =Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(1, await db.Accounts.CountAsync());
    }

    [Fact]
    public async Task GetStatement_ShouldReturnNotFoundWhenAccountDoesNotExist()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var controller = new AccountsController(db);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await controller.GetStatement(nonExistentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }
    
    [Fact]
    public async Task GetStatement_ShouldReturnAccountAndTransactionsWhenExists()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var controller = new AccountsController(db);

        // Create Test Account
        var account = Account.Open("Rafael", AccountType.Checking);
        account.Credit(300);
        db.Accounts.Add(account);

        // Craete an Transaction associated account
        var transaction = Transaction.Create(account.Id, TransactionType.Deposit, 300, "Depósito Inicial");
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        // Act
        var result = await controller.GetStatement(account.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        // Validate if objet return essential data of Account and Transactions
        Assert.NotNull(okResult.Value);
    }   
}