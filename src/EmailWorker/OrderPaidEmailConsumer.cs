using Contracts;

namespace EmailWorker;

public sealed class OrderPaidEmailHandler(ILogger<OrderPaidEmailHandler> logger)
{
    public Task HandleAsync(OrderPaid message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Mail sent to {UserEmail} for OrderId {OrderId} (CorrelationId: {CorrelationId})",
            message.UserEmail,
            message.OrderId,
            message.CorrelationId);

        return Task.CompletedTask;
    }
}
