using Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;

namespace Contract.Tests
{
    public class TestableSubscriberService : TestOrderPaidSubscriber
    {
        public TestableSubscriberService(IOptions<RabbitMqOptions> options, ILogger<TestOrderPaidSubscriber> logger) : base(options, logger)
        {
        }

        public Task InvokeProcessMessageAsync(
            BasicDeliverEventArgs args,
            CancellationToken token)
        {
            return ProcessMessageAsync(args, token);
        }
    }
}
