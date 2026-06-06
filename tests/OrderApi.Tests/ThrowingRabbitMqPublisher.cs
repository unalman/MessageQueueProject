using Contracts;
using Messaging;

namespace OrderApi.Tests
{
    public sealed class ThrowingRabbitMqPublisher : IRabbitMqPublisher
    {
        public Task PublishAsync<T>(T message, string routingKey, CancellationToken token) where T : IntegrationEvent
        {
            throw new Exception("RabbitMQ unavailable");
        }
    }
}
