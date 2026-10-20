namespace Nur.Tricenter.Observability.Config;

/// <summary>
/// Where traces go. The JSON section it binds to is <see cref="SectionName"/>, so the
/// configuration key is <c>JaegerSettings</c>.
/// </summary>
public class JaegerSettings
{
    public const string SectionName = "JaegerSettings";

    /// <summary>Host running the collector that receives OTLP traffic. Empty means OTLP is off.</summary>
    public string Host { get; set; } = string.Empty;
}