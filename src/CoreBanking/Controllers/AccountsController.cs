using Microsoft.AspNetCore.Authorization;
using CoreBanking.Data;
using CoreBanking.Models.Accounts;
using CoreBanking.Domain.Accounts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController(AppDbContext _db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> OpenAccount([FromBody] OpenAccountRequest request)
    {
        var account = Account.Open(request.HolderName, request.Type);
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        
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
        var account = await _db.Accounts.FindAsync(id);
        if (account == null)
            return NotFound(new { error = "Conta não encontrada" });

        var transactions = await _db.Transactions
            .Where(t => t.AccountId == id || t.ToAccountId == id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(new
        {
            account.Id,
            account.HolderName,
            account.Balance,
            Transaction = transactions
        });
    }
}
