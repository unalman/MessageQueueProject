using Contracts.Models;

namespace Contracts.Events
{
    public sealed record PaymentCompleteEvent(
            Guid OrderId,
            string UserEmail,
            IReadOnlyList<OrderItem> Items,
            DateTime PaidAtUtc
        ) : IntegrationEvent;
}
