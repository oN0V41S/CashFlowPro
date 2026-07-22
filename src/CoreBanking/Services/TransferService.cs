using CoreBanking.Data;
using CoreBanking.Domain.Transaction;
using CoreBanking.Domain.Transaction.Events;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Services;

public class TransferService
{
    private readonly AppDbContext _db;
    private readonly IEventPublisher _eventPublisher;

    // DI: ASP.NET Core automatically injects AppDbContext
    public TransferService(AppDbContext db, IEventPublisher eventPublisher)
    {
        _db = db;
        _eventPublisher = eventPublisher;
    }

    public async Task TransferAsync(
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string? description = null)
    {
        // 1. Search accounts (FindAsync = SELECT ... WHERE ID = ...)
        var from = await _db.Accounts.FindAsync(fromAccountId);
        var to = await _db.Accounts.FindAsync(toAccountId);

        // 2. Validations
        if (from == null)
            throw new KeyNotFoundException("Conta Origem não encontrada");

        if (to == null)
            throw new KeyNotFoundException("Conta destino não encontrada");

        if (!from.IsActive || !to.IsActive)
            throw new InvalidOperationException("Conta inativa");

        // 3. Logic using DOMAIN METHODS (not direct setters!)
        from.Debit(amount);
        to.Credit(amount);

        // 4. Register transaction using Factory Method
        var transaction = Transaction.Create(
            fromAccountId,
            TransactionType.TransferOut,
            amount,
            description,
            toAccountId
        );

        _db.Transactions.Add(transaction);

        // 5. Save - this turns into ONE SQL transaction (ACID!)
        // If something fails, everything goes back (ROLLBACK)
        await _db.SaveChangesAsync();

        // 6. Publish domain event to RabbitMQ asynchronously
        var transferEvent = new TransferCompleted
        {
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId,
            Amount = amount
        };

        await _eventPublisher.PublishAsync(transferEvent, routingKey: "transfer.completed");
    }
}