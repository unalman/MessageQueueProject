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
            var message = JsonSerializer.Deserialize<TMessage>(
                Encoding.UTF8.GetString(args.Body.Span));

            if (message is null)
                throw new InvalidOperationException();

            await HandleMessageAsync(message, cancellationToken);

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
}

