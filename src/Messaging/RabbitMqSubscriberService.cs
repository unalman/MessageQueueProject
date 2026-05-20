using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Messaging;

public abstract class RabbitMqSubscriberService(
    IOptions<RabbitMqOptions> options,
    ILogger logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly Dictionary<string, Func<string, CancellationToken, Task>> _handlers
    = new();

    protected abstract string QueueName { get; }
    protected abstract List<string> RoutingKeys { get; }

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

        foreach (var key in RoutingKeys)
        {
            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: MessagingConstants.EventsExchangeName,
                routingKey: key,
                arguments: null,
                cancellationToken: stoppingToken);
        }

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, args)
         => await ProcessMessageAsync(args, stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Subscribed to {QueueName}", QueueName);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    protected virtual async Task ProcessMessageAsync(
        BasicDeliverEventArgs args,
        CancellationToken cancellationToken)
    {
        try
        {
            var routingKey = args.RoutingKey;

            var body = Encoding.UTF8.GetString(args.Body.Span);

            if (!_handlers.TryGetValue(routingKey, out var handler))
                throw new InvalidOperationException($"Handler not found for {routingKey}");

            await handler(body, cancellationToken);

            await _channel!.BasicAckAsync(
                args.DeliveryTag,
                false,
                cancellationToken);
        }
        catch
        {
            await _channel!.BasicNackAsync(
                args.DeliveryTag,
                false,
                true,
                cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
            await _channel.DisposeAsync();

        _connection?.Dispose();
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }

    protected void RegisterHandler(
    string routingKey,
    Func<string, CancellationToken, Task> handler)
    {
        _handlers[routingKey] = handler;
    }
}

