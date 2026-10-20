using Joseco.Communication.External.Contracts.Message;

namespace Nur.Tricenter.Integration;

// Re-exported so a service references ONE kernel for its contracts instead of the vendor
// package directly. The base record itself belongs to Joseco: it carries no platform decision,
// it is the transport-agnostic shape every cross-service message shares.
//
// The messages themselves are NOT here. They belong to the service that publishes them — see
// ms2-plans-and-recipes-catalog/contracts. A kernel that carried FoodPublished would have to be
// forked by the next service to publish CateringOrderAccepted, and the two copies would drift.

/// <summary>
/// Base type for every message this platform sends between microservices.
/// </summary>
/// <remarks>
/// It carries an id, a creation timestamp, a correlation id and a source, which is what lets a
/// trace cross a service boundary and still read as one operation. It has no transport logic:
/// publishing it is the publisher's job, not the contract's.
/// </remarks>
public abstract record IntegrationMessageContract : IntegrationMessage;