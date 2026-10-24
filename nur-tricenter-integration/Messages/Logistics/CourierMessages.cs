using Nur.Tricenter.Integration;

namespace Nur.Tricenter.Integration.Messages.Logistics;

/// <summary>
/// Announces that a courier was registered in the logistics service (ms5).
/// </summary>
/// <remarks>
/// Published to the <c>courier-created</c> fanout exchange. Enum values travel as their member
/// names, so a consumer needs no knowledge of the publisher's enums.
/// </remarks>
public record CourierCreated : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public CourierCreated()
    {
        CourierId = Guid.Empty;
        FullName = string.Empty;
        Status = string.Empty;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public CourierCreated(string? correlationId, string? source)
        : base(correlationId, source)
    {
        CourierId = Guid.Empty;
        FullName = string.Empty;
        Status = string.Empty;
    }

    /// <summary>
    /// Stable discriminator on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form every contract
    /// on the platform uses. Consumers switch on this, never on the CLR type.
    /// </summary>
    public string EventType => "Courier.Created";

    public Guid CourierId { get; init; }

    public string FullName { get; init; }

    /// <summary>Courier status as the enum member name (<c>"Available"</c>, <c>"OnRoute"</c>, ...).</summary>
    public string Status { get; init; }
}

/// <summary>
/// Announces that a courier's status changed. Published to <c>courier-status-changed</c>.
/// </summary>
public record CourierStatusChanged : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public CourierStatusChanged()
    {
        CourierId = Guid.Empty;
        OldStatus = string.Empty;
        NewStatus = string.Empty;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public CourierStatusChanged(string? correlationId, string? source)
        : base(correlationId, source)
    {
        CourierId = Guid.Empty;
        OldStatus = string.Empty;
        NewStatus = string.Empty;
    }

    public string EventType => "Courier.StatusChanged";

    public Guid CourierId { get; init; }

    public string OldStatus { get; init; }

    public string NewStatus { get; init; }
}
