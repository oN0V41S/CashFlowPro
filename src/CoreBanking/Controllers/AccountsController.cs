using CoreBanking.Data;
using CoreBanking.Models.Accounts;
using CoreBanking.Domain.Accounts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AccountsController (AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult>OpenAccount([FromBody] OpenAccountRequest request)
    {
        // 1. Create the aggregate using DDD's Factory Method
        var account = Account.Open(request.HolderName, request.Type);

        // 2. Add to DbContext and save
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        // 3. Returns 201 Created with generated ID
        return CreatedAtAction(nameof(GetStatement), new { id = account.Id }, new
        {
           account.Id,
           account.HolderName,
           account.Balance,
           account.Type,
           account.CreatedAt,
        });
    }

    [HttpGet("{id:guid}/statement")]
    public async Task<IActionResult> GetStatement(Guid id)
    {
        // 1. Search Account
        var account = await _db.Accounts.FindAsync(id);
        if(account == null)
            return NotFound(new {error = "Conta não encontrada"});

        // 2. Get all transactions associated (send or recieved)
        var transactions = await _db.Transactions
            .Where(t => t.AccountId == id || t.ToAccountId == id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        // 3. Retorna o extrato consolidado
        return Ok(new
        {
            account.Id,
            account.HolderName,
            account.Balance,
            Transaction = transactions           
        });


    }
}