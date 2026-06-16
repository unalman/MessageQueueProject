using Contracts.Models;

namespace Contracts.Events
{
    public sealed record StockReservedEvent(
        Guid OrderId,
        string UserEmail,
        IReadOnlyList<OrderItem> Items) : IntegrationEvent;
}
