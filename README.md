# Nur-Tricenter Platform Kernels

Four transversal libraries shared by every microservice on the platform. They ship as NuGet packages
through **GitHub Packages**, so a service consumes them with a `PackageReference` and never references a
sibling folder on disk. **A kernel may not know any service's domain** — that rule is enforced by tests.

## Packages

| Package | What it gives a service |
|---|---|
| `Nur.Tricenter.Access` | `ScopeAccessRule` (shape of one authorization rule: route, method, required scope) + platform permission names + `IdentityPermissions.Expand` (wildcard → concrete scopes). |
| `Nur.Tricenter.Integration` | `IntegrationMessageContract` (base for every cross-service message) + concrete contracts under `Messages/<Service>/` (Catalog: FoodPublished, FoodNutritionalInfoUpdated, RecipePublished, RecipeUpdated, MealPlanPublished; Identity: UserPublished, UserChanged). |
| `Nur.Tricenter.Observability` | `AddObservability`, `UseLogging`, and the `Store.*` meter collection rule. |
| `Nur.Tricenter.Security` | `AddSecurity`, `JwtOptions` — one authorization policy per permission. |

## The rule these libraries exist to enforce

**A kernel may not know any service's domain.** The moment a kernel mentions one service's concepts,
every other consumer inherits them as a dependency, and the next team has to fork the kernel.

So `Nur.Tricenter.Access` declares the *shape* of a permission rule and the platform permission names,
but no service-specific logic. `Nur.Tricenter.Integration` declares the *base* message and the concrete
contracts that two services agree on (a contract is by definition shared, so it lives here). Those
belong to the platform, not to a single service.

`KernelHasNoDomainKnowledgeTests` asserts this. A type or member whose name mentions a domain term
fails the build, so the rule cannot erode one pull request at a time.

## Consuming

```xml
<PackageReference Include="Nur.Tricenter.Access" Version="1.0.9" />
<PackageReference Include="Nur.Tricenter.Integration" Version="1.0.9" />
<PackageReference Include="Nur.Tricenter.Observability" Version="1.0.9" />
<PackageReference Include="Nur.Tricenter.Security" Version="1.0.9" />
```

Restore from the organization feed (see `NuGet.config`):

```bash
dotnet nuget add source https://nuget.pkg.github.com/Nur-tricenter/index.json \
  --name Nur-tricenter --username <github-user> --password <TOKEN>
```

The token needs `read:packages` to restore and `write:packages` to push. Never put it in `NuGet.config`
or in version control.

## How to modify the kernels when adding a new service

1. **New permission / scope**: add the name to `Nur.Tricenter.Access` (and to `IdentityPermissions` if
   identity must mint it). Bump the version (minor if additive).
2. **New integration contract**: add a record under `Messages/<Service>/` extending
   `IntegrationMessageContract`. Both publisher and consumer update their `PackageReference` to the new
   version — no duplicated contracts, no drift.
3. **New observability rule**: extend the meter list in `Nur.Tricenter.Observability` so the new meter
   is actually exported (a meter not on the list is counted in-process and silently never exported).
4. **Security change**: `AddSecurity` / `JwtOptions` live here; every validator depends on them, so a
   breaking change is a major bump.

## Building, testing and publishing

```bash
dotnet build nur-tricenter-shared-kernels.slnx
dotnet test  nur-tricenter-shared-kernels.slnx

dotnet pack nur-tricenter-shared-kernels.slnx -c Release -o ./artifacts
dotnet nuget push ./artifacts/*.nupkg --source Nur-tricenter --api-key $NUGET_AUTH_TOKEN
```

The version lives in `Directory.Build.props` (`<Version>`) and applies to all four packages at once, so
they can never ship mismatched versions of the same platform.

## Versioning (contract)

| Change | Version |
|---|---|
| Adding a member, new overload | minor |
| Removing or changing a signature | major |
| Anything internal | patch |

A major bump breaks every consumer at `dotnet restore`. That is the point: it makes the break visible
at build time instead of at run time.

## How a new microservice consumes and stays in sync

- Pin all four kernels to the **same** version in its `Directory.Packages.props`.
- After a kernel release, bump the pin and rebuild; `dotnet restore` fails loudly if a version is missing.
- Never vendor the kernels (no copy into the service). The whole point is that one published package
  serves every consumer.
- The feed credentials (`NUGET_USERNAME` / `NUGET_TOKEN`) are read from the environment, never from a
  tracked file. Both secret files must be UTF-8 without a BOM.

## Consumers today

- `ms2-plans-and-recipes-catalog` — Access, Integration (Catalog messages), Observability, Security.
- `nur-tricenter-identity` — Access, Integration (Identity messages), Observability, Security.

Gateway and orchestrator do not consume the kernels (gateway auth is deferred; orchestrator is compose
only).
