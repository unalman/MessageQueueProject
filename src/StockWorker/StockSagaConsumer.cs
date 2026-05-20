using Contracts.Events;
using Messaging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace StockWorker;

public sealed class StockSagaConsumer : RabbitMqSubscriberService
{
    private readonly InMemoryStockStore _stockStore;
    private readonly ILogger<StockSagaConsumer> _logger;
    private readonly RabbitMqConnectionProvider _rabbitMq;
    public StockSagaConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger<StockSagaConsumer> logger,
        InMemoryStockStore stockStore,
        RabbitMqConnectionProvider rabbitMq) : base(options, logger)
    {
        _stockStore = stockStore;
        _rabbitMq = rabbitMq;
        _logger = logger;

        RegisterHandler(MessagingConstants.PaymentCompletedRoutingKey, HandlePaymentCompletedAsync);
    }

    protected override string QueueName => MessagingConstants.StockOrderEventsQueueName;
    protected override List<string> RoutingKeys => new List<string>()
    {
        MessagingConstants.PaymentCompletedRoutingKey
    };

    public async Task HandlePaymentCompletedAsync(string body, CancellationToken token)
    {
        var message = JsonSerializer.Deserialize<PaymentCompleteEvent>(body);
        if (message is null)
            return;

        try
        {
            foreach (var item in message.Items)
            {
                if (_stockStore.TryDecrease(item.Sku, item.Quantity, out var remaining))
                {
                    _logger.LogInformation(
                        "Stock decreased for SKU {Sku} by {Quantity}. Remaining: {Remaining} (OrderId: {OrderId})",
                        item.Sku,
                        item.Quantity,
                        remaining,
                        message.OrderId);
                }
                else
                {
                    _logger.LogWarning(
                        "Insufficient stock for SKU {Sku} (requested {Quantity}). (OrderId: {OrderId})",
                        item.Sku,
                        item.Quantity,
                        message.OrderId);

                    await _rabbitMq.PublishAsync(
                        new StockFailedEvent(message.OrderId, $"Insufficient stock for SKU {item.Sku}")
                        {
                            SagaId = message.SagaId
                        },
                        MessagingConstants.StockFailedOrderEventsRoutingKey,
                        token);

                    return;
                }
            }

            await _rabbitMq.PublishAsync(
                        new StockReservedEvent(message.OrderId, message.UserEmail),
                        MessagingConstants.StockReservedOrderEventsRoutingKey,
                        token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stock failed for order {OrderId}", message.OrderId);
            await _rabbitMq.PublishAsync(
                new StockFailedEvent(message.OrderId, $"Stock failed for order {message.OrderId}")
                {
                    SagaId = message.SagaId
                },
                MessagingConstants.StockFailedOrderEventsRoutingKey,
                token);
        }
    }
}
