namespace Nur.Tricenter.Access.DeliveryCalendar;

/// <summary>
/// The scopes the delivery calendar service (ms4) recognises.
/// </summary>
/// <remarks>
/// Declared in the kernel for the same reason as <c>CatalogPermission</c>: the delivery calendar
/// owns the MEANING of these scopes, but identity has to be able to grant them into a token without
/// depending on ms4.
/// <para>
/// Scopes are split by caller rather than by verb alone, because ms4 has three very different
/// clients: ms3 creates calendars and reads the next day's production order, the patient frontend
/// edits its own days, and ms5 closes each day with a delivery result. A token for one of them must
/// not carry the others' authority.
/// </para>
/// </remarks>
public static class DeliveryCalendarPermission
{
    public const string CalendarRead = "deliveryCalendar.calendar:read";
    public const string CalendarCreate = "deliveryCalendar.calendar:create";
    public const string CalendarEdit = "deliveryCalendar.calendar:edit";

    public const string ProductionOrderRead = "deliveryCalendar.productionOrder:read";

    public const string DeliveryResultEdit = "deliveryCalendar.deliveryResult:edit";

    public const string CatalogResolutionManage = "deliveryCalendar.catalogResolution:manage";

    /// <summary>
    /// Every scope this service defines. Registered by ms4 to build one authorization policy per
    /// entry, and read by identity when granting a platform administrator the full set.
    /// </summary>
    public static IReadOnlyList<string> PermissionsList { get; } = new[]
    {
        CalendarRead, CalendarCreate, CalendarEdit,
        ProductionOrderRead,
        DeliveryResultEdit,
        CatalogResolutionManage
    };
}

/// <summary>
/// The delivery calendar's routes and the scope each one requires.
/// </summary>
/// <remarks>
/// Same status as <c>CatalogRoutes</c>: today nothing reads this at runtime. Authorization is
/// enforced inside ms4 by each endpoint's policy; this table is the declaration of what each route
/// requires, kept beside the permissions so the two cannot drift.
/// </remarks>
public static class DeliveryCalendarRoutes
{
    private const string Base = "/api/delivery-calendar";

    /// <summary>
    /// Every route the delivery calendar exposes, with the scope a caller must present.
    /// </summary>
    public static IReadOnlyList<ScopeAccessRule> Routes { get; } = new[]
    {
        // ms3
        new ScopeAccessRule { RouteTemplate = $"{Base}/v1/schedule", HttpMethod = "POST", RequiredScope = DeliveryCalendarPermission.CalendarCreate },
        new ScopeAccessRule { RouteTemplate = $"{Base}/v1/calendars/active-tomorrow", HttpMethod = "GET", RequiredScope = DeliveryCalendarPermission.ProductionOrderRead },

        // Patient frontend
        new ScopeAccessRule { RouteTemplate = $"{Base}/v1/calendars/{{id}}", HttpMethod = "GET", RequiredScope = DeliveryCalendarPermission.CalendarRead },
        new ScopeAccessRule { RouteTemplate = $"{Base}/v1/calendars/{{id}}/dates", HttpMethod = "PUT", RequiredScope = DeliveryCalendarPermission.CalendarEdit },
        new ScopeAccessRule { RouteTemplate = $"{Base}/v1/calendars/{{id}}/addresses", HttpMethod = "POST", RequiredScope = DeliveryCalendarPermission.CalendarEdit },
        new ScopeAccessRule { RouteTemplate = $"{Base}/v1/calendars/{{id}}/cancel-day", HttpMethod = "POST", RequiredScope = DeliveryCalendarPermission.CalendarEdit },

        // ms5
        new ScopeAccessRule { RouteTemplate = $"{Base}/v1/calendars/days/{{dayId}}/delivery-result", HttpMethod = "PATCH", RequiredScope = DeliveryCalendarPermission.DeliveryResultEdit },

        // Operations
        new ScopeAccessRule { RouteTemplate = $"{Base}/v1/calendars/resolve-daily-catalog", HttpMethod = "POST", RequiredScope = DeliveryCalendarPermission.CatalogResolutionManage }
    };
}
