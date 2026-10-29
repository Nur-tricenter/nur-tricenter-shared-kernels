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
/// <para>
/// <see cref="DeliveryDate"/> is the day the package must be delivered (the production date), so the
/// consumer assigns it to that day's route instead of guessing from <see cref="ReleasedAt"/>.
/// <see cref="PatientName"/> comes from the label so the consumer can show it to the courier without
/// calling the clinic service at read time.
/// </para>
/// <para>
/// <see cref="DeliveryDayId"/> is the MS4 delivery day (<c>packId</c>) of the matched calendar, and
/// is <c>null</c> like the coordinates when no calendar matches. The consumer forwards it in
/// <c>DeliveryAddedToRoute</c> so MS4 can link the delivery to that day.
/// </para>
/// </remarks>
public record PackageValidated : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public PackageValidated()
    {
        DeliveryAddress = string.Empty;
        TrackingNumber = string.Empty;
        PatientName = string.Empty;
        DeliveryDate = DateOnly.MinValue;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public PackageValidated(string? correlationId, string? source)
        : base(correlationId, source)
    {
        DeliveryAddress = string.Empty;
        TrackingNumber = string.Empty;
        PatientName = string.Empty;
        DeliveryDate = DateOnly.MinValue;
    }

    /// <summary>Stable discriminator on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form.</summary>
    public string EventType => "Package.Validated";

    public Guid PackageId { get; init; }

    public Guid PatientId { get; init; }

    public string PatientName { get; init; }

    public Guid ProductionOrderId { get; init; }

    public Guid CatalogPlanId { get; init; }

    public DateTime ReleasedAt { get; init; }

    public DateOnly DeliveryDate { get; init; }

    public string DeliveryAddress { get; init; }

    public string TrackingNumber { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public int? DeliveryDayId { get; init; }
}
