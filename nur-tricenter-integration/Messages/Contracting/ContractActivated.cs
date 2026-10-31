using Nur.Tricenter.Integration;

namespace Nur.Tricenter.Integration.Messages.Contracting;

/// <summary>
/// Announces that a contract was paid and activated. Published to <c>contract-activated</c>.
/// </summary>
/// <remarks>
/// Published by MS3 (contracting &amp; production) when the payment is confirmed. MS4 (delivery
/// calendar) consumes it from <c>ms4.contract-activated</c> and creates the delivery calendar for
/// the contract. Consumers must be idempotent by <see cref="ContractId"/>: the broker delivers at
/// least once.
/// </remarks>
public record ContractActivated : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public ContractActivated()
    {
        StartDate = DateOnly.MinValue;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public ContractActivated(string? correlationId, string? source)
        : base(correlationId, source)
    {
        StartDate = DateOnly.MinValue;
    }

    /// <summary>Stable discriminator on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form.</summary>
    public string EventType => "Contract.Activated";

    public Guid ContractId { get; init; }

    public Guid PatientId { get; init; }

    public Guid CatalogPlanId { get; init; }

    /// <summary>Number of delivery days the contract covers (15 or 30).</summary>
    public int DurationDays { get; init; }

    /// <summary>First delivery date of the contract.</summary>
    public DateOnly StartDate { get; init; }
}
