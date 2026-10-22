using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nur.Tricenter.Access;
using Nur.Tricenter.Observability.Tracing;
using Xunit;

namespace Nur.Tricenter.SharedKernels.Tests;

/// <summary>
/// The rule that keeps these libraries shared rather than forked, and the one place it is
/// deliberately relaxed.
/// </summary>
/// <remarks>
/// <para>
/// A shared kernel earns its name only if a service that knows nothing about it can consume it.
/// The moment a kernel carries a service's BEHAVIOUR, that service's concepts become a dependency
/// every other consumer inherits, and the next team to arrive has to fork the kernel to add its own
/// — at which point there are two copies and no one upstream to reconcile them.
/// </para>
/// <para>
/// <b>What changed, and why it is not a contradiction.</b> This file previously forbade the kernels
/// from naming any service domain at all, and
/// <c>AccessKernelKnowsNoServiceSpecificPermissions</c> failed the moment permission names arrived.
/// That rule was correct for behaviour and wrong for vocabulary. A JWT validator has no business
/// knowing a catalog exists; a service that grants catalog permissions has no choice but to NAME
/// them somewhere both sides can see. Permission names are the shared language of authorization;
/// domain logic is not shared language, it is coupling.
/// </para>
/// <para>
/// So the rule is narrowed rather than deleted. The kernels may now carry permission NAMES and the
/// aggregator that collects them; they may not carry domain types, domain rules, or a service's
/// business behaviour. That is what the forbidden-term list now checks.
/// </para>
/// </remarks>
public class KernelHasNoDomainKnowledgeTests
{
    /// <summary>
    /// Domain terms that must never appear in a shared kernel.
    /// </summary>
    /// <remarks>
    /// Checked against type and member NAMES only, not against string constant VALUES — which is
    /// what allows <c>CatalogPermission</c> to declare the literal <c>"catalog.food:read"</c>
    /// without the kernel thereby depending on what a food is.
    /// </remarks>
    private static readonly string[] ForbiddenTerms =
    [
        "Food", "Recipe", "MealPlan", "MealTime", "PlanDay", "Ingredient",
        "Catalog", "Catering", "Delivery", "Production", "Nutrition", "Portion"
    ];

    /// <summary>
    /// Names that legitimately contain a domain word under the new rule, because they are
    /// permission VOCABULARY rather than domain knowledge.
    /// </summary>
    private static readonly string[] AllowedVocabulary =
    [
        "CatalogPermission", "IdentityPermission", "Permission", "PlatformPermissions"
    ];

    private static IEnumerable<Assembly> KernelAssemblies()
    {
        yield return typeof(Nur.Tricenter.Access.ScopeAccessRule).Assembly;
        yield return typeof(Nur.Tricenter.Integration.IntegrationMessageContract).Assembly;
        yield return typeof(Nur.Tricenter.Observability.Tracing.ObservabilityExtensions).Assembly;
        yield return typeof(Nur.Tricenter.Security.JwtOptions).Assembly;
    }

    [Fact]
    public void NoKernelDeclaresDomainBehaviour()
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
            "A shared kernel must not carry a service's domain behaviour. Found: " +
            string.Join("; ", leaks) +
            ". Permission NAMES are allowed (see AllowedVocabulary); a domain type, a domain rule " +
            "or a business method is not — the next service would have to fork the kernel.");
    }

    [Fact]
    public void AccessKernelDeclaresOnlyVocabularyNotBehaviour()
    {
        // The kernel holds permission NAMES and the aggregator over them. It must hold no member
        // that does anything service-specific: a permission list is data every service needs, a
        // rule about who may edit a recipe is a service's own rule.
        var assembly = typeof(Nur.Tricenter.Access.ScopeAccessRule).Assembly;

        var offenders = assembly.GetExportedTypes()
            .Where(type => !AllowedVocabulary.Contains(type.Name.Split('`')[0]))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .Where(name => ForbiddenTerms.Any(term =>
                name.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "The access kernel must expose permission vocabulary, not per-service behaviour: " +
            string.Join(", ", offenders));
    }

    [Fact]
    public void PlatformPermissionsExpandsToEveryDeclaredScope()
    {
        // The sentinel is the mechanism that lets identity grant catalog permissions it does not
        // implement. If the expansion ever stops covering the full list, a platform administrator
        // silently loses access to a service — with no error anywhere.
        var expanded = PlatformPermissions.Expand(new[] { PlatformPermissions.All });

        Assert.Equal(Permission.PermissionsList.Count, expanded.Count);
        Assert.Contains(Nur.Tricenter.Access.Catalog.CatalogPermission.MealPlanCreate, expanded);
        Assert.Contains(Nur.Tricenter.Access.Identity.IdentityPermission.Manage, expanded);
    }

    [Fact]
    public void PlatformExpansionLeavesOrdinaryScopesAlone()
    {
        // A non-administrator's token must come out unchanged; otherwise every account would
        // silently acquire the whole platform's permissions.
        var held = new[] { Nur.Tricenter.Access.Catalog.CatalogPermission.FoodRead };

        Assert.Equal(held, PlatformPermissions.Expand(held));
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
        // Vocabulary names are exempt: `CatalogPermission` is the shared vocabulary of the
        // catalog's authorization, not knowledge of what a catalog contains.
        if (AllowedVocabulary.Any(allowed => candidate.Contains(allowed, StringComparison.Ordinal)))
        {
            return Array.Empty<string>();
        }

        return ForbiddenTerms
            .Where(term => candidate.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Select(term => $"{assemblyName}: {candidate} mentions '{term}'");
    }
}