namespace Contracts
{
    public interface IIntegrationEvent
    {
        Guid EventId { get; }
        Guid SagaId { get; }
        DateTime CreatedAtUtc { get; }
    }
}
