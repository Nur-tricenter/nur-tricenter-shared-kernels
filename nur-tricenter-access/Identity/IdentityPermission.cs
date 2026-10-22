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