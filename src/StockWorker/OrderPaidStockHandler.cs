using Contracts;

namespace StockWorker;

public sealed class OrderPaidStockHandler(InMemoryStockStore stockStore, ILogger<OrderPaidStockHandler> logger)
{
    public Task HandleAsync(OrderPaid message, CancellationToken _)
    {
        foreach (var item in message.Items)
        {
            if (stockStore.TryDecrease(item.Sku, item.Quantity, out var remaining))
            {
                logger.LogInformation(
                    "Stock decreased for SKU {Sku} by {Quantity}. Remaining: {Remaining} (OrderId: {OrderId})",
                    item.Sku,
                    item.Quantity,
                    remaining,
                    message.OrderId);
            }
            else
            {
                logger.LogWarning(
                    "Insufficient stock for SKU {Sku} (requested {Quantity}). (OrderId: {OrderId})",
                    item.Sku,
                    item.Quantity,
                    message.OrderId);
            }
        }

        return Task.CompletedTask;
    }
}
