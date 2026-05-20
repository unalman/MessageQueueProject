namespace Contracts.Events
{
    public sealed record StockReservedEvent(
        Guid OrderId,
        string UserEmail) : IntegrationEvent;
}
