using Contracts.Events;
using Contracts.Models;
using Messaging;
using Microsoft.Extensions.Options;
using StockWorker.IntegrationEvents.Events;
using System.Text.Json;

namespace StockWorker;

public sealed class StockSagaConsumer : RabbitMqSubscriberService
{
    private readonly InMemoryStockStore _stockStore;
    private readonly ILogger<StockSagaConsumer> _logger;
    private readonly IRabbitMqPublisher _rabbitMq;
    public StockSagaConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger<StockSagaConsumer> logger,
        InMemoryStockStore stockStore,
        IRabbitMqPublisher rabbitMq) : base(options, logger)
    {
        _stockStore = stockStore;
        _rabbitMq = rabbitMq;
        _logger = logger;

        RegisterHandler(MessagingConstants.PaymentCompletedRoutingKey, HandlePaymentCompletedAsync);
        RegisterHandler(MessagingConstants.OrderCompletedEventsRoutingKey, HandleOrderCompletedAsync);
    }

    protected override string QueueName => MessagingConstants.StockOrderEventsQueueName;
    protected override List<string> RoutingKeys => new List<string>()
    {
        MessagingConstants.PaymentCompletedRoutingKey,
        MessagingConstants.OrderCompletedEventsRoutingKey
    };

    public async Task HandlePaymentCompletedAsync(string body, CancellationToken token)
    {
        var message = JsonSerializer.Deserialize<PaymentCompleteEvent>(body);
        if (message is null)
            return;

        try
        {
            var reservedItems = new List<OrderItem>();
            foreach (var item in message.Items)
            {
                if (_stockStore.TryReserve(item.Sku, item.Quantity))
                {
                    _logger.LogInformation(
                        "Stock decreased for SKU {Sku} by {Quantity}. (OrderId: {OrderId})",
                        item.Sku,
                        item.Quantity,
                        message.OrderId);
                    reservedItems.Add(item);
                }
                else
                {
                    _logger.LogWarning(
                        "Insufficient stock for SKU {Sku} (requested {Quantity}). (OrderId: {OrderId})",
                        item.Sku,
                        item.Quantity,
                        message.OrderId);

                    foreach (var reservedItem in reservedItems)
                        _stockStore.Release(reservedItem.Sku, reservedItem.Quantity);

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
                        new StockReservedEvent(message.OrderId, message.UserEmail, message.Items),
                        MessagingConstants.StockReservedOrderEventsRoutingKey,
                        token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stock failed for order {OrderId}", message.OrderId);
            try
            {
                await _rabbitMq.PublishAsync(
                    new StockFailedEvent(message.OrderId, $"Stock failed for order {message.OrderId}")
                    {
                        SagaId = message.SagaId
                    },
                    MessagingConstants.StockFailedOrderEventsRoutingKey,
                    token);
            }
            catch (Exception publishEx)
            {
                _logger.LogCritical(publishEx, "PublishAsync throw exceptions");
                throw;
            }
        }
    }

    public async Task HandleOrderCompletedAsync(string body, CancellationToken token)
    {
        var message = JsonSerializer.Deserialize<OrderCompletedEvent>(body);
        if (message is null)
            return;

        try
        {
            foreach (var item in message.Items)
            {
                var result = _stockStore.Commit(item.Sku, item.Quantity);
                if (!result)
                {
                    _logger.LogError($"Stock process failed for SKU {item.Sku}");
                    throw new InvalidOperationException(
                            $"Commit failed for {item.Sku}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stock process failed for order {OrderId}", message.OrderId);
            throw;
        }
    }
}
