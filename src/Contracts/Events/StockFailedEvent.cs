
namespace Contracts.Events
{
    public sealed record StockFailedEvent(
         Guid OrderId,
         string Reason
        ) : IntegrationEvent;
}
