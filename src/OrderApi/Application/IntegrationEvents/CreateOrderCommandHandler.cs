using Contracts.Events;
using MediatR;
using Messaging;

namespace OrderApi.Application.IntegrationEvents
{
    public class CreateOrderCommandHandler :
        IRequestHandler<CreateOrderCommand, Guid>
    {
        private readonly IRabbitMqPublisher _rabbitMq;

        public CreateOrderCommandHandler(IRabbitMqPublisher rabbitMq)
        {
            _rabbitMq = rabbitMq;
        }

        public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            var orderId = Guid.NewGuid();
            var sagaId = Guid.NewGuid();

            var orderCreatedEvent = new OrderCreatedEvent(
                orderId,
                request.UserEmail,
                request.Items.ToArray(),
                DateTime.UtcNow,
                request.Payment)
            {
                SagaId = sagaId
            };

            await _rabbitMq.PublishAsync(orderCreatedEvent, MessagingConstants.OrderCreatedEventsRoutingKey, cancellationToken);

            return orderId;
        }
    }
}
