using CoreBanking.Domain.Common;

namespace CoreBanking.Services;

// Define the default rule for Event publish on Domain - SOLID
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string routingKey) where T: IDomainEvent;
}