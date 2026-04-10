using Contracts;
using EmailWorker;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection("RabbitMq"))
    .ValidateOnStart();

builder.Services.AddSingleton<OrderPaidEmailHandler>();
builder.Services.AddHostedService<OrderPaidEmailSubscriber>();

var host = builder.Build();
host.Run();
