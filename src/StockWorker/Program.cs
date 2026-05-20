using Messaging;
using StockWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<InMemoryStockStore>();
builder.Services.AddHostedService<StockSagaConsumer>();

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection("RabbitMq"))
    .ValidateOnStart();

var host = builder.Build();
host.Run();
