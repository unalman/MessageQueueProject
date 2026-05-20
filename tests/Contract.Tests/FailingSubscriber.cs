using Contracts;
using Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;

namespace Contract.Tests
{
    public class FailingSubscriber : RabbitMqSubscriberService
    {
        public FailingSubscriber(
            IOptions<RabbitMqOptions> options,
            ILogger<FailingSubscriber> logger) : base(options, logger)
        {
        }

        protected override string QueueName => "orders";

        protected override List<string> RoutingKeys => new List<string>() { "order.paid" };

        protected void HandleMessageAsync(OrderPaid message)
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
