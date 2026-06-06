using Contracts.Events;
using Contracts.Models;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;

namespace Messaging.Tests
{
    public class RabbitMqConnectionProviderTests
    {
        [Fact]
        public async Task PublishAsync_Should_Publish_Message()
        {
            var channelMock = new Mock<IChannel>();

            channelMock
                .Setup(
                    x => x.BasicPublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<BasicProperties>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            channelMock
                .Setup(x => x.DisposeAsync())
                .Returns(ValueTask.CompletedTask);

            var connectionMock = new Mock<IConnection>();

            connectionMock
                .Setup(
                    x => x.CreateChannelAsync(
                    It.IsAny<CreateChannelOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(channelMock.Object);

            connectionMock
                .SetupGet(x => x.IsOpen)
                .Returns(true);

            var factoryMock = new Mock<IRabbitMqConnectionFactory>();

            factoryMock
                .Setup(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(connectionMock.Object);

            var options = Options.Create(new RabbitMqOptions
            {
                Host = "localhost"
            });

            var logger =
                new Mock<Microsoft.Extensions.Logging.ILogger<RabbitMqConnectionProvider>>()
                .Object;

            var provider = new RabbitMqConnectionProvider(factoryMock.Object, options, logger);

            var message = new OrderCreatedEvent(
                Guid.NewGuid(),
                "test@test.com",
                [
                    new OrderItem("ABC", 2)
                ],
                DateTime.UtcNow,
                new Payment("card123", 100)
            );

            //Act

            await provider.PublishAsync(message, MessagingConstants.OrderCreatedEventsRoutingKey, CancellationToken.None);

            //Assert

            channelMock.Verify(x =>
                x.BasicPublishAsync(
                    MessagingConstants.EventsExchangeName,
                    MessagingConstants.OrderCreatedEventsRoutingKey,
                    false,
                    It.IsAny<BasicProperties>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<CancellationToken>()),
                 Times.Once);

        }
    }
}
