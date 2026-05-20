namespace Contracts.Events
{
    public sealed record OrderCompletedEvent(
        Guid OrderId,
        string UserEmail) : IntegrationEvent;
}
