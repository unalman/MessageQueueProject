using Contracts.Events;
using Contracts.Models;
using FluentAssertions;
using Messaging;
using OrderApi.Application.IntegrationEvents;

namespace OrderApi.Tests
{
    public class CreateOrderCommandHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Return_OrderId()
        {
            var publisher = new FakeRabbitMqPublisher();

            var handler = new CreateOrderCommandHandler(publisher);

            var command = CreateCommand();

            var response = await handler.Handle(command, CancellationToken.None);

            response.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task Handle_Should_Publish_OrderCreatedEvent()
        {
            var publisher = new FakeRabbitMqPublisher();

            var handler = new CreateOrderCommandHandler(publisher);

            var command = CreateCommand();

            await handler.Handle(command, CancellationToken.None);

            publisher.PublishedMessage.Should()
                .BeOfType<OrderCreatedEvent>();
        }

        [Fact]
        public async Task Handle_Should_Use_Correct_RoutingKey()
        {
            var publisher = new FakeRabbitMqPublisher();
            var handler = new CreateOrderCommandHandler(publisher);
            var command = CreateCommand();

            await handler.Handle(command, CancellationToken.None);

            publisher.RoutingKey.Should()
                .Be(MessagingConstants.OrderCreatedEventsRoutingKey);
        }

        [Fact]
        public async Task Handle_Should_Throw_Exception_When_RabbitMq_Is_Unavailable()
        {
            var publisher = new ThrowingRabbitMqPublisher();

            var handler = new CreateOrderCommandHandler(publisher);

            var command = CreateCommand();

            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("RabbitMQ unavailable");
        }

        private static CreateOrderCommand CreateCommand()
        {
            return new CreateOrderCommand(
                "test@test.com",
                new List<OrderItem>
                {
                    new("SKU-1", 2)
                },
                new Payment("tok_123", 100));
        }
    }
}
