using Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;

namespace Contract.Tests
{
    public sealed class FailingSubscriber : RabbitMqSubscriberService<OrderPaid>
    {
        public FailingSubscriber(
            IOptions<RabbitMqOptions> options,
            ILogger<FailingSubscriber> logger) : base(options, logger)
        {
        }

        protected override string QueueName => "orders";

        protected override string RoutingKey => "order.paid";

        protected override Task HandleMessageAsync(OrderPaid message, CancellationToken cancellationToken)
        {
            throw new Exception("processing failed");
        }

        public Task InvokeProcessMessageAsync(
            BasicDeliverEventArgs args,
            CancellationToken token)
        {
            return ProcessMessageAsync(args, token);
        }
    }
}
