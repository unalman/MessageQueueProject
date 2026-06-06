using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;

namespace Messaging.Tests
{
    public class TestableSubscriberService : TestSagaConsumer
    {
        public TestableSubscriberService(
            IOptions<RabbitMqOptions> options,
            ILogger<TestableSubscriberService> logger) : base(options, logger)
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
