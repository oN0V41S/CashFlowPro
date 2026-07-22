namespace CoreBanking.Domain.Common;

// Interface that every domain event must implement
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}