using Contracts.Events;
using Contracts.Models;
using FluentAssertions;
using Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace StockWorker.Tests
{
    public class StockSagaConsumerUnitTests
    {
        [Fact]
        public async Task HandlePaymentCompletedAsync_Should_Decrease_Stock_And_Publish_StockReserved()
        {
            //Arrange

            var options = Options.Create(new RabbitMqOptions());

            var logger = Mock.Of<ILogger<StockSagaConsumer>>();

            var stockStore = new InMemoryStockStore();

            var rabbitMqMock = new Mock<IRabbitMqPublisher>();

            rabbitMqMock
                .Setup(x => x.PublishAsync(
                    It.IsAny<StockReservedEvent>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var consumer = new StockSagaConsumer(options, logger, stockStore, rabbitMqMock.Object);

            var message = new PaymentCompleteEvent(
                Guid.NewGuid(),
                "test@test.com",
                [
                    new OrderItem("SKU-1", 2)
                ],
                DateTime.UtcNow);

            var json = JsonSerializer.Serialize(message);

            //act

            await consumer.HandlePaymentCompletedAsync(json, CancellationToken.None);

            //Assert

            stockStore.GetStock("SKU-1").Should().Be(8);

            rabbitMqMock.Verify(x =>
                x.PublishAsync(It.IsAny<StockReservedEvent>(),
                MessagingConstants.StockReservedOrderEventsRoutingKey,
                It.IsAny<CancellationToken>()),
              Times.Once);

        }

        [Fact]
        public async Task HandlePaymentCompletedAsync_Should_Publish_StockFailed_When_Insufficient()
        {
            //Arrange
            var options = Options.Create(new RabbitMqOptions());
            var logger = Mock.Of<ILogger<StockSagaConsumer>>();
            var stockStore = new InMemoryStockStore();
            var rabbitMqMock = new Mock<IRabbitMqPublisher>();

            rabbitMqMock
                .Setup(x =>
                    x.PublishAsync(
                        It.IsAny<StockFailedEvent>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var consumer = new StockSagaConsumer(options, logger, stockStore, rabbitMqMock.Object);

            var message = new PaymentCompleteEvent(
                Guid.NewGuid(),
                "test@test.com",
                [
                    new OrderItem("SKU-1", 11)
                ],
                DateTime.UtcNow);

            var json = JsonSerializer.Serialize(message);


            //act
            await consumer.HandlePaymentCompletedAsync(json, CancellationToken.None);


            stockStore.GetStock("SKU-1").Should().Be(10);

            //Assert
            rabbitMqMock.Verify(x =>
                x.PublishAsync(
                    It.IsAny<StockFailedEvent>(),
                    MessagingConstants.StockFailedOrderEventsRoutingKey,
                    It.IsAny<CancellationToken>()),
                  Times.Once);

            rabbitMqMock.Verify(x =>
                x.PublishAsync(
                    It.IsAny<StockReservedEvent>(),
                    MessagingConstants.StockReservedOrderEventsRoutingKey,
                    It.IsAny<CancellationToken>()),
                  Times.Never);
        }

        [Fact]
        public async Task HandlePaymentCompletedAsync_Should_Publish_StockFailed_When_Json_Invalid()
        {
            //Arrange
            var options = Options.Create(new RabbitMqOptions());
            var logger = Mock.Of<ILogger<StockSagaConsumer>>();
            var stockStore = new InMemoryStockStore();
            var rabbitMqMock = new Mock<IRabbitMqPublisher>();

            var consumer = new StockSagaConsumer(options, logger, stockStore, rabbitMqMock.Object);

            var json = "invalid json";

            var action = async () =>
            {
                await consumer.HandlePaymentCompletedAsync(json, CancellationToken.None);
            };

            await action.Should().ThrowAsync<JsonException>();

            rabbitMqMock.Verify(x =>
                x.PublishAsync(
                    It.IsAny<StockFailedEvent>(),
                    MessagingConstants.StockFailedOrderEventsRoutingKey,
                    It.IsAny<CancellationToken>()),
                  Times.Never);

            rabbitMqMock.Verify(x =>
                x.PublishAsync(
                    It.IsAny<StockReservedEvent>(),
                    MessagingConstants.StockReservedOrderEventsRoutingKey,
                    It.IsAny<CancellationToken>()),
                   Times.Never);

        }
    }
}
