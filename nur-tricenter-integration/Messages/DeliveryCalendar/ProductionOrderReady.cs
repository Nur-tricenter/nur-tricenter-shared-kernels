using Nur.Tricenter.Integration;

namespace Nur.Tricenter.Integration.Messages.DeliveryCalendar;

/// <summary>
/// Announces that the production order for a delivery date is ready. Published to
/// <c>production-order-ready</c>.
/// </summary>
/// <remarks>
/// Published by MS4 (delivery calendar) once it resolved the menus of the next day's delivery
/// days. MS3 consumes it from <c>ms3.production-order-ready</c> and replaces its local list of
/// scheduled deliveries for <see cref="DeliveryDate"/>, which feeds the daily production order,
/// the package labels and the validation. It is a full snapshot of the day.
/// </remarks>
public record ProductionOrderReady : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public ProductionOrderReady()
    {
        DeliveryDate = DateOnly.MinValue;
        ProductionDate = DateOnly.MinValue;
        Lines = [];
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public ProductionOrderReady(string? correlationId, string? source)
        : base(correlationId, source)
    {
        DeliveryDate = DateOnly.MinValue;
        ProductionDate = DateOnly.MinValue;
        Lines = [];
    }

    /// <summary>Stable discriminator on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form.</summary>
    public string EventType => "ProductionOrder.Ready";

    public int ProductionOrderId { get; init; }

    public DateOnly DeliveryDate { get; init; }

    public DateOnly ProductionDate { get; init; }

    public IReadOnlyList<ProductionOrderReadyLine> Lines { get; init; }
}

/// <summary>One delivery day of the order, with the menu the patient receives.</summary>
public record ProductionOrderReadyLine
{
    public ProductionOrderReadyLine()
    {
        Address = string.Empty;
        Menu = [];
    }

    /// <summary>MS4 delivery day id (<c>packId</c>).</summary>
    public int DeliveryDayId { get; init; }

    public Guid PatientId { get; init; }

    public Guid PlanId { get; init; }

    /// <summary>Delivery hour in <c>HH:mm</c>. <c>null</c> when the patient has not chosen one.</summary>
    public string? Hour { get; init; }

    public string Address { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public IReadOnlyList<ProductionOrderReadyMenuItem> Menu { get; init; }
}

/// <summary>One recipe of the day's menu.</summary>
public record ProductionOrderReadyMenuItem
{
    public ProductionOrderReadyMenuItem()
    {
        MealTime = string.Empty;
        RecipeName = string.Empty;
    }

    public string MealTime { get; init; }

    public int MealTimeOrder { get; init; }

    public Guid RecipeId { get; init; }

    public string RecipeName { get; init; }

    public int Rations { get; init; }
}
