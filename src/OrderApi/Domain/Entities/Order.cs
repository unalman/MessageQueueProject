using Contracts.Models;

namespace OrderApi.Domain.Entities
{
    public class Order
    {
        public Guid OrderId { get; set; }

        public OrderStatus OrderStatus { get; private set; }

        public DateTime OrderDate { get; private set; }

        public Buyer Buyer { get; }

        private readonly List<OrderItem> _orderItems;

        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

        public Order(Guid orderId, string email, DateTime orderDate)
        {
            OrderStatus = OrderStatus.Created;
            OrderId = orderId;
            OrderDate = orderDate;
            Buyer = new Buyer(email);
        }

        public void AddOrderItem(string sku, int quantity)
        {
            var existingOrderSku = _orderItems.SingleOrDefault(x => x.Sku == sku);
            if (existingOrderSku != null)
            {
                existingOrderSku.AddQuantity(quantity);
            }
            else
            {
                _orderItems.Add(new OrderItem(sku, quantity));
            }
        }
    }
}
