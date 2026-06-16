using Contracts.Models;

namespace Contracts.Events
{
    public sealed record OrderCompletedEvent(
        Guid OrderId,
        string UserEmail,
        IReadOnlyList<OrderItem> Items) : IntegrationEvent;
}
