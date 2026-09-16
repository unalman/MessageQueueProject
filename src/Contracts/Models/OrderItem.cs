namespace Contracts.Models;

public class OrderItem
{
    public string Sku { get; private set; }
    public int Quantity { get; private set; }

    public OrderItem(string sku, int quantity)
    {
        Sku = sku;
        Quantity = quantity;
    }

    public void AddQuantity(int quantity)
    {
        Quantity += quantity;
    }
};

