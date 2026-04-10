var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/payments/charge", (ChargeRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.CardToken))
        return Results.BadRequest(new ChargeResponse(false, "cardToken is required"));

    if (request.Amount <= 0)
        return Results.BadRequest(new ChargeResponse(false, "amount must be > 0"));

    return Results.Ok(new ChargeResponse(true, "charged"));
})
.WithName("ChargePayment");

app.Run();

internal sealed record ChargeRequest(string CardToken, decimal Amount, Guid CorrelationId);
internal sealed record ChargeResponse(bool Success, string? Message);
