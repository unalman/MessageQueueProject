using Contracts;
using Microsoft.Extensions.Options;

namespace StockWorker;

public sealed class OrderPaidStockSubscriber(
    IOptions<RabbitMqOptions> options,
    OrderPaidStockHandler handler,
    ILogger<OrderPaidStockSubscriber> logger)
    : RabbitMqSubscriberService<OrderPaid>(options, logger)
{
    protected override string QueueName => MessagingConstants.StockOrderPaidQueueName;
    protected override string RoutingKey => MessagingConstants.OrderPaidRoutingKey;

    protected override Task HandleMessageAsync(OrderPaid message, CancellationToken cancellationToken)
        => handler.HandleAsync(message, cancellationToken);
}
