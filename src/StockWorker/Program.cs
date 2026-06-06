using Messaging;
using StockWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<InMemoryStockStore>();

builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddHostedService<StockSagaConsumer>();

var host = builder.Build();
host.Run();
