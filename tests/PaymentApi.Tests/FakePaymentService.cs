using Contracts.Models;

namespace PaymentApi.Tests
{
    public class FakePaymentService : IPaymentService
    {
        public bool IsPaymentSucceed { get; set; }

        public Task<bool> PayAsync(Payment payment)
        {
            return Task.FromResult(IsPaymentSucceed);
        }
    }
}
