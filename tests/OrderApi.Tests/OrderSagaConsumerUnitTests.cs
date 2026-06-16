using Contracts.Events;
using Contracts.Models;
using FluentAssertions;
using Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace OrderApi.Tests
{
    public class OrderSagaConsumerUnitTests
    {
        [Fact]
        public async Task HandleStockReservedAsync_Should_Publish_OrderCompletedEvent()
        {
            //Arrange
            var publisher = new Mock<IRabbitMqPublisher>();
            var options = Options.Create(new RabbitMqOptions());
            var logger = Mock.Of<ILogger<OrderSagaConsumer>>();

            var consumer = new OrderSagaConsumer(options, logger, publisher.Object);

            var evt = new StockReservedEvent(
                Guid.NewGuid(),
                "test@test.com",
                [
                    new OrderItem("SKU-1",2)
                ])
                {
                    SagaId = Guid.NewGuid(),
                };

            var body = JsonSerializer.Serialize(evt);

            //Act
            await consumer.HandleStockReservedAsync(body, CancellationToken.None);

            //Assert
            publisher.Verify(x =>
                x.PublishAsync(
                    It.Is<OrderCompletedEvent>(e =>
                        e.SagaId == evt.SagaId &&
                        e.OrderId == evt.OrderId &&
                        e.UserEmail == evt.UserEmail
                        ),
                    MessagingConstants.OrderCompletedEventsRoutingKey,
                    It.IsAny<CancellationToken>()),
                  Times.Once);
        }

        [Fact]
        public async Task HandleStockReservedAsync_Should_Throw_When_Json_Is_Invalid()
        {
            //arrange
            var publisher = new Mock<IRabbitMqPublisher>();
            var options = Options.Create(new RabbitMqOptions());
            var logger = Mock.Of<ILogger<OrderSagaConsumer>>();

            var consumer = new OrderSagaConsumer(options, logger, publisher.Object);

            //act
            var action = async () =>
            {
                await consumer.HandleStockReservedAsync("invalid-json", CancellationToken.None);
            };

            //Assert
            await action.Should().ThrowAsync<JsonException>();

            publisher.Verify(x =>
                x.PublishAsync(
                    It.IsAny<OrderCreatedEvent>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                  Times.Never);

        }
    }
}
