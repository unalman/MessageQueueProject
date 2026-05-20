namespace Messaging;

public static class MessagingConstants
{
    public const string EventsExchangeName = "saga.events";

    public const string OrderOrderEventsQueueName = "order.order-events";
    public const string OrderCreatedEventsRoutingKey = "order.created";
    public const string OrderCompletedEventsRoutingKey = "order.completed";

    public const string PaymentOrderEventsQueueName = "payment.order-events";
    public const string PaymentCompletedRoutingKey = "payment.completed";
    public const string PaymentFailedRoutingKey = "payment.failed";
    public const string PaymentRefundedRoutingKey = "payment.refunded";

    public const string StockOrderEventsQueueName = "stock.order-events";
    public const string StockReservedOrderEventsRoutingKey = "stock.reserved";
    public const string StockFailedOrderEventsRoutingKey = "stock.failed";

    public const string EmailOrderEventsQueueName = "email.order-events";
    public const string EmailSentOrderEventsRoutingKey = "email.sent";
}