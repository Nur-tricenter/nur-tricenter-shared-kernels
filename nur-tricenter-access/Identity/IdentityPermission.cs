namespace Nur.Tricenter.Access.Identity;

/// <summary>
/// The scopes the identity service recognises.
/// </summary>
/// <remarks>
/// Lives in the shared kernel rather than in the service so that a consumer can reason about what
/// identity is allowed to do without taking a dependency on the identity service. That is the
/// whole point of putting it here: identity grants these to a token, and the services that
/// enforce them are exactly the ones that must not reference identity.
/// </remarks>
public static class IdentityPermission
{
    public const string Login = "identity.user:login";
    public const string Read = "identity.user:read";
    public const string Create = "identity.user:create";
    public const string Edit = "identity.user:edit";
    public const string Manage = "identity.user:manage";

    public const string RoleRead = "identity.role:read";
    public const string RoleCreate = "identity.role:create";
    public const string RoleEdit = "identity.role:edit";
    public const string RoleManage = "identity.role:manage";

    /// <summary>
    /// The sentinel an administrator holds. Expanded by the identity service at login into
    /// <see cref="PermissionsList"/>, so a token never carries it literally: a downstream service
    /// that understood the wildcard would have to know what it expands to.
    /// </summary>
    public const string All = "identity:*";

    /// <summary>
    /// Every scope this service defines, so a policy cannot be registered without also being
    /// listed and a typo cannot hide a permission.
    /// </summary>
    public static IReadOnlyList<string> PermissionsList { get; } = new[]
    {
        Login, Read, Create, Edit, Manage,
        RoleRead, RoleCreate, RoleEdit, RoleManage
    };
}

/// <summary>
/// The identity service's routes and the scope each one requires.
/// </summary>
/// <remarks>
/// Beside <see cref="IdentityPermission"/> for the same reason as the catalog's: a permission and
/// the route it guards are one decision.
/// <para>
/// The two login-adjacent routes are marked <c>AllowAnonymous</c>, and that is a real decision
/// rather than an oversight — you cannot present a scope you are trying to obtain. User creation
/// is anonymous here too, matching the reference architecture, and it is the one entry in this
/// table worth a second look: an open registration endpoint is a product decision about who may
/// create an account, not a technical detail.
/// </para>
/// </remarks>
public static class IdentityRoutes
{
    private const string Base = "/api/identity";

    /// <summary>Every route identity exposes, with the scope a caller must present.</summary>
    public static IReadOnlyList<ScopeAccessRule> Routes { get; } = new[]
    {
        new ScopeAccessRule { RouteTemplate = $"{Base}/auth/login", HttpMethod = "POST", AllowAnonymous = true },
        new ScopeAccessRule { RouteTemplate = $"{Base}/users", HttpMethod = "POST", RequiredScope = IdentityPermission.Create },
        new ScopeAccessRule { RouteTemplate = $"{Base}/users", HttpMethod = "GET", RequiredScope = IdentityPermission.Read }
    };
}