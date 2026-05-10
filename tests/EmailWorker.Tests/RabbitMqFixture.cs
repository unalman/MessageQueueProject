using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace EmailWorker.Tests
{
    public class RabbitMqFixture : IAsyncLifetime
    {
        private readonly RabbitMqContainer _container =
            new RabbitMqBuilder("rabbitmq:3-management").Build();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }

        public string ConnectionString => _container.GetConnectionString();
    }
}
