namespace Contracts
{
    public abstract record IntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();

        public Guid SagaId { get; init; }

        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

        public int RetryCount { get; set; }
    }
}
