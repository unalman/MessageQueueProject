namespace Contracts;

public sealed record OrderPaid(
    Guid OrderId,
    string UserEmail,
    IReadOnlyList<OrderItem> Items,
    DateTime PaidAtUtc,
    Guid CorrelationId);

