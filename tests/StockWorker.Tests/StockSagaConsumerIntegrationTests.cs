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

namespace StockWorker.Tests
{
    public class StockSagaConsumerIntegrationTests : IClassFixture<RabbitMqFixture>
    {
        private readonly RabbitMqFixture _fixture;
        public StockSagaConsumerIntegrationTests(
            RabbitMqFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task PaymentCompleted_Should_Reserve_Stock_And_Publish_StockReserved()
        {
            var uri = new Uri(_fixture.ConnectionString);
            using var host = CreateHost(uri);

            await host.StartAsync();

            //publisher connection

            var factory = new ConnectionFactory()
            {
                Uri = uri
            };

            await using var connection =
                await factory.CreateConnectionAsync();

            await using var channel =
                await connection.CreateChannelAsync();

            var queueName = $"test-stock-reserved-{Guid.NewGuid()}";
            try
            {
                // Test Queue
                await DeclareTestQueueAsync(channel, queueName, MessagingConstants.StockReservedOrderEventsRoutingKey);

                var message = new PaymentCompleteEvent(
                    Guid.NewGuid(),
                    "test@test.com",
                    [
                        new OrderItem("SKU-1", 2)
                    ],
                    DateTime.UtcNow);

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                //act

                await channel.BasicPublishAsync(
                    MessagingConstants.EventsExchangeName,
                    MessagingConstants.PaymentCompletedRoutingKey,
                    false,
                    body);

                BasicGetResult? result = await WaitForMessageAsync(channel, queueName);

                // Assert

                result.Should().NotBeNull();

                var stockStore = host.Services.GetRequiredService<InMemoryStockStore>();

                stockStore.GetStock("SKU-1").Should().Be(8);

                var json = Encoding.UTF8.GetString(result!.Body.ToArray());

                var reservedEvent = JsonSerializer.Deserialize<StockReservedEvent>(json);
                reservedEvent.Should().NotBeNull();

                reservedEvent!.UserEmail.Should().Be("test@test.com");
                reservedEvent.OrderId.Should().Be(message.OrderId);
            }
            finally
            {
                await channel.QueueDeleteAsync(queueName);
            }
        }

        [Fact]
        public async Task PaymentCompleted_Should_Publish_StockFailed_When_Insufficient()
        {
            var uri = new Uri(_fixture.ConnectionString);
            var host = CreateHost(uri);

            await host.StartAsync();

            var factory = new ConnectionFactory() { Uri = uri };

            await using var connection =
               await factory.CreateConnectionAsync();
            await using var channel =
                await connection.CreateChannelAsync();

            var queueName = $"test-stock-failed-{Guid.NewGuid()}";

            try
            {
                await DeclareTestQueueAsync(channel, queueName, MessagingConstants.StockFailedOrderEventsRoutingKey);

                var message = new PaymentCompleteEvent(
                  Guid.NewGuid(),
                  "test@test.com",
                  [
                      new OrderItem("SKU-1", 11)
                  ],
                  DateTime.UtcNow);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                await channel.BasicPublishAsync(MessagingConstants.EventsExchangeName, MessagingConstants.PaymentCompletedRoutingKey, body);

                BasicGetResult? result = await WaitForMessageAsync(channel, queueName);

                result.Should().NotBeNull();

                var stockStore = host.Services.GetRequiredService<InMemoryStockStore>();
                stockStore.GetStock("SKU-1").Should().Be(10);

                var stockFailedEvent = JsonSerializer.Deserialize<StockFailedEvent>(Encoding.UTF8.GetString(result!.Body.ToArray()));

                stockFailedEvent.Should().NotBeNull();
                stockFailedEvent.OrderId.Should().Be(message.OrderId);
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

                services.AddSingleton<InMemoryStockStore>();

                services.AddHostedService<StockSagaConsumer>();
            })
            .Build();
        }

        private static async Task DeclareTestQueueAsync(IChannel channel, string queueName, string routingKey)
        {
            await channel.ExchangeDeclareAsync(
                exchange: MessagingConstants.EventsExchangeName,
                type: ExchangeType.Topic,
                durable: true
            );

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: MessagingConstants.EventsExchangeName,
                routingKey: routingKey
            );
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
