using System.Text;
using System.Text.Json;
using Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace StockWorker;

public sealed class OrderPaidStockSubscriber(
    IOptions<RabbitMqOptions> options,
    OrderPaidStockHandler handler,
    ILogger<OrderPaidStockSubscriber> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cfg = options.Value;

        var attempt = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = cfg.Host,
                    VirtualHost = cfg.VirtualHost,
                    UserName = cfg.User,
                    Password = cfg.Pass,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                };

                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                var delaySeconds = Math.Min(60, (int)Math.Pow(2, Math.Min(6, attempt)));
                logger.LogWarning(ex, "RabbitMQ connection attempt {Attempt} failed. Retrying in {DelaySeconds}s...", attempt, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
        }

        if (_connection is null || _channel is null)
            return;

        await _channel.ExchangeDeclareAsync(
            exchange: MessagingConstants.EventsExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: MessagingConstants.StockOrderPaidQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: MessagingConstants.StockOrderPaidQueueName,
            exchange: MessagingConstants.EventsExchangeName,
            routingKey: MessagingConstants.OrderPaidRoutingKey,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, args) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(args.Body.Span);
                var message = JsonSerializer.Deserialize<OrderPaid>(json);
                if (message is null)
                    throw new InvalidOperationException("Invalid message body (could not deserialize OrderPaid)");

                await handler.HandleAsync(message, stoppingToken);
                await _channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to handle OrderPaid message");
                await _channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: MessagingConstants.StockOrderPaidQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Subscribed to {Queue}", MessagingConstants.StockOrderPaidQueueName);

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
