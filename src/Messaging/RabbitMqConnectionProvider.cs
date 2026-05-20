using Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Messaging
{
    public sealed class RabbitMqConnectionProvider(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConnectionProvider> logger) : IAsyncDisposable
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private IConnection? _connection;

        public async Task PublishAsync<T>(
            T message,
            string routingKey,
            CancellationToken token) where T : IntegrationEvent
        {
            var connection = await GetOrCreateConnectionAsync(token);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: token);

            await channel.ExchangeDeclareAsync(
                exchange: MessagingConstants.EventsExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: token
                );

            var bodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var props = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                ContentType = "application/json"
            };

            await channel.BasicPublishAsync(
                exchange: MessagingConstants.EventsExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: bodyBytes,
                cancellationToken: token);

            logger.LogInformation("Published event {EventType} with SagaId {SagaId}", typeof(T).Name, message.SagaId);
        }

        private async Task<IConnection> GetOrCreateConnectionAsync(CancellationToken token)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            await _gate.WaitAsync(token);

            try
            {
                if (_connection is { IsOpen: true })
                    return _connection;

                _connection?.Dispose();

                _connection = await RabbitMqRetryHelper.ExecuteWithRetryAsync(
                    async token => await options.Value.CreateFactory().CreateConnectionAsync(token),
                    logger,
                    "RabbitMQ connect",
                    token,
                    maxDelaySeconds: 30);

                logger.LogInformation("RabbitMQ connected to {Host}", options.Value.Host);

                return _connection;
            }
            finally
            {
                _gate.Release();
            }
        }

        public ValueTask DisposeAsync()
        {
            _connection?.Dispose();
            _gate.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
