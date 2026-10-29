using Nur.Tricenter.Integration;

namespace Nur.Tricenter.Integration.Messages.Logistics;

/// <summary>
/// Announces that a route was scheduled for a courier. Published to <c>route-created</c>.
/// </summary>
public record RouteCreated : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public RouteCreated()
    {
        RouteId = Guid.Empty;
        CourierId = Guid.Empty;
        ScheduledDate = DateOnly.MinValue;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public RouteCreated(string? correlationId, string? source)
        : base(correlationId, source)
    {
        RouteId = Guid.Empty;
        CourierId = Guid.Empty;
        ScheduledDate = DateOnly.MinValue;
    }

    /// <summary>
    /// Stable discriminator on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form every contract
    /// on the platform uses. Consumers switch on this, never on the CLR type.
    /// </summary>
    public string EventType => "Route.Created";

    public Guid RouteId { get; init; }

    public Guid CourierId { get; init; }

    /// <summary>Day the route is scheduled for. Serialized as an ISO date (yyyy-MM-dd).</summary>
    public DateOnly ScheduledDate { get; init; }
}

/// <summary>
/// Announces that a courier was assigned to a route. Published to <c>courier-assigned-to-route</c>.
/// </summary>
public record CourierAssignedToRoute : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public CourierAssignedToRoute()
    {
        RouteId = Guid.Empty;
        CourierId = Guid.Empty;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public CourierAssignedToRoute(string? correlationId, string? source)
        : base(correlationId, source)
    {
        RouteId = Guid.Empty;
        CourierId = Guid.Empty;
    }

    public string EventType => "Route.CourierAssigned";

    public Guid RouteId { get; init; }

    public Guid CourierId { get; init; }
}

/// <summary>
/// Announces that a delivery was added to a route. Published to <c>delivery-added-to-route</c>.
/// </summary>
/// <remarks>
/// <see cref="DeliveryDayId"/> is the MS4 delivery day the package was produced for (from
/// <c>PackageValidated</c>); <c>null</c> when the package matched no calendar or was loaded by hand.
/// </remarks>
public record DeliveryAddedToRoute : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public DeliveryAddedToRoute()
    {
        RouteId = Guid.Empty;
        DeliveryId = Guid.Empty;
        PackageId = string.Empty;
        PatientId = string.Empty;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public DeliveryAddedToRoute(string? correlationId, string? source)
        : base(correlationId, source)
    {
        RouteId = Guid.Empty;
        DeliveryId = Guid.Empty;
        PackageId = string.Empty;
        PatientId = string.Empty;
    }

    public string EventType => "Route.DeliveryAdded";

    public Guid RouteId { get; init; }

    public Guid DeliveryId { get; init; }

    public string PackageId { get; init; }

    public string PatientId { get; init; }

    public int? DeliveryDayId { get; init; }
}

/// <summary>
/// Announces that a courier started a route. Published to <c>route-started</c>.
/// </summary>
public record RouteStarted : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public RouteStarted()
    {
        RouteId = Guid.Empty;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public RouteStarted(string? correlationId, string? source)
        : base(correlationId, source)
    {
        RouteId = Guid.Empty;
    }

    public string EventType => "Route.Started";

    public Guid RouteId { get; init; }
}

/// <summary>
/// Announces that every stop on a route reached a terminal state and the route was completed.
/// Published to <c>route-completed</c>.
/// </summary>
public record RouteCompleted : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public RouteCompleted()
    {
        RouteId = Guid.Empty;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public RouteCompleted(string? correlationId, string? source)
        : base(correlationId, source)
    {
        RouteId = Guid.Empty;
    }

    public string EventType => "Route.Completed";

    public Guid RouteId { get; init; }
}

/// <summary>
/// Announces that a route was cancelled. Published to <c>route-cancelled</c>.
/// </summary>
public record RouteCancelled : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public RouteCancelled()
    {
        RouteId = Guid.Empty;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public RouteCancelled(string? correlationId, string? source)
        : base(correlationId, source)
    {
        RouteId = Guid.Empty;
    }

    public string EventType => "Route.Cancelled";

    public Guid RouteId { get; init; }
}
