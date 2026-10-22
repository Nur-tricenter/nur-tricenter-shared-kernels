namespace Nur.Tricenter.Access;

/// <summary>
/// Every permission and every route rule the platform recognises, gathered in one place.
/// </summary>
/// <remarks>
/// <para>
/// This aggregator is what lets a service grant another service's permissions without depending on
/// it. Identity, for instance, has to put catalog scopes into an administrator's token; if it read
/// them from the catalog it would depend on the catalog, and the catalog would then be unable to
/// change its own permission list without a matching release of identity. Reading them from the
/// kernel inverts that: both depend on the kernel, and neither on each other.
/// </para>
/// <para>
/// The gateway enumerates <see cref="Permission.PermissionsList"/> once at startup to register the
/// policies it enforces, so a scope added here becomes enforceable without touching the gateway.
/// </para>
/// <para>
/// <b>A departure from this repository's earlier rule.</b> A previous version of this file asserted
/// that a shared kernel must not mention any service's domain, and forbade exactly these types.
/// That rule was right for a kernel holding <em>behaviour</em> — a JWT validator has no business
/// knowing that a catalog exists. Permission NAMES are different: they are the vocabulary services
/// use to talk about authorization, and a service cannot grant a permission it cannot name. The rule
/// has been narrowed to what still matters, in
/// <c>KernelHasNoDomainKnowledgeTests</c>: the kernels carry no domain LOGIC, only the shared
/// vocabulary. Behaviour stays out; names come in.
/// </para>
/// </remarks>
public static class Permission
{
    /// <summary>
    /// Every scope on the platform. Concatenated per service so adding a service is one more
    /// <c>Concat</c>, not an edit to a list someone has to remember.
    /// </summary>
    public static IReadOnlyList<string> PermissionsList { get; } = new[]
    {
        Identity.IdentityPermission.Login,
        Identity.IdentityPermission.Read,
        Identity.IdentityPermission.Create,
        Identity.IdentityPermission.Edit,
        Identity.IdentityPermission.Manage,
        Identity.IdentityPermission.RoleRead,
        Identity.IdentityPermission.RoleCreate,
        Identity.IdentityPermission.RoleEdit,
        Identity.IdentityPermission.RoleManage,
        Catalog.CatalogPermission.FoodRead,
        Catalog.CatalogPermission.FoodCreate,
        Catalog.CatalogPermission.FoodEdit,
        Catalog.CatalogPermission.FoodManage,
        Catalog.CatalogPermission.RecipeRead,
        Catalog.CatalogPermission.RecipeCreate,
        Catalog.CatalogPermission.RecipeEdit,
        Catalog.CatalogPermission.RecipeManage,
        Catalog.CatalogPermission.MealPlanRead,
        Catalog.CatalogPermission.MealPlanCreate,
        Catalog.CatalogPermission.MealPlanEdit,
        Catalog.CatalogPermission.MealPlanManage
    };
}

/// <summary>
/// Every route on the platform, with the scope each one requires.
/// </summary>
/// <remarks>
/// The route-side twin of <see cref="Permission"/>, collected per service the same way. This is
/// what the reference architecture's gateway reads to build its routing table.
/// <para>
/// Our gateway does not consume it yet — it discovers routes from Consul instead. So this is the
/// declaration of what each endpoint requires, kept beside the permissions it references so the
/// two cannot drift.
/// </para>
/// </remarks>
public static class Routes
{
    /// <summary>Every route across every service, in registry order.</summary>
    public static IReadOnlyList<ScopeAccessRule> RouteList { get; } = new[]
    {
        Catalog.CatalogRoutes.Routes,
        Identity.IdentityRoutes.Routes
    }.SelectMany(routes => routes).ToArray();
}

/// <summary>
/// Every scope that grants administrative authority over the whole platform.
/// </summary>
/// <remarks>
/// Distinct from a service's own wildcard: <c>identity:*</c> means "every identity permission", and
/// this means "every permission on every service". A token issued to a platform administrator
/// carries the expanded list rather than this sentinel, so nothing downstream has to know how it
/// expands.
/// </remarks>
public static class PlatformPermissions
{
    /// <summary>
    /// The sentinel a platform administrator holds. Identity expands it at login into
    /// <see cref="Permission.PermissionsList"/>.
    /// </summary>
    public const string All = "platform:*";

    /// <summary>Every scope a platform administrator receives after expansion.</summary>
    public static IReadOnlyList<string> Expand(IEnumerable<string> heldScopes)
    {
        ArgumentNullException.ThrowIfNull(heldScopes);

        var scopes = heldScopes.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

        return scopes.Any(s => s is All)
            ? Permission.PermissionsList
            : scopes;
    }
}