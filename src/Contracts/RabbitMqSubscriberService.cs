using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Contracts;

public abstract class RabbitMqSubscriberService<TMessage>(
    IOptions<RabbitMqOptions> options,
    ILogger logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }
    protected abstract Task HandleMessageAsync(TMessage message, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        (_connection, _channel) = await RabbitMqRetryHelper.ExecuteWithRetryAsync(
            async token =>
            {
                var connection = await options.Value.CreateFactory().CreateConnectionAsync(token);
                var channel = await connection.CreateChannelAsync(cancellationToken: token);
                return (connection, channel);
            },
            logger,
            $"{GetType().Name} RabbitMQ connection",
            stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: MessagingConstants.EventsExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: MessagingConstants.EventsExchangeName,
            routingKey: RoutingKey,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<TMessage>(Encoding.UTF8.GetString(args.Body.Span));
                if (message is null)
                    throw new InvalidOperationException($"Invalid message body for {typeof(TMessage).Name}");

                await HandleMessageAsync(message, stoppingToken);
                await _channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process message from {QueueName}", QueueName);
                await _channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Subscribed to {QueueName}", QueueName);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);
        _connection?.Dispose();
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

