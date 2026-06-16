using StockWorker.Models;
using System.Collections.Concurrent;

namespace StockWorker;

public sealed class InMemoryStockStore
{
    private readonly ConcurrentDictionary<string, StockItem> _stock = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SKU-1"] = new(10),
        ["SKU-2"] = new(5),
        ["SKU-3"] = new(20)
    };

    public bool TryReserve(string sku, int quantity)
    {
        if (quantity <= 0)
            return false;

        if (!_stock.TryGetValue(sku, out var item))
            return false;

        if (item.Available < quantity)
            return false;

        lock (item)
        {
            if (item.Available < quantity)
                return false;

            item.Available -= quantity;
            item.Reseverved += quantity;

            return true;
        }
    }

    public bool Release(string sku, int quantity)
    {
        if (!_stock.TryGetValue(sku, out var item))
            return false;

        lock (item)
        {
            if (item.Reseverved < quantity)
                return false;

            item.Reseverved -= quantity;
            item.Available += quantity;

            return true;
        }
    }

    public bool Commit(string sku, int quantity)
    {
        if (!_stock.TryGetValue(sku, out var item))
            return false;

        lock (item)
        {
            if (item.Reseverved < quantity)
                return false;

            item.Reseverved -= quantity;

            return true;
        }
    }

    public StockItem? GetStock(string sku)
    {
        return _stock.TryGetValue(sku, out var stock)
            ? stock
            : null;
    }

    public void Reset()
    {
        _stock["SKU-1"] = new(10);
        _stock["SKU-2"] = new(5);
        _stock["SKU-3"] = new(20);
    }
}

