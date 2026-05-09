using Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Contract.Tests
{
    public class TestOrderPaidSubscriber : RabbitMqSubscriberService<OrderPaid>
    {
        public bool HandleCalled { get; private set; }

        public TestOrderPaidSubscriber(
            IOptions<RabbitMqOptions> options,
            ILogger<TestOrderPaidSubscriber> logger) : base(options, logger)
        { 
        }

        protected override string QueueName => "orders";

        protected override string RoutingKey => "order.paid";

        protected override Task HandleMessageAsync(OrderPaid message, CancellationToken cancellationToken)
        {
            HandleCalled = true;
            return Task.CompletedTask;
        }
    }
}
