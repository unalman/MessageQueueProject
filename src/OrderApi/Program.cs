using Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection("RabbitMq"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient("PaymentApi", client =>
{
    var baseUrl = builder.Configuration["PaymentApi:BaseUrl"] ?? "http://localhost:5082";
    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
});

builder.Services.AddSingleton<RabbitMqConnectionProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/orders/purchase", async (
    PurchaseRequest request,
    IHttpClientFactory httpClientFactory,
    RabbitMqConnectionProvider rabbitMq,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.UserEmail))
        return Results.BadRequest(new { error = "userEmail is required" });

    if (request.Items is null || request.Items.Count == 0 || request.Items.Any(i => string.IsNullOrWhiteSpace(i.Sku) || i.Quantity <= 0))
        return Results.BadRequest(new { error = "items must include sku and quantity > 0" });

    if (request.Payment is null || request.Payment.Amount <= 0 || string.IsNullOrWhiteSpace(request.Payment.CardToken))
        return Results.BadRequest(new { error = "payment.cardToken and payment.amount > 0 are required" });

    var orderId = Guid.NewGuid();
    var correlationId = Guid.NewGuid();

    var client = httpClientFactory.CreateClient("PaymentApi");
    var paymentResponse = await client.PostAsJsonAsync(
        "/payments/charge",
        new ChargeRequest(request.Payment.CardToken, request.Payment.Amount, correlationId),
        cancellationToken);

    if (!paymentResponse.IsSuccessStatusCode)
        return Results.BadRequest(new { error = "payment failed" });

    var chargeResult = await paymentResponse.Content.ReadFromJsonAsync<ChargeResponse>(cancellationToken: cancellationToken);
    if (chargeResult is null || !chargeResult.Success)
        return Results.BadRequest(new { error = "payment failed", detail = chargeResult?.Message });

    var paidEvent = new OrderPaid(
        orderId,
        request.UserEmail,
        request.Items.Select(i => new OrderItem(i.Sku, i.Quantity)).ToArray(),
        DateTime.UtcNow,
        correlationId);

    await rabbitMq.PublishAsync(paidEvent, cancellationToken);

    return Results.Ok(new { orderId, status = "paid" });
});

app.Run();

internal sealed record PurchaseRequest(string UserEmail, List<PurchaseItem> Items, PurchasePayment Payment);
internal sealed record PurchaseItem(string Sku, int Quantity);
internal sealed record PurchasePayment(string CardToken, decimal Amount);

internal sealed record ChargeRequest(string CardToken, decimal Amount, Guid CorrelationId);
internal sealed record ChargeResponse(bool Success, string? Message);

internal sealed class RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnectionProvider> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task PublishAsync(OrderPaid message, CancellationToken cancellationToken)
    {
        var connection = await GetOrCreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: MessagingConstants.EventsExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var bodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: MessagingConstants.EventsExchangeName,
            routingKey: MessagingConstants.OrderPaidRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: bodyBytes,
            cancellationToken: cancellationToken);

        logger.LogInformation("Published OrderPaid event for OrderId {OrderId}", message.OrderId);
    }

    private async Task<IConnection> GetOrCreateConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _connection?.Dispose();

            _connection = await RabbitMqRetryHelper.ExecuteWithRetryAsync(
                async token => await options.Value.CreateFactory().CreateConnectionAsync(token),
                logger,
                "RabbitMQ connect",
                cancellationToken,
                maxDelaySeconds: 30);

            logger.LogInformation("RabbitMQ connected to {Host}", options.Value.Host);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }
}
