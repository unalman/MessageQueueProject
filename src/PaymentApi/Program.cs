using Messaging;
using PaymentApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddRabbitMqMessaging(builder.Configuration);

builder.Services.AddHostedService<PaymentSagaConsumer>();

var app = builder.Build();

app.Run();


