using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoreBanking.Controllers;
using CoreBanking.Data;
using CoreBanking.Domain.Accounts;
using CoreBanking.Domain.User;
using CoreBanking.Models.Transaction;
using CoreBanking.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace CoreBanking.Tests.Controllers;

public class TransfersControllerTests
{
    private const string TestSecretKey = "chave-secreta-para-testes-unitarios-1234567890";
    private const string TestIssuer = "CashFlowPro";
    private const string TestAudience = "CashFlowPro";

    private static string GenerateTokenForAccount(Guid accountId)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("accountId", accountId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static IConfiguration CreateConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key", TestSecretKey },
            { "Jwt:Issuer", TestIssuer },
            { "Jwt:Audience", TestAudience }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private (AppDbContext db, TransfersController controller) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var mockPublisher = new Mock<IEventPublisher>();
        var service = new TransferService(db, mockPublisher.Object);
        var configuration = CreateConfiguration();
        var controller = new TransfersController(service, configuration);
        return (db, controller);
    }

    private static void SetControllerUser(TransfersController controller, Guid accountId)
    {
        var claims = new List<Claim>
        {
            new("accountId", accountId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task Transfer_ShouldReturnOkWhenSuccessful()
    {
        var (db, controller) = CreateSut();

        var fromAccount = Account.Open("Origem", AccountType.Checking);
        fromAccount.Credit(500);
        var toAccount = Account.Open("Destino", AccountType.Checking);
        db.Accounts.AddRange(fromAccount, toAccount);
        await db.SaveChangesAsync();

        SetControllerUser(controller, fromAccount.Id);

        var request = new TransferRequest
        {
            ToAccountToken = GenerateTokenForAccount(toAccount.Id),
            Amount = 200,
            Description = "Pix"
        };

        var result = await controller.Transfer(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task Transfer_ShouldReturnNotFoundWhenAccountNotFound()
    {
        var (db, controller) = CreateSut();

        var fromAccount = Account.Open("Origem", AccountType.Checking);
        db.Accounts.Add(fromAccount);
        await db.SaveChangesAsync();

        SetControllerUser(controller, fromAccount.Id);

        var request = new TransferRequest
        {
            ToAccountToken = GenerateTokenForAccount(Guid.NewGuid()),
            Amount = 100,
            Description = null
        };

        var result = await controller.Transfer(request);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task Transfer_ShouldReturnBadRequestWhenInsufficientBalance()
    {
        var (db, controller) = CreateSut();

        var fromAccount = Account.Open("Origem", AccountType.Checking);
        var toAccount = Account.Open("Destino", AccountType.Checking);
        db.Accounts.AddRange(fromAccount, toAccount);
        await db.SaveChangesAsync();

        SetControllerUser(controller, fromAccount.Id);

        var request = new TransferRequest
        {
            ToAccountToken = GenerateTokenForAccount(toAccount.Id),
            Amount = 600,
            Description = null
        };

        var result = await controller.Transfer(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Transfer_ShouldReturnBadRequestWhenSameAccount()
    {
        var (db, controller) = CreateSut();

        var account = Account.Open("Conta Unica", AccountType.Checking);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        SetControllerUser(controller, account.Id);

        var request = new TransferRequest
        {
            ToAccountToken = GenerateTokenForAccount(account.Id),
            Amount = 100,
            Description = "Teste mesma conta"
        };

        var result = await controller.Transfer(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Transfer_ShouldReturnBadRequestWhenInvalidToken()
    {
        var (db, controller) = CreateSut();

        var account = Account.Open("Origem", AccountType.Checking);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        SetControllerUser(controller, account.Id);

        var request = new TransferRequest
        {
            ToAccountToken = "token-invalido",
            Amount = 100,
            Description = null
        };

        var result = await controller.Transfer(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}
