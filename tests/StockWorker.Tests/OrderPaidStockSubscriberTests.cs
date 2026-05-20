using Contracts;
using FluentAssertions;
using Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace StockWorker.Tests
{
    public class OrderPaidStockSubscriberTests : IClassFixture<RabbitMqFixture>
    {
        private readonly RabbitMqFixture _fixture;

        public OrderPaidStockSubscriberTests(RabbitMqFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task OrderPaid_Should_Decrease_Stock_When_Consumed()
        {
            var uri = new Uri(_fixture.ConnectionString);

            using var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<RabbitMqOptions>(options =>
                    {
                        options.Host = uri.Host;
                        options.Port = uri.Port;
                        options.User = uri.UserInfo.Split(":")[0];
                        options.Pass = uri.UserInfo.Split(":")[1];
                        options.VirtualHost = "/";
                    });

                    services.AddSingleton<OrderPaidStockHandler>();
                    services.AddSingleton<InMemoryStockStore>();
                    services.AddHostedService<StockSagaConsumer>();
                })
                .Build();

            await host.StartAsync();
            await Task.Delay(1000);
            //Publish

            var factory = new ConnectionFactory
            {
                Uri = uri
            };

            await using var connection =
                await factory.CreateConnectionAsync();

            await using var channel =
                await connection.CreateChannelAsync();

            var message = new OrderPaid(
                Guid.NewGuid(),
                "test@test.com",
                [
                    new OrderItem("SKU-1", 2)
                ],
                DateTime.UtcNow
            );

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            //act

            var props = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                ContentType = "application/json",
            };

            await channel.BasicPublishAsync(
                exchange: MessagingConstants.EventsExchangeName,
                routingKey: MessagingConstants.OrderPaidRoutingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: CancellationToken.None
            );

            await Task.Delay(2000);

            var stockStore = host.Services.GetRequiredService<InMemoryStockStore>();

            stockStore.GetStock("SKU-1").Should().Be(8);

            var result = await channel.QueueDeclarePassiveAsync(MessagingConstants.StockOrderPaidQueueName);
            result.MessageCount.Should().Be(0);
        }
    }
}
