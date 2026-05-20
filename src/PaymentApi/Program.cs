using Messaging;
using PaymentApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection("RabbitMq"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddHostedService<PaymentSagaConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();


