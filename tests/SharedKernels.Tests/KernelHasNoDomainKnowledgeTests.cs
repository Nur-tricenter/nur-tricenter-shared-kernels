using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nur.Tricenter.Observability.Tracing;
using Xunit;

namespace Nur.Tricenter.SharedKernels.Tests;

/// <summary>
/// The single rule that keeps these libraries shared rather than forked.
/// </summary>
/// <remarks>
/// <para>
/// A shared kernel earns its name only if a service that knows nothing about it can consume it.
/// The moment a kernel mentions one service's domain, that service's concepts become a dependency
/// every other consumer inherits, and the next team to arrive has to fork the kernel to add its
/// own — at which point there are two copies and no one upstream to reconcile them.
/// </para>
/// <para>
/// Nothing here enforces that by convention, so it is asserted instead. The forbidden terms are
/// checked as substrings against every public type and member name, which catches a leak whether
/// it arrives as a type, a property or a string constant.
/// </para>
/// </remarks>
public class KernelHasNoDomainKnowledgeTests
{
    /// <summary>Domain terms that must never appear in a shared kernel.</summary>
    private static readonly string[] ForbiddenTerms =
    [
        "Food", "Recipe", "MealPlan", "MealTime", "PlanDay", "Ingredient",
        "Catalog", "Catering", "Delivery", "Production", "Nutrition", "Portion"
    ];

    private static IEnumerable<Assembly> KernelAssemblies()
    {
        yield return typeof(Nur.Tricenter.Access.ScopeAccessRule).Assembly;
        yield return typeof(Nur.Tricenter.Integration.IntegrationMessageContract).Assembly;
        yield return typeof(Nur.Tricenter.Observability.Tracing.ObservabilityExtensions).Assembly;
        yield return typeof(Nur.Tricenter.Security.JwtOptions).Assembly;
    }

    [Fact]
    public void NoKernelTypeOrMemberMentionsTheCatalogDomain()
    {
        // Arrange
        var leaks = new List<string>();

        // Act
        foreach (var assembly in KernelAssemblies())
        {
            foreach (var type in assembly.GetExportedTypes())
            {
                leaks.AddRange(Inspect(type.Name, assembly.GetName().Name!));

                foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    leaks.AddRange(Inspect($"{type.Name}.{member.Name}", assembly.GetName().Name!));
                }
            }
        }

        // Assert
        Assert.True(
            leaks.Count == 0,
            "The shared kernel must not know any service's domain. Found: " +
            string.Join("; ", leaks) +
            ". Move this to the service's own contracts project: a kernel that mentions a domain " +
            "has to be forked by the next service, and the copies diverge.");
    }

    [Fact]
    public void AccessKernelKnowsNoServiceSpecificPermissions()
    {
        // The kernel declares the SHAPE of a permission rule. The permissions themselves are the
        // service's, so a catalog scope named here would become every service's concern.
        var assembly = typeof(Nur.Tricenter.Access.ScopeAccessRule).Assembly;

        var leakedScopes = assembly.GetExportedTypes()
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(field => field.Name)
            .Where(name => ForbiddenTerms.Any(term =>
                name.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.True(
            leakedScopes.Count == 0,
            "The access kernel must not declare service-specific scopes: " +
            string.Join(", ", leakedScopes));
    }

    [Fact]
    public void IntegrationKernelDeclaresNoConcreteMessages()
    {
        // A concrete message is a contract between two services. Only the service that publishes
        // it may own it, or every consumer of the kernel depends on messages it cannot use.
        var assembly = typeof(Nur.Tricenter.Integration.IntegrationMessageContract).Assembly;

        var concrete = assembly.GetExportedTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .Where(type => type.Name != "IntegrationMessageContract")
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            concrete.Count == 0,
            "The integration kernel must declare only the base contract, found: " +
            string.Join(", ", concrete) +
            ". Concrete messages belong to the service that publishes them.");
    }

    [Fact]
    public void ObservabilityCollectsTheStorePrefix()
    {
        // The sharpest rule in this kernel: a meter outside the collected list is not
        // reported as broken, it is simply never exported. The wildcard is what makes a business
        // meter visible, so losing it is a silent failure, not a compile error.
        var meters = Nur.Tricenter.Observability.Tracing.ObservabilityExtensions.Meters;

        Assert.Contains("Store.*", meters);
        Assert.Contains("Npgsql", meters);
        Assert.Contains("Joseco.Outbox", meters);
        Assert.Contains("Joseco.Communication.RabbitMQ", meters);
    }

    [Fact]
    public void AddObservabilityRefusesAnEmptyServiceName()
    {
        // The service name becomes the OpenTelemetry resource identity and the meter source. An
        // empty one produces telemetry that no backend can attribute to anything.
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddObservability(string.Empty));
        Assert.Throws<ArgumentException>(() => services.AddObservability("   "));
    }

    private static IEnumerable<string> Inspect(string candidate, string assemblyName)
    {
        return ForbiddenTerms
            .Where(term => candidate.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Select(term => $"{assemblyName}: {candidate} mentions '{term}'");
    }
}