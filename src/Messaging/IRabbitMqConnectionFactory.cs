using RabbitMQ.Client;

namespace Messaging
{
    public interface IRabbitMqConnectionFactory
    {
        Task<IConnection> CreateConnectionAsync(CancellationToken token);
    }
}
