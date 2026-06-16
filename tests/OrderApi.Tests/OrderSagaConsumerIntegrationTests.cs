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

namespace OrderApi.Tests
{
    public class OrderSagaConsumerIntegrationTests : IClassFixture<RabbitMqFixture>
    {
        private readonly RabbitMqFixture _fixture;

        public OrderSagaConsumerIntegrationTests(
            RabbitMqFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task HandleStockReservedAsync_Should_Publish_OrderCompletedEvent()
        {
            var uri = new Uri(_fixture.ConnectionString);
            var host = CreateHost(uri);

            await host.StartAsync();

            var factory = new ConnectionFactory() { Uri = uri };

            await using var connection =
                await factory.CreateConnectionAsync();
            await using var channel =
                await connection.CreateChannelAsync();

            var queueName = $"test-order-completed-{Guid.NewGuid()}";

            await DeclareTestQueueAsync(channel, queueName, MessagingConstants.OrderCompletedEventsRoutingKey);

            var message = new StockReservedEvent(
                Guid.NewGuid(),
                "test@test.com",
                [
                    new OrderItem("SKU-1",2)
                ]);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            //act

            await channel.BasicPublishAsync(
                MessagingConstants.EventsExchangeName,
                MessagingConstants.StockReservedOrderEventsRoutingKey,
                body);

            //assert

            BasicGetResult? result = await WaitForMessageAsync(channel, queueName);

            result.Should().NotBeNull();

            var json = Encoding.UTF8.GetString(result.Body!.ToArray());

            var orderCompletedEvent = JsonSerializer.Deserialize<OrderCompletedEvent>(json);
            orderCompletedEvent.Should().NotBeNull();
            orderCompletedEvent.OrderId.Should().Be(message.OrderId);
            orderCompletedEvent.UserEmail.Should().Be(message.UserEmail);
            orderCompletedEvent.SagaId.Should().Be(message.SagaId);
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

                services.AddHostedService<OrderSagaConsumer>();
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
