using Contracts.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Messaging.Tests
{
    public class FailingSubscriber : RabbitMqSubscriberService
    {
        public FailingSubscriber(
          IOptions<RabbitMqOptions> options,
          ILogger<FailingSubscriber> logger) : base(options, logger)
        {
            RegisterHandler(MessagingConstants.OrderCreatedEventsRoutingKey, HandleMessageAsync);
        }

        protected override string QueueName => MessagingConstants.OrderOrderEventsQueueName;

        protected override List<string> RoutingKeys => new List<string>() { MessagingConstants.OrderCreatedEventsRoutingKey };

        protected Task HandleMessageAsync(string body, CancellationToken token)
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
