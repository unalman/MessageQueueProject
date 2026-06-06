using Contracts;

namespace Messaging
{
    public interface IRabbitMqPublisher
    {
        public Task PublishAsync<T>(
            T message,
            string routingKey,
            CancellationToken token) where T : IntegrationEvent;
    }
}
