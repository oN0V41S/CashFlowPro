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
        var mockChannel = new Mock<IChannel>();

        var publisher = new RabbitMQEventPublisher(mockChannel.Object);

        var transferEvent = new TransferCompleted
        {
            FromAccountId = Guid.NewGuid(),
            ToAccountId = Guid.NewGuid(),
            Amount = 150.00m
        };

        await publisher.PublishAsync(transferEvent, "transfer.completed");

        mockChannel.Verify(c => c.BasicPublishAsync(
                It.Is<string>(e => e == "cashflow-exchange"),
                It.Is<string>(rk => rk == "transfer.completed"),
                It.Is<bool>(m => m == true),
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
}
