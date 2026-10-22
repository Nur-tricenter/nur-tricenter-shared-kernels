using Nur.Tricenter.Integration;

namespace Nur.Tricenter.Integration.Messages.Identity;

/// <summary>
/// Announces that an account was created in the identity service.
/// </summary>
/// <remarks>
/// Lives in the shared kernel rather than in the publishing service, so the consumer compiles
/// against the same type the publisher does. Duplicated contracts drift: the publisher adds a
/// field, the consumer's copy is untouched, and the field is silently lost on the way through —
/// no error anywhere, because an unknown JSON member deserializes without complaint.
/// <para>
/// Carries no credential of any kind: no password hash, no token, nothing secret. This record
/// leaves the service and lands in a broker, so anything on it must be safe to log.
/// </para>
/// </remarks>
public record UserPublished : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public UserPublished()
    {
        UserId = Guid.Empty;
        UserName = string.Empty;
        Email = string.Empty;
        Scopes = Array.Empty<string>();
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public UserPublished(string? correlationId, string? source)
        : base(correlationId, source)
    {
        UserId = Guid.Empty;
        UserName = string.Empty;
        Email = string.Empty;
        Scopes = Array.Empty<string>();
    }

    /// <summary>
    /// Stable discriminator on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form every contract
    /// on the platform uses. Consumers switch on this, never on the CLR type.
    /// </summary>
    public string EventType => "User.Published";

    public Guid UserId { get; init; }

    public string UserName { get; init; }

    public string Email { get; init; }

    public bool IsAdministrator { get; init; }

    /// <summary>Concrete scopes the account holds. Never the wildcard sentinel.</summary>
    public string[] Scopes { get; init; }
}

/// <summary>
/// Announces that an account changed.
/// </summary>
/// <remarks>
/// Separate from <see cref="UserPublished"/> on purpose: creation and mutation are different facts
/// for a consumer. A projection that upserts on one contract would rewrite every author's row
/// whenever a profile changed, and one that handles only the other would never learn that a user
/// exists.
/// </remarks>
public record UserChanged : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public UserChanged()
    {
        UserId = Guid.Empty;
        UserName = string.Empty;
        Email = string.Empty;
        Scopes = Array.Empty<string>();
        IsActive = false;
        ChangeKind = string.Empty;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public UserChanged(string? correlationId, string? source)
        : base(correlationId, source)
    {
        UserId = Guid.Empty;
        UserName = string.Empty;
        Email = string.Empty;
        Scopes = Array.Empty<string>();
        IsActive = false;
        ChangeKind = string.Empty;
    }

    public string EventType => "User.Changed";

    public Guid UserId { get; init; }

    public string UserName { get; init; }

    public string Email { get; init; }

    /// <summary>The scopes the account holds after the change, already expanded.</summary>
    public string[] Scopes { get; init; }

    public bool IsActive { get; init; }

    /// <summary>What changed, so a consumer can act without diffing the whole record.</summary>
    public string ChangeKind { get; init; }
}