using Contracts.Events;
using Messaging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace PaymentApi
{
    public sealed class PaymentSagaConsumer : RabbitMqSubscriberService
    {
        private readonly IRabbitMqPublisher _rabbitMq;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentSagaConsumer> _logger;
        public PaymentSagaConsumer(
            IOptions<RabbitMqOptions> options,
            ILogger<PaymentSagaConsumer> logger,
            IRabbitMqPublisher rabbitMq,
            IPaymentService paymentService)
            : base(options, logger)
        {
            _rabbitMq = rabbitMq;
            _logger = logger;
            _paymentService = paymentService;
            RegisterHandler(MessagingConstants.OrderCreatedEventsRoutingKey, HandleOrderCreatedAsync);
            RegisterHandler(MessagingConstants.StockFailedOrderEventsRoutingKey, HandleStockFailedAsync);
        }
        protected override string QueueName => MessagingConstants.PaymentOrderEventsQueueName;

        protected override List<string> RoutingKeys
            => new List<string>() {
                MessagingConstants.OrderCreatedEventsRoutingKey,
                MessagingConstants.StockFailedOrderEventsRoutingKey
            };

        public async Task HandleOrderCreatedAsync(string body, CancellationToken token)
        {
            var message = JsonSerializer.Deserialize<OrderCreatedEvent>(body);
            if (message is null)
                return;

            try
            {
                var isPaymentSucceeded = await _paymentService.PayAsync(message.payment);

                if (!isPaymentSucceeded)
                {
                    await _rabbitMq.PublishAsync(
                      new PaymentFailedEvent(message.OrderId, "Card declined")
                      {
                          SagaId = message.SagaId
                      },
                      MessagingConstants.PaymentFailedRoutingKey,
                      token);
                    return;
                }

                await _rabbitMq.PublishAsync(
                    new PaymentCompleteEvent(message.OrderId, message.UserEmail, message.Items, message.PaidAtUtc),
                    MessagingConstants.PaymentCompletedRoutingKey,
                    token
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment failed for order {OrderId}", message.OrderId);

                await _rabbitMq.PublishAsync(
                    new PaymentFailedEvent(message.OrderId, ex.Message)
                    {
                        SagaId = message.SagaId
                    },
                    MessagingConstants.PaymentFailedRoutingKey,
                    token);
            }

        }

        public async Task HandleStockFailedAsync(string body, CancellationToken token)
        {
            var message = JsonSerializer.Deserialize<StockFailedEvent>(body);

            if (message is null)
                return;

            // refund process

            await _rabbitMq.PublishAsync(
                  new PaymentRefundedEvent(message.OrderId)
                  {
                      SagaId = message.SagaId
                  },
                  MessagingConstants.PaymentRefundedRoutingKey,
                  token
                );
        }
    }
}
