# Nur-Tricenter Platform Kernels

Four transversal libraries shared by every microservice on the platform. They ship as NuGet
packages through **GitHub Packages**, so a service consumes them with a `PackageReference` and
never references a sibling folder on disk.

| Package | What it gives a service |
|---------|------------------------|
| `Nur.Tricenter.Access` | `ScopeAccessRule` — the shape of one authorization rule (route, method, required scope) |
| `Nur.Tricenter.Integration` | `IntegrationMessageContract` — the base every cross-service message extends |
| `Nur.Tricenter.Observability` | `AddObservability`, `UseLogging`, and the `Store.*` meter rule |
| `Nur.Tricenter.Security` | `AddSecurity`, `JwtOptions` — one authorization policy per permission |

## The rule these libraries exist to enforce

**A kernel may not know any service's domain.**

That is not a style preference. The moment a kernel mentions one service's concepts, every other
consumer inherits them as a dependency, and the next team to arrive has to fork the kernel to add
its own — at which point there are two copies and nobody upstream to reconcile them.

So `Nur.Tricenter.Access` declares the *shape* of a permission rule and nothing about which
permissions exist. `Nur.Tricenter.Integration` declares the *base* message and no concrete
messages. Those belong to the service that publishes or consumes them:

```
ms2-plans-and-recipes-catalog/contracts/   CatalogPermissions, CatalogRoutes, FoodPublished…
```

`KernelHasNoDomainKnowledgeTests` asserts this. A type or member whose name mentions a domain term
fails the build, so the rule cannot erode one pull request at a time.

## Consuming

```xml
<PackageReference Include="Nur.Tricenter.Access" Version="1.0.0" />
```

Restore from the organization feed (see `NuGet.config`):

```bash
dotnet nuget add source https://nuget.pkg.github.com/Nur-tricenter/index.json \
  --name Nur-tricenter --username <github-user> --password <TOKEN>
```

The token needs `read:packages` to restore and `write:packages` to push. Never put it in
`NuGet.config`.

## Building and testing

```bash
dotnet build nur-tricenter-shared-kernels.slnx
dotnet test  nur-tricenter-shared-kernels.slnx
```

## Publishing

```bash
dotnet pack nur-tricenter-shared-kernels.slnx -c Release -o ./artifacts
dotnet nuget push ./artifacts/*.nupkg --source Nur-tricenter --api-key $NUGET_AUTH_TOKEN
```

The version lives in `Directory.Build.props` and applies to all four packages at once, so they can
never ship mismatched versions of the same platform.

## Versioning

Semantic versioning, and it is a contract:

| Change | Version |
|--------|---------|
| Adding a member, new overload | minor |
| Removing or changing a signature | major |
| Anything internal | patch |

A major bump breaks every consumer at `dotnet restore`. That is the point: it makes the break
visible at build time instead of at run time.