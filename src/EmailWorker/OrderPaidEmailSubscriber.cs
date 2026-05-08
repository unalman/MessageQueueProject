using Contracts;
using Microsoft.Extensions.Options;

namespace EmailWorker;

public sealed class OrderPaidEmailSubscriber(
    IOptions<RabbitMqOptions> options,
    OrderPaidEmailHandler handler,
    ILogger<OrderPaidEmailSubscriber> logger)
    : RabbitMqSubscriberService<OrderPaid>(options, logger)
{
    protected override string QueueName => MessagingConstants.EmailOrderPaidQueueName;
    protected override string RoutingKey => MessagingConstants.OrderPaidRoutingKey;

    protected override Task HandleMessageAsync(OrderPaid message, CancellationToken cancellationToken)
        => handler.HandleAsync(message, cancellationToken);
}
