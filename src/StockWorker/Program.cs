using Contracts;
using Microsoft.Extensions.Options;
using StockWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<InMemoryStockStore>();
builder.Services.AddSingleton<OrderPaidStockHandler>();
builder.Services.AddHostedService<OrderPaidStockSubscriber>();

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection("RabbitMq"))
    .ValidateOnStart();

var host = builder.Build();
host.Run();
