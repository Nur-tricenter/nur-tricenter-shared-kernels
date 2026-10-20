using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nur.Tricenter.Observability.Config;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Configuration;
using Serilog.Enrichers.Span;

namespace Nur.Tricenter.Observability.Tracing;

/// <summary>
/// One call every service makes to wire traces, metrics and logging identically. The reference
/// architecture factors this into a shared kernel so a new microservice inherits the meter
/// collection rule below instead of re-deriving it — and that rule is easy to get wrong.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// The meters this kernel collects. The last entry is a WILDCARD.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A meter whose name is not in this list is not collected, and the symptom is silent: the
    /// instrument is created and counted correctly in-process, it simply never reaches the
    /// exporter. Nothing throws, nothing logs a warning, and the metric is invisible downstream.
    /// That is a collection problem, not an emission problem.
    /// </para>
    /// <para>
    /// The three concrete entries cover the libraries this platform uses (the RabbitMQ client and
    /// the transactional outbox publish their own meters, and Npgsql emits its own). <c>Store.*</c>
    /// is the platform's own prefix: any meter named <c>Store.Something</c> is collected, which is
    /// why a business meter must be named with that prefix to be visible at all.
    /// </para>
    /// </remarks>
    public static readonly string[] Meters =
    [
        "Joseco.Communication.RabbitMQ",
        "Joseco.Outbox",
        "Npgsql",
        "Store.*"
    ];

    /// <summary>
    /// Registers tracing and metrics for <paramref name="serviceName"/>.
    /// </summary>
    /// <param name="shouldIncludeHttpInstrumentation">
    /// Off for the worker: it serves no HTTP traffic, so instrumenting it adds noise, not signal.
    /// </param>
    /// <param name="instrumentationFactory">
    /// Escape hatch for a service that needs an instrumentation the kernel does not add itself.
    /// </param>
    /// <param name="metricsFactory">Escape hatch for extra meters or views.</param>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        string serviceName,
        JaegerSettings? jaegerSettings = null,
        Action<TracerProviderBuilder>? instrumentationFactory = null,
        bool shouldIncludeHttpInstrumentation = true,
        Action<MeterProviderBuilder>? metricsFactory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(serviceName)
                    .AddOtlpExporter();

                if (shouldIncludeHttpInstrumentation)
                {
                    tracing.AddAspNetCoreInstrumentation()
                           .AddHttpClientInstrumentation();
                }

                instrumentationFactory?.Invoke(tracing);
            })
            .WithMetrics(metrics =>
            {
                foreach (var meter in Meters)
                {
                    metrics.AddMeter(meter);
                }

                metrics.AddOtlpExporter();

                metricsFactory?.Invoke(metrics);
            });

        return services;
    }

    /// <summary>
    /// Configures Serilog for every host: console output plus the enrichers that put TraceId,
    /// SpanId and the process identity on each entry.
    /// </summary>
    /// <remarks>
    /// Named <c>UseLogging</c> to match the reference kernel, where it is a peer of
    /// <c>AddObservability</c> — one call for telemetry, one for logging.
    /// </remarks>
    public static IHostBuilder UseLogging(
        this IHostBuilder hostBuilder,
        string serviceName,
        IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        return hostBuilder
            .UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithSpan()
                .Enrich.WithProperty("service.name", serviceName));
    }
}