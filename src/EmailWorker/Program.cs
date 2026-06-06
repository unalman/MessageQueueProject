using EmailWorker;
using Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRabbitMqMessaging(builder.Configuration);

builder.Services.AddHostedService<EmailSagaConsumer>();

var host = builder.Build();
host.Run();
