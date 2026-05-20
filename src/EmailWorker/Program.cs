using EmailWorker;
using Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection("RabbitMq"))
    .ValidateOnStart();

builder.Services.AddSingleton<RabbitMqConnectionProvider>();

builder.Services.AddHostedService<EmailSagaConsumer>();

var host = builder.Build();
host.Run();
