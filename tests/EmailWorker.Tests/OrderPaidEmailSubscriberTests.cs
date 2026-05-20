using Messaging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Contracts;

namespace EmailWorker.Tests
{
    public sealed class OrderPaidEmailSubscriberTests : IClassFixture<RabbitMqFixture>
    {
        private readonly RabbitMqFixture _fixture;

        public OrderPaidEmailSubscriberTests(
            RabbitMqFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task OrderPaid_Should_Be_Consumed_By_EmailWorker()
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

                    services.AddSingleton<OrderPaidEmailHandler>();
                    services.AddHostedService<OrderPaidEmailSubscriber>();
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
                    new OrderItem("ABC", 2)
                ],
                DateTime.UtcNow
            );

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            //act

            var props = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                ContentType = "application/json"
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

            var result = await channel.QueueDeclarePassiveAsync(MessagingConstants.EmailOrderPaidQueueName);
            result.MessageCount.Should().Be(0);
        }
    }
}
