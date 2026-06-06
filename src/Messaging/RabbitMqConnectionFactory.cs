using RabbitMQ.Client;

namespace Messaging
{
    public sealed class RabbitMqConnectionFactory(
        ConnectionFactory factory)
        : IRabbitMqConnectionFactory
    {
        public Task<IConnection> CreateConnectionAsync(CancellationToken token)
        {
            return factory.CreateConnectionAsync(token);
        }
    }
}
