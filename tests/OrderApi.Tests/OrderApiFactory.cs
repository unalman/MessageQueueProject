using Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace OrderApi.Tests
{
    public sealed class OrderApiFactory : WebApplicationFactory<Program>
    {
        public FakeRabbitMqPublisher Publisher { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services
                    .SingleOrDefault(x => x.ServiceType == typeof(IRabbitMqPublisher));

                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddSingleton<IRabbitMqPublisher>(Publisher);
            });
        }
    }
}
