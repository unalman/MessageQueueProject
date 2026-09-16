using Contracts;
using Contracts.Models;

namespace EmailWorker.IntegrationEvents.Events
{
    public sealed record OrderCompletedEvent(
        Guid OrderId,
        string UserEmail,
        IReadOnlyList<OrderItem> Items) : IntegrationEvent;
}
