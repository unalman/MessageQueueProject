using Contracts.Events;
using Contracts.Models;
using FluentAssertions;
using Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace PaymentApi.Tests
{
    public class PaymentSagaConsumerIntegrationTest : IClassFixture<RabbitMqFixture>
    {
        private readonly RabbitMqFixture _fixture;

        public PaymentSagaConsumerIntegrationTest(RabbitMqFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task HandleOrderCreatedAsync_Should_Publish_PaymentCompleteEvent()
        {
            Uri uri = new Uri(_fixture.ConnectionString);
            var host = CreateHost(uri);

            await host.StartAsync();

            var consumer =
              host.Services
                  .GetServices<IHostedService>()
                  .OfType<PaymentSagaConsumer>()
                  .Single();

            await consumer.Started.Task;

            var factory = new ConnectionFactory() { Uri = uri };

            await using var connection =
                await factory.CreateConnectionAsync();
            await using var channel =
                await connection.CreateChannelAsync();

            var queueName = $"test-payment-complete-{Guid.NewGuid()}";
            try
            {
                await DeclareTestQueueAsync(channel, queueName, MessagingConstants.PaymentCompletedRoutingKey);

                var message = new OrderCreatedEvent(
                   Guid.NewGuid(),
                   "test@gmail.com",
                   [
                        new OrderItem("SKU-1",2)
                   ],
                   DateTime.UtcNow,
                   new Payment("cardToken", 100));

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                await channel.BasicPublishAsync(
                    MessagingConstants.EventsExchangeName,
                    MessagingConstants.OrderCreatedEventsRoutingKey,
                    body,
                    CancellationToken.None);

                BasicGetResult? result = await WaitForMessageAsync(channel, queueName);

                result.Should().NotBeNull();

                var json = Encoding.UTF8.GetString(result.Body.ToArray());

                var paymentCompleted = JsonSerializer.Deserialize<PaymentCompleteEvent>(json);
                paymentCompleted.Should().NotBeNull();
                paymentCompleted.OrderId.Should().Be(message.OrderId);
                paymentCompleted.SagaId.Should().Be(message.SagaId);
                paymentCompleted.UserEmail.Should().Be(message.UserEmail);
            }
            finally
            {
                await channel.QueueDeleteAsync(queueName);
                await host.StopAsync();
            }
        }

        [Fact]
        public async Task HandleOrderCreatedAsync_Should_Publish_PaymentFailedEvent()
        {
            Uri uri = new Uri(_fixture.ConnectionString);
            var host = CreateHost(uri, false);

            await host.StartAsync();

            var consumer =
           host.Services
               .GetServices<IHostedService>()
               .OfType<PaymentSagaConsumer>()
               .Single();

            await consumer.Started.Task;

            var factory = new ConnectionFactory() { Uri = uri };

            await using var connection =
                await factory.CreateConnectionAsync();
            await using var channel =
                await connection.CreateChannelAsync();

            var queueName = $"test-payment-failed-{Guid.NewGuid()}";
            try
            {
                await DeclareTestQueueAsync(channel, queueName, MessagingConstants.PaymentFailedRoutingKey);

                var message = new OrderCreatedEvent(
                   Guid.NewGuid(),
                   "test@gmail.com",
                   [
                        new OrderItem("SKU-1",2)
                   ],
                   DateTime.UtcNow,
                   new Payment("cardToken", 100));

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                await channel.BasicPublishAsync(
                    MessagingConstants.EventsExchangeName,
                    MessagingConstants.PaymentFailedRoutingKey,
                    body,
                    CancellationToken.None);

                BasicGetResult? result = await WaitForMessageAsync(channel, queueName);

                result.Should().NotBeNull();

                var json = Encoding.UTF8.GetString(result.Body.ToArray());

                var paymentFailed = JsonSerializer.Deserialize<PaymentFailedEvent>(json);
                paymentFailed.Should().NotBeNull();
                paymentFailed.OrderId.Should().Be(message.OrderId);
                paymentFailed.SagaId.Should().Be(message.SagaId);
            }
            finally
            {
                await channel.QueueDeleteAsync(queueName);
                await host.StopAsync();
            }
        }

        private IHost CreateHost(Uri uri, bool isPaymentSucceeded = true)
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

                services.AddSingleton<IPaymentService>(
                    new FakePaymentService()
                    {
                        IsPaymentSucceed = isPaymentSucceeded
                    });

                services.AddHostedService<PaymentSagaConsumer>();
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
