using Contracts.Models;
using EmailWorker.IntegrationEvents;
using EmailWorker.IntegrationEvents.Events;
using FluentAssertions;
using Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace EmailWorker.Tests
{
    public class EmailSagaConsumerIntegrationTests : IClassFixture<RabbitMqFixture>
    {
        private readonly RabbitMqFixture _fixture;

        public EmailSagaConsumerIntegrationTests(RabbitMqFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task HandleOrderCompletedAsync_Should_Publish_EmailSentEvent()
        {
            var uri = new Uri(_fixture.ConnectionString);
            using var host = CreateHost(uri);

            await host.StartAsync();

            var factory = new ConnectionFactory() { Uri = uri };

            await using var connection =
                await factory.CreateConnectionAsync();
            await using var channel =
                await connection.CreateChannelAsync();

            var queueName = "test-email-sent-events-" + Guid.NewGuid();

            try
            {
                await DeclareTestQueueAsync(channel, queueName, MessagingConstants.EmailSentOrderEventsRoutingKey);

                var message = new OrderCompletedEvent(
                    Guid.NewGuid(),
                    "test@test.com",
                    [
                    new OrderItem("SKU-1",2)
                    ]);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                await channel.BasicPublishAsync(
                    MessagingConstants.EventsExchangeName,
                    MessagingConstants.OrderCompletedEventsRoutingKey,
                    false,
                    body);

                BasicGetResult? result = await WaitForMessageAsync(channel, queueName);

                result.Should().NotBeNull();

                var json = Encoding.UTF8.GetString(result.Body.ToArray());
                var emailSentMessage = JsonSerializer.Deserialize<EmailSentEvent>(json);
                emailSentMessage.Should().NotBeNull();
                emailSentMessage.SagaId.Should().Be(message.SagaId);
            }
            finally
            {
                await channel.QueueDeleteAsync(queueName);
            }
        }

        private IHost CreateHost(Uri uri)
        {
            return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.Configure<RabbitMqOptions>(options =>
                {
                    options.Host = uri.Host;
                    options.Port = uri.Port;
                    options.User = "rabbitmq";
                    options.Pass = "rabbitmq";
                    options.VirtualHost = "/";
                });

                services.AddSingleton(sp =>
                {
                    var options = sp
                        .GetRequiredService<IOptions<RabbitMqOptions>>()
                        .Value;

                    return options.CreateFactory();
                });

                services.AddSingleton<
                    IRabbitMqConnectionFactory,
                    RabbitMqConnectionFactory>();

                services.AddSingleton<
                    IRabbitMqPublisher,
                    RabbitMqConnectionProvider>();


                services.AddHostedService<EmailSagaConsumer>();
            })
            .Build();
        }

        private static async Task DeclareTestQueueAsync(IChannel channel, string queueName, string routingKey)
        {
            await channel.ExchangeDeclareAsync(
                exchange: MessagingConstants.EventsExchangeName,
                type: ExchangeType.Topic,
                durable: true);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false);

            await channel.QueueBindAsync(
               queue: queueName,
               exchange: MessagingConstants.EventsExchangeName,
               routingKey: routingKey);
        }

        private static async Task<BasicGetResult?> WaitForMessageAsync(IChannel channel, string queueName, int retryCount = 5)
        {
            for (var i = 0; i < retryCount; i++)
            {
                var result = await channel.BasicGetAsync(
                    queueName,
                    true);

                if (result is not null)
                    return result;

                await Task.Delay(500);
            }

            return null;
        }
    }
}
