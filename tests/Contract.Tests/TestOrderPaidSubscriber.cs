using Contracts;
using Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Contract.Tests
{
    public class TestOrderPaidSubscriber : RabbitMqSubscriberService
    {
        public bool HandleCalled { get; private set; }

        public TestOrderPaidSubscriber(
            IOptions<RabbitMqOptions> options,
            ILogger<TestOrderPaidSubscriber> logger) : base(options, logger)
        { 
        }

        protected override string QueueName => "orders";

        protected override List<string> RoutingKeys => new List<string>() { "order.paid" };

        protected Task HandleMessageAsync(string message, CancellationToken cancellationToken)
        {
            var order = JsonSerializer.Deserialize<OrderPaid>(message);
            if (order is not null)
            {
                HandleCalled = true;
            }
            return Task.CompletedTask;
        }
    }
}
