using Contracts;
using Contracts.Models;

namespace StockWorker.IntegrationEvents.Events
{
    public sealed record OrderCompletedEvent(
        Guid OrderId,
        string UserEmail,
        IReadOnlyList<OrderItem> Items) : IntegrationEvent;
}
