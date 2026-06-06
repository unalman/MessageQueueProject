using Contracts.Models;

namespace PaymentApi
{
    public interface IPaymentService
    {
        public Task<bool> PayAsync(Payment payment);
    }
}
