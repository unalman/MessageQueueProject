using EmailWorker.IntegrationEvents;
using EmailWorker.IntegrationEvents.Events;
using Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace EmailWorker.Tests
{
    public class EmailSagaConsumerUnitTests
    {
        [Fact]
        public async Task HandleOrderCompletedAsync_Should_Sent_Email_And_Publish_EmailSentEvent()
        {
            var rabbitMqMock = new Mock<IRabbitMqPublisher>();
            var options = Options.Create(new RabbitMqOptions());
            var logger = Mock.Of<ILogger<EmailSagaConsumer>>();

            rabbitMqMock
                .Setup(x => x.PublishAsync(
                    It.IsAny<EmailSentEvent>(),
                    MessagingConstants.EmailSentOrderEventsRoutingKey,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var consumer = new EmailSagaConsumer(options, logger, rabbitMqMock.Object);

            var message = new EmailSentEvent();

            var json = JsonSerializer.Serialize(message);

            await consumer.HandleOrderCompletedAsync(json, CancellationToken.None);

            rabbitMqMock.Verify(
                x => x.PublishAsync(
                    It.IsAny<EmailSentEvent>(),
                    MessagingConstants.EmailSentOrderEventsRoutingKey,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleOrderCompletedAsync_Should_Rethrow_When_RetryCount_Is_3()
        {
            var rabbitMqMock = new Mock<IRabbitMqPublisher>();
            var options = Options.Create(new RabbitMqOptions());
            var logger = Mock.Of<ILogger<EmailSagaConsumer>>();

            rabbitMqMock.Setup(
                x =>
                x.PublishAsync(
                    It.IsAny<EmailSentEvent>(),
                    MessagingConstants.EmailSentOrderEventsRoutingKey,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("RabbitMQ Error"));

            var consumer = new EmailSagaConsumer(options, logger, rabbitMqMock.Object);

            var message = new EmailSentEvent()
            {
                RetryCount = 3
            };

            var json = JsonSerializer.Serialize(message);

            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.HandleOrderCompletedAsync(json, CancellationToken.None));
        }

        [Fact]
        public async Task HandleOrderCompletedAsync_Should_Publish_OrderCompletedEvent_When_RetryCount_Less_Than_3()
        {
            var rabbitMqMock = new Mock<IRabbitMqPublisher>();
            var options = Options.Create(new RabbitMqOptions());
            var logger = Mock.Of<ILogger<EmailSagaConsumer>>();

            rabbitMqMock.Setup(
              x =>
              x.PublishAsync(
                  It.IsAny<EmailSentEvent>(),
                  MessagingConstants.EmailSentOrderEventsRoutingKey,
                  It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException("RabbitMQ Error"));

            rabbitMqMock.Setup(
                x =>
                x.PublishAsync(
                    It.IsAny<OrderCompletedEvent>(),
                    MessagingConstants.OrderCompletedEventsRoutingKey,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var consumer = new EmailSagaConsumer(options, logger, rabbitMqMock.Object);

            var message = new EmailSentEvent()
            {
                RetryCount = 2
            };

            var json = JsonSerializer.Serialize(message);

            await consumer.HandleOrderCompletedAsync(json, CancellationToken.None);

            rabbitMqMock.Verify(
                x => x.PublishAsync(
                    It.IsAny<EmailSentEvent>(),
                    MessagingConstants.EmailSentOrderEventsRoutingKey,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            rabbitMqMock.Verify(
                x => x.PublishAsync(
                    It.Is<OrderCompletedEvent>(e => e.RetryCount == 3),
                    MessagingConstants.OrderCompletedEventsRoutingKey,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
