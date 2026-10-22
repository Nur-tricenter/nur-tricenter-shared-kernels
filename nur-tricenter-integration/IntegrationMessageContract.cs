using Joseco.Communication.External.Contracts.Message;

namespace Nur.Tricenter.Integration;

// Re-exported so a service references ONE kernel for its contracts instead of the vendor
// package directly. The base record itself belongs to Joseco: it carries no platform decision,
// it is the transport-agnostic shape every cross-service message shares.
//
// THE MESSAGES ARE HERE NOW, under Messages/<Service>/. They used to live in each publishing
// repository's own contracts project, which meant two copies of every consumed contract and no
// mechanism to keep them in step — the worst arrangement available. When the publisher added a
// field, the consumer's copy silently dropped it, with no error anywhere, because an unknown JSON
// member deserializes without complaint.
//
// Putting them here means publisher and consumer compile against the SAME type. Changing a
// contract is one commit in one repository and every consumer picks it up on its next restore.
//
// This is what the reference architecture does, and for messages specifically it is the right
// call: a contract is by definition something two services agree on, so it is not a service's
// private detail to be forked.

/// <summary>
/// Base type for every message this platform sends between microservices.
/// </summary>
/// <remarks>
/// It carries an id, a creation timestamp, a correlation id and a source, which is what lets a
/// trace cross a service boundary and still read as one operation. It has no transport logic:
/// publishing it is the publisher's job, not the contract's.
/// </remarks>
public abstract record IntegrationMessageContract : IntegrationMessage
{
    /// <summary>Creates the message with a fresh id and timestamp.</summary>
    protected IntegrationMessageContract()
    {
    }

    /// <summary>
    /// Creates the message and forwards trace metadata to the shared envelope.
    /// </summary>
    /// <remarks>
    /// Declared here rather than left to the vendor base alone because the vendor's version takes
    /// the two arguments on the base type, and a derived record cannot chain to a base constructor
    /// it does not re-expose. Every concrete contract in this kernel publishes through this one.
    /// </remarks>
    protected IntegrationMessageContract(string? correlationId, string? source)
        : base(correlationId, source)
    {
    }
}