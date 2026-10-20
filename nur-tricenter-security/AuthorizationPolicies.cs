namespace Nur.Tricenter.Security;

/// <summary>
/// Policy names that are not permissions. A permission policy is named after the permission it
/// requires, so it needs no constant here; these two do.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Allows any caller, authenticated or not.</summary>
    public const string Anonymous = "tricenter:anonymous";
}