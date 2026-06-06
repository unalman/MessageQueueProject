using Contracts;
using Messaging;

namespace OrderApi.Tests
{
    public sealed class FakeRabbitMqPublisher : IRabbitMqPublisher
    {
        public object? PublishedMessage { get; private set; }
        public string? RoutingKey { get; private set; }

        public Task PublishAsync<T>(T message, string routingKey, CancellationToken token) where T : IntegrationEvent
        {
            PublishedMessage = message;
            RoutingKey = routingKey;

            return Task.CompletedTask;
        }

        public void Reset()
        {
            PublishedMessage = null;
            RoutingKey = null;
        }
    }
}
