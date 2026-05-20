using Contracts;
using Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Contract.Tests
{
    public class RabbitMqSubscriberTests
    {
        [Fact]
        public async Task ProcessMessageAsync_Should_Ack_Message_When_Message_Is_Valid()
        {
            // Arrange
            var channelMock = new Mock<IChannel>();

            channelMock
                .Setup(x => x.BasicAckAsync(
                    It.IsAny<ulong>(),
                    false,
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var service = CreateSubscriber();

            SetPrivateChannel(service, channelMock.Object);

            var orderPaid = new OrderPaid(
               Guid.NewGuid(),
               "test@test.com",
               [
                   new OrderItem("ABC-123", 2)
               ],
               DateTime.UtcNow
            );

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(orderPaid));

            var args = new BasicDeliverEventArgs(
                consumerTag: "test-consumer",
                deliveryTag: 1,
                redelivered: false,
                exchange: "events",
                routingKey: "order.paid",
                properties: null!,
                body: body
            );

            await service.InvokeProcessMessageAsync(args, CancellationToken.None);

            //assert

            channelMock.Verify(x =>
                x.BasicAckAsync(1, false, It.IsAny<CancellationToken>()),
                Times.Once);

            service.HandleCalled.Should().BeTrue();
        }

        [Fact]
        public async Task ProcessMessageAsync_Should_Nack_Message_When_Handler_Throws_Exception()
        {
            var channelMock = new Mock<IChannel>();
            channelMock
                .Setup(x => x.BasicNackAsync(
                    It.IsAny<ulong>(),
                    false,
                    true,
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var service = CreateFailingSubscriber();

            SetPrivateChannel(service, channelMock.Object);

            var orderPaid = new OrderPaid(
                Guid.NewGuid(),
                "test@test.com",
                [
                    new OrderItem("ABC", 2)
                ],
                DateTime.UtcNow
            );

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(orderPaid));

            var args = new BasicDeliverEventArgs(
                consumerTag: "test-consumer",
                deliveryTag: 1,
                redelivered: false,
                exchange: "events",
                routingKey: "order.paid",
                properties: null!,
                body: body
            );

            await service.InvokeProcessMessageAsync(args, CancellationToken.None);

            channelMock.Verify(x => x.BasicNackAsync(1, false, true, It.IsAny<CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task ProcessMessageAsync_Should_Nack_Message_When_Message_Is_Invalid()
        {
            var channelMock = new Mock<IChannel>();

            channelMock
                .Setup(x => x.BasicNackAsync(It.IsAny<ulong>(), false, true, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var service = CreateSubscriber();

            SetPrivateChannel(service, channelMock.Object);

            var invalidJosn = "invalid-json";

            var body = Encoding.UTF8.GetBytes(invalidJosn);

            var args = new BasicDeliverEventArgs(
                consumerTag: "test-consumer",
                deliveryTag: 3,
                redelivered: false,
                exchange: "events",
                routingKey: "order.paid",
                properties: null!,
                body: body
            );

            await service.InvokeProcessMessageAsync(args, CancellationToken.None);

            channelMock.Verify(x => x.BasicNackAsync(3, false, true, It.IsAny<CancellationToken>()), Times.Once);

            channelMock.Verify(x => x.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task StopAsync_Shoud_Close_Channel()
        {
            var channelMock = new Mock<IChannel>();

            channelMock
                .Setup(x => x.DisposeAsync())
                .Returns(ValueTask.CompletedTask);

            var service = CreateSubscriber();

            SetPrivateChannel(service, channelMock.Object);

            await service.StopAsync(CancellationToken.None);

            channelMock.Verify(x => x.DisposeAsync(), Times.Once);
        }

        private static void SetPrivateChannel(RabbitMqSubscriberService service,
            IChannel channel)
        {
            var field = typeof(RabbitMqSubscriberService)
                .GetField("_channel",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            field!.SetValue(service, channel);
        }

        private static TestableSubscriberService CreateSubscriber()
        {
            var options = Options.Create(new RabbitMqOptions());

            var logger = Mock.Of<ILogger<TestableSubscriberService>>();

            return new TestableSubscriberService(options, logger);
        }

        private static FailingSubscriber CreateFailingSubscriber()
        {
            var options = Options.Create(new RabbitMqOptions());

            var logger = Mock.Of<ILogger<FailingSubscriber>>();

            return new FailingSubscriber(options, logger);
        }

    }
}
