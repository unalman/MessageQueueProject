using Contracts.Models;

namespace PaymentApi
{
    public class PaymentService : IPaymentService
    {
        public Task<bool> PayAsync(Payment payment)
        {
            if (payment == null || string.IsNullOrWhiteSpace(payment.CardToken) || payment.Amount <= 0)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }
    }
}
