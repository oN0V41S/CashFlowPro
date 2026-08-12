using System.Text;
using System.Text.Json;
using CoreBanking.Domain.Common;
using RabbitMQ.Client;

namespace CoreBanking.Services;

public class RabbitMQEventPublisher(IChannel channel) : IEventPublisher
{
    private const string ExchangeName = "cashflow-exchange";

    public async Task PublishAsync<T>(T @event, string routingKey) where T : IDomainEvent
    {
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken: CancellationToken.None);

        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true
        };

        await channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body);
    }
}
