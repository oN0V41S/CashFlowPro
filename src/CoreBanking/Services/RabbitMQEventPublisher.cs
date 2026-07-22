using System.Text;
using System.Text.Json;
using CoreBanking.Domain.Common;
using RabbitMQ.Client;

namespace CoreBanking.Services;

public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly IModel _channel;
    private const string ExchangeName = "cashflow-exchange";

    public RabbitMQEventPublisher(IModel channel)
    {
        _channel = channel;

        // Ensures that the Topic type Exchange (key routing) exists in the broker
        _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true) ;
    }

    public Task PublishAsync<T>(T @event, string routingKey) where T : IDomainEvent
    {
        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true; // Ensures message survives RabbitMQ reboots

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body
        );

        return Task.CompletedTask;
    } 
}