namespace Nur.Tricenter.Observability.Config;

/// <summary>
/// Where traces go. The reference architecture misspells this type as <c>JeagerSettings</c>;
/// here it is spelled correctly, and the JSON section it binds to is <see cref="SectionName"/>
/// so the configuration key is unchanged.
/// </summary>
public class JaegerSettings
{
    public const string SectionName = "JaegerSettings";

    /// <summary>Host running the collector that receives OTLP traffic. Empty means OTLP is off.</summary>
    public string Host { get; set; } = string.Empty;
}