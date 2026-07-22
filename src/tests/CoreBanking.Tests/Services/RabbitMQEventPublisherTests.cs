using System.Text;
using System.Text.Json;
using CoreBanking.Domain.Transaction.Events;
using CoreBanking.Services;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace CoreBanking.Tests.Services;

public class RabbitMQEventPublisherTests
{
  [Fact]
    public async Task PublishAsync_ShouldCorrectlySerializeEventToJson()
    {
        // Arrange
        var mockChannel = new Mock<IModel>();

        // 1. Mock CreateBasicProperties to return a false (non-null) properties object
        var mockProperties = new Mock<IBasicProperties>();
        mockChannel.Setup(c => c.CreateBasicProperties()).Returns(mockProperties.Object);

        byte[]? capturedBody = null;
        mockChannel
            .Setup(c => c.BasicPublish(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<bool>(), 
                It.IsAny<IBasicProperties>(), 
                It.IsAny<ReadOnlyMemory<byte>>()))
            .Callback<string, string, bool, IBasicProperties, ReadOnlyMemory<byte>>(
                (exchange, rk, mandatory, props, body) => 
                {
                    capturedBody = body.ToArray();
                });

        var publisher = new RabbitMQEventPublisher(mockChannel.Object);
        var transferEvent = new TransferCompleted
        {
            FromAccountId = Guid.NewGuid(),
            ToAccountId = Guid.NewGuid(),
            Amount = 250.00m
        };

        // Act
        await publisher.PublishAsync(transferEvent, "transfer.completed");

        // Assert
        Assert.NotNull(capturedBody);
        
        // Deserializes the captured JSON back to validate that the data matches perfectly
        var jsonString = Encoding.UTF8.GetString(capturedBody);
        var deserializedEvent = JsonSerializer.Deserialize<TransferCompleted>(jsonString);

        Assert.NotNull(deserializedEvent);
        Assert.Equal(transferEvent.FromAccountId, deserializedEvent.FromAccountId);
        Assert.Equal(transferEvent.ToAccountId, deserializedEvent.ToAccountId);
        Assert.Equal(250.00m, deserializedEvent.Amount);
    }
}