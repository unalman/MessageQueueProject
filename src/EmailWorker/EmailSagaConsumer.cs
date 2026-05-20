using Contracts.Events;
using Messaging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EmailWorker
{
    public sealed class EmailSagaConsumer : RabbitMqSubscriberService
    {
        private readonly ILogger<EmailSagaConsumer> _logger;
        private readonly RabbitMqConnectionProvider _rabbitMq;
        public EmailSagaConsumer(
            IOptions<RabbitMqOptions> options,
            ILogger<EmailSagaConsumer> logger,
            RabbitMqConnectionProvider rabbitMq) : base(options, logger)
        {
            _logger = logger;
            _rabbitMq = rabbitMq;

            RegisterHandler(MessagingConstants.OrderCompletedEventsRoutingKey, HandleOrderCompletedAsync);
        }

        protected override string QueueName => MessagingConstants.EmailOrderEventsQueueName;

        protected override List<string> RoutingKeys => new List<string>()
        {
            MessagingConstants.OrderCompletedEventsRoutingKey
        };

        private async Task HandleOrderCompletedAsync(string body, CancellationToken token)
        {
            var message = JsonSerializer.Deserialize<OrderCompletedEvent>(body);
            if (message is null)
                return;

            _logger.LogInformation(
                "Sending email for OrderId: {OrderId}",
                message.OrderId);

            try
            {
                // Email sent logic
                // await _emailService.SendAsync(...);

                await _rabbitMq.PublishAsync(
                    new EmailSentEvent()
                    {
                        SagaId = message.SagaId
                    },
                    MessagingConstants.EmailSentOrderEventsRoutingKey,
                    token);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Email sending failed for OrderId: {OrderId}",
                    message.OrderId);

                var retryCount = message.RetryCount;

                if (retryCount >= 3)
                    throw;

                await _rabbitMq.PublishAsync(
                    new OrderCompeletedEvent(message.OrderId, message.UserEmail)
                    {
                        SagaId = message.SagaId,
                        RetryCount = retryCount + 1
                    },
                    MessagingConstants.OrderCompletedEventsRoutingKey,
                    token);
            }
        }

    }
}
