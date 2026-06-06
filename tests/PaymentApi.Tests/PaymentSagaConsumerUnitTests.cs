using Contracts.Events;
using Contracts.Models;
using Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace PaymentApi.Tests
{
    public class PaymentSagaConsumerUnitTests
    {
        [Fact]
        public async Task HandleOrderCreatedAsync_Should_Publish_PaymentCompleteEvent()
        {
            var channelMock = new Mock<IRabbitMqPublisher>();

            channelMock.Setup(x =>
                x.PublishAsync(
                    It.IsAny<PaymentCompleteEvent>(),
                    MessagingConstants.PaymentCompletedRoutingKey,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var options = Options.Create(new RabbitMqOptions());
            var logger = Mock.Of<ILogger<PaymentSagaConsumer>>();
            var paymentService = new Mock<IPaymentService>();

            paymentService
                .Setup(x =>
                    x.PayAsync(It.IsAny<Payment>()))
                .ReturnsAsync(true);

            var consumer = new PaymentSagaConsumer(options, logger, channelMock.Object, paymentService.Object);

            var message = new OrderCreatedEvent(
               Guid.NewGuid(),
               "test@gmail.com",
               [
                    new OrderItem("SKU-1",2)
               ],
               DateTime.UtcNow,
               new Payment("cardToken", 100));

            var json = JsonSerializer.Serialize(message);

            await consumer.HandleOrderCreatedAsync(json, CancellationToken.None);

            channelMock.Verify(x =>
                x.PublishAsync(
                    It.IsAny<PaymentCompleteEvent>(),
                    MessagingConstants.PaymentCompletedRoutingKey,
                    It.IsAny<CancellationToken>()),
                   Times.Once);
        }

        [Fact]
        public async Task HandleOrderCreatedAsync_Should_Publish_PaymentFailed_When_Payment_Fails()
        {
            var channelMock = new Mock<IRabbitMqPublisher>();

            channelMock.Setup(x =>
                x.PublishAsync(
                    It.IsAny<PaymentFailedEvent>(),
                    MessagingConstants.PaymentFailedRoutingKey,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var options = Options.Create(new RabbitMqOptions());
            var logger = Mock.Of<ILogger<PaymentSagaConsumer>>();
            var paymentService = new Mock<IPaymentService>();

            paymentService
                .Setup(x =>
                    x.PayAsync(It.IsAny<Payment>()))
                .ReturnsAsync(false);

            var consumer = new PaymentSagaConsumer(options, logger, channelMock.Object, paymentService.Object);

            var message = new OrderCreatedEvent(
               Guid.NewGuid(),
               "test@gmail.com",
               [
                    new OrderItem("SKU-1",2)
               ],
               DateTime.UtcNow,
               new Payment("cardToken", 100));

            var json = JsonSerializer.Serialize(message);

            await consumer.HandleOrderCreatedAsync(json, CancellationToken.None);

            channelMock.Verify(x =>
                x.PublishAsync(
                    It.IsAny<PaymentFailedEvent>(),
                    MessagingConstants.PaymentFailedRoutingKey,
                    It.IsAny<CancellationToken>()),
                   Times.Once);

            channelMock.Verify(x =>
                x.PublishAsync(
                    It.IsAny<PaymentCompleteEvent>(),
                    MessagingConstants.PaymentCompletedRoutingKey,
                    It.IsAny<CancellationToken>()),
                   Times.Never);
        }

        [Fact]
        public async Task HandleOrderCreatedAsync_Should_Not_Publish_When_Exception_Occurs()
        {
            var channelMock = new Mock<IRabbitMqPublisher>();

            channelMock
                .Setup(x =>
                    x.PublishAsync(
                        It.IsAny<PaymentCompleteEvent>(),
                        MessagingConstants.PaymentCompletedRoutingKey,
                        It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("RabbitMQ failed"));

            channelMock
                .Setup(x =>
                    x.PublishAsync(
                        It.IsAny<PaymentFailedEvent>(),
                        MessagingConstants.PaymentFailedRoutingKey,
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var options = Options.Create(new RabbitMqOptions());
            var logger = Mock.Of<ILogger<PaymentSagaConsumer>>();
            var paymentService = new Mock<IPaymentService>();
            paymentService
                .Setup(x =>
                    x.PayAsync(It.IsAny<Payment>()))
                .ReturnsAsync(true);

            var consumer = new PaymentSagaConsumer(options, logger, channelMock.Object, paymentService.Object);

            var message = new OrderCreatedEvent(
               Guid.NewGuid(),
               "test@gmail.com",
               [
                    new OrderItem("SKU-1",2)
               ],
               DateTime.UtcNow,
               new Payment("cardToken", 100));

            var json = JsonSerializer.Serialize(message);

            await consumer.HandleOrderCreatedAsync(json, CancellationToken.None);

            channelMock.Verify(x =>
                x.PublishAsync(
                    It.IsAny<PaymentFailedEvent>(),
                    MessagingConstants.PaymentFailedRoutingKey,
                    It.IsAny<CancellationToken>()),
                   Times.Once);
        }

        [Fact]
        public async Task HandleStockFailedAsync_Should_Publish_PaymentRefundedEvent()
        {
            var channelMock = new Mock<IRabbitMqPublisher>();

            channelMock.Setup(x =>
                x.PublishAsync(
                    It.IsAny<PaymentRefundedEvent>(),
                    MessagingConstants.PaymentRefundedRoutingKey,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var options = Options.Create(new RabbitMqOptions());
            var logger = Mock.Of<ILogger<PaymentSagaConsumer>>();
            var paymentService = new Mock<IPaymentService>();

            paymentService
                .Setup(x =>
                    x.PayAsync(It.IsAny<Payment>()))
                .ReturnsAsync(true);

            var consumer = new PaymentSagaConsumer(options, logger, channelMock.Object, paymentService.Object);

            var message = new StockFailedEvent(
               Guid.NewGuid(),
               "Insufficient stock");

            var json = JsonSerializer.Serialize(message);

            await consumer.HandleStockFailedAsync(json, CancellationToken.None);

            channelMock.Verify(x =>
                x.PublishAsync(
                    It.IsAny<PaymentRefundedEvent>(),
                    MessagingConstants.PaymentRefundedRoutingKey,
                    It.IsAny<CancellationToken>()),
                   Times.Once);
        }
    }
}
