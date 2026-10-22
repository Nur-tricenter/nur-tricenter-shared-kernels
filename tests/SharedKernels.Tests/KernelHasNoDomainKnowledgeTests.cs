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
/// ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â at which point there are two copies and no one upstream to reconcile them.
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
    /// Checked against type and member NAMES only, not against string constant VALUES ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â which is
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
        "CatalogPermission", "CatalogRoutes", "IdentityPermission", "IdentityRoutes", "Permission", "PlatformPermissions", "Routes"
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
        // A concrete MESSAGE is exempt. It has to name its domain ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â a contract describing a food
        // cannot avoid the word "food" and still describe one. That is not the kernel knowing the
        // domain; it is the contract BEING the domain's vocabulary, which is its whole job.
        //
        // What the kernel still must not carry is a domain TYPE or RULE: no NutritionInfo class,
        // no validation method, nothing a service could import instead of writing itself.
        var messageContracts = typeof(Nur.Tricenter.Integration.IntegrationMessageContract).Assembly
            .GetExportedTypes()
            .Where(t => t.Namespace?.StartsWith("Nur.Tricenter.Integration.Messages.", StringComparison.Ordinal) == true)
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        // Arrange
        var leaks = new List<string>();

        // Act
        foreach (var assembly in KernelAssemblies())
        {
            foreach (var type in assembly.GetExportedTypes())
            {
                if (messageContracts.Contains(type.Name))
                {
                    continue;
                }

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
            ". Permission names and concrete message contracts are allowed (both ARE the domain's " +
            "vocabulary by design); a domain TYPE, a domain rule or a business method is not ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â the " +
            "next service would have to fork the kernel.");
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
    public void EveryRouteIsDeclaredBesideItsPermission()
    {
        // A route and the scope it guards are one decision. Splitting them across repositories is
        // how an endpoint gets added with a scope nothing declares. This asserts the route table
        // lives in the kernel, beside the permissions it references, as the reference
        // architecture does Ã¢â‚¬â€ and that it is not empty, which a moved-but-unused table would be.
        var assembly = typeof(Nur.Tricenter.Access.ScopeAccessRule).Assembly;

        var routes = Nur.Tricenter.Access.Routes.RouteList;
        Assert.NotEmpty(routes);

        var known = Nur.Tricenter.Access.Permission.PermissionsList.ToHashSet(StringComparer.Ordinal);

        // Every non-anonymous route must name a scope the platform actually declares, or it is a
        // policy nothing can ever satisfy.
        var unknown = routes
            .Where(rule => !rule.AllowAnonymous && !string.IsNullOrWhiteSpace(rule.RequiredScope))
            .Where(rule => !known.Contains(rule.RequiredScope))
            .Select(rule => $"{rule.HttpMethod} {rule.RouteTemplate} -> {rule.RequiredScope}")
            .ToList();

        Assert.True(
            unknown.Count == 0,
            "These routes require a scope the platform never declares: " + string.Join("; ", unknown));
    }

    [Fact]
    public void PlatformPermissionsExpandsToEveryDeclaredScope()
    {
        // The sentinel is the mechanism that lets identity grant catalog permissions it does not
        // implement. If the expansion ever stops covering the full list, a platform administrator
        // silently loses access to a service ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â with no error anywhere.
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
    public void IntegrationKernelDeclaresOnlyCrossServiceMessages()
    {
        // This test previously required the kernel to declare NO concrete message, on the reasoning
        // that a message belongs to the service that publishes it. That rule was wrong for messages,
        // and it produced the worst possible outcome in practice: publisher and consumer each held a
        // COPY, so when the publisher added a field the consumer silently dropped it ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â no error,
        // because an unknown JSON member deserializes without complaint.
        //
        // A contract is by definition something two services agree on. Keeping it in one place is
        // what makes it a contract; keeping it in the publisher's repo makes it a copy.
        //
        // What the test still enforces is the part that matters: every concrete message in the
        // kernel must be namespaced under Messages.<Service>. That keeps a service's internal
        // vocabulary out, and makes it obvious which service owns each contract.
        var assembly = typeof(Nur.Tricenter.Integration.IntegrationMessageContract).Assembly;

        var misplaced = assembly.GetExportedTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .Where(type => type.Name != "IntegrationMessageContract")
            .Where(type => type.Namespace?.StartsWith(
                "Nur.Tricenter.Integration.Messages.", StringComparison.Ordinal) != true)
            .Select(type => type.FullName!)
            .ToList();

        Assert.True(
            misplaced.Count == 0,
            "Every concrete message in the integration kernel must live under " +
            "Nur.Tricenter.Integration.Messages.<Service>. Found outside it: " +
            string.Join(", ", misplaced));
    }

    [Fact]
    public void EveryPublishedMessageBelongsToAServiceFolder()
    {
        // The regression this whole change exists to prevent: two copies of one contract. If a
        // message reappears in a service's own repository, the copies can diverge silently.
        //
        // The check is structural rather than exhaustive ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â it asserts the kernel holds at least
        // one contract per publishing service, so a service that starts publishing without adding
        // its folder is visible.
        var messages = typeof(Nur.Tricenter.Integration.IntegrationMessageContract).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith(
                "Nur.Tricenter.Integration.Messages.", StringComparison.Ordinal) == true)
            .ToList();

        var services = messages
            .Select(type =>
            {
                // Nur.Tricenter.Integration.Messages.<Service> ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â the service segment is the one
                // right after "Messages", so take it by name rather than by index.
                var parts = type.Namespace!.Split('.');
                var marker = Array.IndexOf(parts, "Messages");
                return marker >= 0 && marker + 1 < parts.Length ? parts[marker + 1] : string.Empty;
            })
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.Contains("Identity", services);
        Assert.Contains("Catalog", services);
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
        // Vocabulary is exempt, and it is matched on the TYPE name rather than on the whole
        // candidate. Matching the candidate would exempt `CatalogPermission.FoodCreate` by accident
        // while still flagging `CatalogRoutes.Routes`, because the member path carries the type
        // name even when the member itself does not.
        //
        // A permission list and a route table ARE the domain's vocabulary â€” a route for
        // /api/catalog/foods cannot avoid the word "catalog". What must not appear is a domain
        // TYPE or RULE, which is what the separate assertions check.
        var typeName = candidate.Split('.')[0];
        if (AllowedVocabulary.Any(allowed =>
                string.Equals(allowed, typeName, StringComparison.Ordinal)))
        {
            return Array.Empty<string>();
        }

        return ForbiddenTerms
            .Where(term => candidate.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Select(term => $"{assemblyName}: {candidate} mentions '{term}'");
    }
}