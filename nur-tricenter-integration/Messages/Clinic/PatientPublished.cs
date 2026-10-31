using Nur.Tricenter.Integration;

namespace Nur.Tricenter.Integration.Messages.Clinic;

/// <summary>
/// Announces that a patient was registered or updated in the clinic. Published to
/// <c>paciente-published</c>.
/// </summary>
/// <remarks>
/// Published by MS1 (clinical nutrition management). MS3 consumes it from
/// <c>ms3.paciente-published</c> to keep a local projection of the patients it needs for contract
/// checks and package labels. The message is a full snapshot: consumers upsert by
/// <see cref="PatientId"/>.
/// </remarks>
public record PatientPublished : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public PatientPublished()
    {
        DocumentNumber = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public PatientPublished(string? correlationId, string? source)
        : base(correlationId, source)
    {
        DocumentNumber = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
    }

    /// <summary>Stable discriminator on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form.</summary>
    public string EventType => "Patient.Published";

    public Guid PatientId { get; init; }

    public string DocumentNumber { get; init; }

    public string FirstName { get; init; }

    public string LastName { get; init; }

    /// <summary>Contact phone of the patient. <c>null</c> when the clinic has none.</summary>
    public string? Phone { get; init; }
}
