namespace OrderApi.Domain.Entities
{
    public enum OrderStatus
    {
        Created = 1,
        Paid = 2,
        StockConfirmed = 3,
        Cancelled = 4
    }
}
