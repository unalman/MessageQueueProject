namespace Contracts.Events
{
    public sealed record PaymentFailedEvent(
        Guid OrderId,
        string? ErrorMessage
        ) : IntegrationEvent;
}
