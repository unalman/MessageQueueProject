namespace Contracts;

public static class MessagingConstants
{
    public const string EventsExchangeName = "ecommerce.events";
    public const string OrderPaidRoutingKey = "order.paid";

    public const string EmailOrderPaidQueueName = "email.orderpaid";
    public const string StockOrderPaidQueueName = "stock.orderpaid";
}

