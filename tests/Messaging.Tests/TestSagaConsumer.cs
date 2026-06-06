using Contracts.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Messaging.Tests
{
    public class TestSagaConsumer : RabbitMqSubscriberService
    {
        public TestSagaConsumer(
           IOptions<RabbitMqOptions> options,
           ILogger<TestSagaConsumer> logger) : base(options, logger)
        {
            RegisterHandler(MessagingConstants.OrderCreatedEventsRoutingKey, HandleMessageAsync);
        }

        public bool HandleCalled { get; private set; }

        protected override string QueueName => MessagingConstants.OrderOrderEventsQueueName;

        protected override List<string> RoutingKeys => new List<string>() { MessagingConstants.OrderCreatedEventsRoutingKey };

        protected Task HandleMessageAsync(string message, CancellationToken cancellationToken)
        {
            var order = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
            if (order is not null)
            {
                HandleCalled = true;
            }
            return Task.CompletedTask;
        }
    }
}
