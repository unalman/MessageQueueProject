using Contracts.Models;
using MediatR;

namespace OrderApi.Application.Commands
{
    public record CreateOrderCommand(string UserEmail, List<OrderItem> Items, Payment Payment) : IRequest<Guid>;
}
