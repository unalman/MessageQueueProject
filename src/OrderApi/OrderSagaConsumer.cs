using Contracts.Events;
using Messaging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace OrderApi
{
    public sealed class OrderSagaConsumer : RabbitMqSubscriberService
    {
        private readonly ILogger<OrderSagaConsumer> _logger;
        private readonly RabbitMqConnectionProvider _rabbitMq;
        public OrderSagaConsumer(
           IOptions<RabbitMqOptions> options,
           ILogger<OrderSagaConsumer> logger,
           RabbitMqConnectionProvider rabbitMq) : base(options, logger)
        {
            _logger = logger;
            _rabbitMq = rabbitMq;

            RegisterHandler(MessagingConstants.PaymentFailedRoutingKey, HandlePaymentFailedAsync);
            RegisterHandler(MessagingConstants.PaymentRefundedRoutingKey, HandlePaymentRefundedAsync);
            RegisterHandler(MessagingConstants.StockReservedOrderEventsRoutingKey, HandleStockReservedAsync);
        }

        protected override string QueueName => MessagingConstants.OrderOrderEventsQueueName;

        protected override List<string> RoutingKeys => new List<string>() {
            MessagingConstants.PaymentFailedRoutingKey,
            MessagingConstants.PaymentRefundedRoutingKey,
            MessagingConstants.StockReservedOrderEventsRoutingKey
        };

        private Task HandlePaymentFailedAsync(
            string body,
            CancellationToken token)
        {
            var message =
                JsonSerializer.Deserialize<PaymentFailedEvent>(body);

            // order status  = cancelled
            return Task.CompletedTask;
        }

        private Task HandlePaymentRefundedAsync(
            string body,
            CancellationToken token)
        {
            var message =
                JsonSerializer.Deserialize<PaymentRefundedEvent>(body);

            // order status = refunded
            return Task.CompletedTask;
        }

        private async Task HandleStockReservedAsync(
            string body,
            CancellationToken token)
        {
            var message =
                JsonSerializer.Deserialize<StockReservedEvent>(body);

            if (message is null)
                return;

            // order status = completed

            await _rabbitMq.PublishAsync(
                new OrderCompletedEvent(message.OrderId, message.UserEmail)
                {
                    SagaId = message.SagaId,
                },
                MessagingConstants.OrderCompletedEventsRoutingKey,
                token);
        }
    }
}
