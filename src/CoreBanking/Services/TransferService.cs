using CoreBanking.Data;
using CoreBanking.Domain.Transaction;
using CoreBanking.Domain.Transaction.Events;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Services;

public class TransferService(AppDbContext _db, IEventPublisher _eventPublisher)
{
    public async Task TransferAsync(
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string? description = null)
    {
        var from = await _db.Accounts.FindAsync(fromAccountId);
        var to = await _db.Accounts.FindAsync(toAccountId);

        if (from == null)
            throw new KeyNotFoundException("Conta Origem não encontrada");
        if (to == null)
            throw new KeyNotFoundException("Conta destino não encontrada");
        if (!from.IsActive || !to.IsActive)
            throw new InvalidOperationException("Conta inativa");

        from.Debit(amount);
        to.Credit(amount);

        var transaction = Transaction.Create(
            fromAccountId,
            TransactionType.TransferOut,
            amount,
            description,
            toAccountId
        );

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        var transferEvent = new TransferCompleted
        {
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId,
            Amount = amount
        };

        await _eventPublisher.PublishAsync(transferEvent, routingKey: "transfer.completed");
    }
}
