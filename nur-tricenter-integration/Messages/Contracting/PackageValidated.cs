using Nur.Tricenter.Integration;

namespace Nur.Tricenter.Integration.Messages.Contracting;

/// <summary>
/// Announces that a package passed quality control and is released to logistics.
/// </summary>
/// <remarks>
/// Published by MS3 (contracting &amp; production) to the <c>package-validated</c> fanout exchange, one
/// message per approved package. The logistics service (MS5) consumes it from
/// <c>ms5.package-validated</c> to assign the package to a delivery route.
/// <para>
/// <see cref="DeliveryAddress"/> and <see cref="TrackingNumber"/> come from the package label.
/// <see cref="Latitude"/> and <see cref="Longitude"/> come from the patient's active delivery calendar
/// and are <c>null</c> when no calendar matches the label's address.
/// </para>
/// </remarks>
public record PackageValidated : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public PackageValidated()
    {
        DeliveryAddress = string.Empty;
        TrackingNumber = string.Empty;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public PackageValidated(string? correlationId, string? source)
        : base(correlationId, source)
    {
        DeliveryAddress = string.Empty;
        TrackingNumber = string.Empty;
    }

    /// <summary>Stable discriminator on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form.</summary>
    public string EventType => "Package.Validated";

    public Guid PackageId { get; init; }

    public Guid PatientId { get; init; }

    public Guid ProductionOrderId { get; init; }

    public Guid CatalogPlanId { get; init; }

    public DateTime ReleasedAt { get; init; }

    public string DeliveryAddress { get; init; }

    public string TrackingNumber { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}
