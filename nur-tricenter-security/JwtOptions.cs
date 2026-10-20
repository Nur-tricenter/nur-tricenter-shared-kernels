namespace Nur.Tricenter.Security;

/// <summary>
/// How a service validates a bearer token. Every flag defaults to the strict choice except
/// <see cref="Lifetime"/>, which has no safe default: a service that neither validates the
/// lifetime nor sets one accepts a token that never expires.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;

    public bool ValidateIssuer { get; set; } = true;
    public string ValidIssuer { get; set; } = string.Empty;

    public bool ValidateAudience { get; set; } = true;
    public string ValidAudience { get; set; } = string.Empty;

    public bool ValidateLifetime { get; set; } = true;

    /// <summary>Token lifetime in seconds. Only used when <see cref="ValidateLifetime"/> is true.</summary>
    public long Lifetime { get; set; }
}