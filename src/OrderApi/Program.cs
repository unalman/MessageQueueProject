using Contracts.Models;
using MediatR;
using Messaging;
using OrderApi;
using OrderApi.Application.IntegrationEvents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddRabbitMqMessaging(builder.Configuration);

builder.Services.AddHostedService<OrderSagaConsumer>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateOrderCommandHandler>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/orders/purchase", async (
    PurchaseRequest request,
    IMediator mediator,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.UserEmail))
        return Results.BadRequest(new { error = "userEmail is required" });

    if (request.Items is null || request.Items.Count == 0 || request.Items.Any(i => string.IsNullOrWhiteSpace(i.Sku) || i.Quantity <= 0))
        return Results.BadRequest(new { error = "items must include sku and quantity > 0" });

    if (request.Payment is null || request.Payment.Amount <= 0 || string.IsNullOrWhiteSpace(request.Payment.CardToken))
        return Results.BadRequest(new { error = "payment.cardToken and payment.amount > 0 are required" });

    var orderItem = request.Items.Select(x => new OrderItem(x.Sku, x.Quantity)).ToList();
    var createOrderCommand = new CreateOrderCommand(request.UserEmail, orderItem, new Payment(request.Payment.CardToken, request.Payment.Amount));

    var orderId = await mediator.Send(createOrderCommand);

    return Results.Ok(new { orderId, status = "processing" });
});

app.Run();

internal sealed record PurchaseRequest(string UserEmail, List<PurchaseItem> Items, PurchasePayment Payment);
internal sealed record PurchaseItem(string Sku, int Quantity);
internal sealed record PurchasePayment(string CardToken, decimal Amount);
