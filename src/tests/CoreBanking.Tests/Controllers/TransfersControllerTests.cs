using CoreBanking.Controllers;
using CoreBanking.Data;
using CoreBanking.Domain.Accounts;
using CoreBanking.Models.Transaction;
using CoreBanking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CoreBanking.Tests.Controllers;

public class TransfersControllerTests
{
    private (AppDbContext db, TransfersController controller) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var mockPublisher = new Mock<IEventPublisher>();
        var service = new TransferService(db, mockPublisher.Object);
        var controller = new TransfersController(service);
        return (db, controller);
    }

    [Fact]
    public async Task Transfer_ShouldReturnOkWhenSuccessful()
    {
        var (db, controller) = CreateSut();

        var from = Account.Open("Origem", AccountType.Checking);
        from.Credit(500);
        var to = Account.Open("Destino", AccountType.Checking);
        db.Accounts.AddRange(from, to);
        await db.SaveChangesAsync();

        var request = new TransferRequest(from.Id, to.Id, 200, "Pix");

        var result = await controller.Transfer(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task Transfer_ShouldReturnNotFoundWhenAccountNotFound()
    {
        var (db, controller) = CreateSut();

        var from = Account.Open("Origem", AccountType.Checking);
        db.Accounts.Add(from);
        await db.SaveChangesAsync();

        var request = new TransferRequest(from.Id, Guid.NewGuid(), 100, null);

        var result = await controller.Transfer(request);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task Transfer_ShouldReturnBadRequestWhenInsufficientBalance()
    {
        var (db, controller) = CreateSut();

        var from = Account.Open("Origem", AccountType.Checking); // Saldo 0
        var to = Account.Open("Destino", AccountType.Checking);
        db.Accounts.AddRange(from, to);
        await db.SaveChangesAsync();

        var request = new TransferRequest(from.Id, to.Id, 600, null);

        var result = await controller.Transfer(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}