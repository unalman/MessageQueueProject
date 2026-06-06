using Contracts.Models;
using Contracts.Events;
using Messaging;
using OrderApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddRabbitMqMessaging(builder.Configuration);

builder.Services.AddHostedService<OrderSagaConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/orders/purchase", async (
    PurchaseRequest request,
    IRabbitMqPublisher rabbitMq,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.UserEmail))
        return Results.BadRequest(new { error = "userEmail is required" });

    if (request.Items is null || request.Items.Count == 0 || request.Items.Any(i => string.IsNullOrWhiteSpace(i.Sku) || i.Quantity <= 0))
        return Results.BadRequest(new { error = "items must include sku and quantity > 0" });

    if (request.Payment is null || request.Payment.Amount <= 0 || string.IsNullOrWhiteSpace(request.Payment.CardToken))
        return Results.BadRequest(new { error = "payment.cardToken and payment.amount > 0 are required" });

    var orderId = Guid.NewGuid();
    var sagaId = Guid.NewGuid();

    var orderCreatedEvent = new OrderCreatedEvent(
        orderId,
        request.UserEmail,
        request.Items.Select(i => new OrderItem(i.Sku, i.Quantity)).ToArray(),
        DateTime.UtcNow,
        new Payment(request.Payment.CardToken, request.Payment.Amount))
    {
        SagaId = sagaId
    };

    await rabbitMq.PublishAsync(orderCreatedEvent, MessagingConstants.OrderCreatedEventsRoutingKey, cancellationToken);

    return Results.Ok(new { orderId, status = "paid" });
});

app.Run();

internal sealed record PurchaseRequest(string UserEmail, List<PurchaseItem> Items, PurchasePayment Payment);
internal sealed record PurchaseItem(string Sku, int Quantity);
internal sealed record PurchasePayment(string CardToken, decimal Amount);
