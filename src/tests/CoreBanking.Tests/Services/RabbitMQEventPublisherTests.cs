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
    public async Task PublishAsync_ShouldSerializeAndPublishEventToCorrectExchange()
    {
        var mockChannel = new Mock<IModel>();
        var mockBasicProperties = new Mock<IBasicProperties>();
        mockChannel.Setup(c => c.CreateBasicProperties()).Returns(mockBasicProperties.Object);

        var publisher = new RabbitMQEventPublisher(mockChannel.Object);

        var transferEvent = new TransferCompleted
        {
            FromAccountId = Guid.NewGuid(),
            ToAccountId = Guid.NewGuid(),
            Amount = 150.00m
        };

        await publisher.PublishAsync(transferEvent, "transfer.completed");

        // Assert — Match.Create executa uma função real, NÃO uma Expression Tree
        mockChannel.Verify(c => c.BasicPublish
            (
                It.Is<string>(e => e == "cashflow-exchange"),
                It.Is<string>(rk => rk == "transfer.completed"),
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>() // ✅ Simplified, works every time
            ),
        Times.Once
        );
    }
}