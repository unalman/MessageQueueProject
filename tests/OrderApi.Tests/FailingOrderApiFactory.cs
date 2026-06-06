using Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderApi.Tests
{
    public class FailingOrderApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IRabbitMqPublisher>();
                services.AddSingleton<IRabbitMqPublisher, ThrowingRabbitMqPublisher>();
                services.RemoveAll<IHostedService>();
            });
        }
    }
}
