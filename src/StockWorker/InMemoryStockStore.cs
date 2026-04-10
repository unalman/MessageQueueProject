using System.Collections.Concurrent;

namespace StockWorker;

public sealed class InMemoryStockStore
{
    private readonly ConcurrentDictionary<string, int> _stock = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SKU-1"] = 10,
        ["SKU-2"] = 5,
        ["SKU-3"] = 20
    };

    public bool TryDecrease(string sku, int quantity, out int remaining)
    {
        remaining = 0;
        if (quantity <= 0)
            return false;

        while (true)
        {
            if (!_stock.TryGetValue(sku, out var current))
                return false;

            if (current < quantity)
            {
                remaining = current;
                return false;
            }

            var next = current - quantity;
            if (_stock.TryUpdate(sku, next, current))
            {
                remaining = next;
                return true;
            }
        }
    }
}

