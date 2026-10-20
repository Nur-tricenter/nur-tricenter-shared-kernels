namespace Nur.Tricenter.Access;

/// <summary>
/// One authorization rule for one route: which scope a caller must carry to reach it.
/// A microservice declares its own permissions in a form the gateway can aggregate without knowing
/// anything about the domain.
/// </summary>
public class ScopeAccessRule
{
    /// <summary>Route template this rule guards, e.g. "/api/catalog/foods/{id}".</summary>
    public string RouteTemplate { get; set; } = default!;

    /// <summary>HTTP method the rule applies to. GET by default.</summary>
    public string HttpMethod { get; set; } = "GET";

    /// <summary>Scope the caller must present, e.g. "catalog.food:edit".</summary>
    public string RequiredScope { get; set; } = default!;

    /// <summary>
    /// True when the route is reachable without any scope. Anonymous routes still get declared
    /// so the gateway's route table is complete; the difference is that they carry no scope.
    /// </summary>
    public bool AllowAnonymous { get; set; } = false;
}