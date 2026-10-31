using Nur.Tricenter.Integration;

namespace Nur.Tricenter.Integration.Messages.Logistics;

/// <summary>
/// Announces that a delivery was completed. Published to <c>delivery-completed</c>.
/// </summary>
public record DeliveryCompleted : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public DeliveryCompleted()
    {
        DeliveryId = Guid.Empty;
        RouteId = Guid.Empty;
        DeliveredAt = DateTime.MinValue;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public DeliveryCompleted(string? correlationId, string? source)
        : base(correlationId, source)
    {
        DeliveryId = Guid.Empty;
        RouteId = Guid.Empty;
        DeliveredAt = DateTime.MinValue;
    }

    /// <summary>
    /// Stable discriminator on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form every contract
    /// on the platform uses. Consumers switch on this, never on the CLR type.
    /// </summary>
    public string EventType => "Delivery.Completed";

    public Guid DeliveryId { get; init; }

    public Guid RouteId { get; init; }

    /// <summary>Moment the delivery was confirmed, in UTC.</summary>
    public DateTime DeliveredAt { get; init; }

    /// <summary>Reference to the proof-of-delivery photo. <c>null</c> on messages published before 1.5.0.</summary>
    public string? EvidencePhotoUrl { get; init; }

    /// <summary>Reference to the recipient's signature. <c>null</c> on messages published before 1.5.0.</summary>
    public string? DigitalSignature { get; init; }
}

/// <summary>
/// Announces that a delivery failed. Published to <c>delivery-failed</c>.
/// </summary>
public record DeliveryFailed : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public DeliveryFailed()
    {
        DeliveryId = Guid.Empty;
        RouteId = Guid.Empty;
        Reason = string.Empty;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public DeliveryFailed(string? correlationId, string? source)
        : base(correlationId, source)
    {
        DeliveryId = Guid.Empty;
        RouteId = Guid.Empty;
        Reason = string.Empty;
    }

    public string EventType => "Delivery.Failed";

    public Guid DeliveryId { get; init; }

    public Guid RouteId { get; init; }

    public string Reason { get; init; }
}
