namespace Contracts.Events
{
    public record PaymentRefundedEvent(
        Guid OrderId
        ) : IntegrationEvent;
}
