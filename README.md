# Nur-Tricenter Platform Kernels

Four transversal libraries shared by every microservice on the platform. They ship as NuGet packages through
**GitHub Packages**, so a service consumes them with a `PackageReference` and never references a sibling
folder on disk. **A kernel may not carry domain logic** — only the shared vocabulary (permission names,
route rules, message contracts, JWT wiring, observability). That rule is enforced by
`KernelHasNoDomainKnowledgeTests`.

## Packages

| Package | What it gives a service |
|---|---|
| `Nur.Tricenter.Access` | Permission names (per service + platform-wide), route rules (`ScopeAccessRule`), `PlatformPermissions.Expand` (wildcard → concrete scopes). |
| `Nur.Tricenter.Integration` | `IntegrationMessageContract` (base for every cross-service message) + concrete contracts under `Messages/<Service>/`. |
| `Nur.Tricenter.Observability` | `AddObservability`, `UseLogging`, Jaeger/OTel settings, and the `Store.*` meter collection rule. |
| `Nur.Tricenter.Security` | `AddSecurity` (JWT bearer + one policy per permission), `JwtOptions`, `AuthorizationPolicies`. |

## Nur.Tricenter.Access

### Permissions

- `IdentityPermission` (`identity.user:*`, `identity.role:*`) and `CatalogPermission` (`catalog.food:*`,
  `catalog.recipe:*`, `catalog.mealPlan:*`) declare the scopes each service recognises, plus a
  `PermissionsList` so a policy cannot be registered without also being listed.
- `Permission.PermissionsList` aggregates every service's permissions into one list (one `Concat` per
  service). The gateway enumerates it once at startup to register policies; identity uses it to expand the
  platform administrator's wildcard.
- `PlatformPermissions.All = "platform:*"` is the sentinel a platform administrator holds. Identity
  expands it at login into `Permission.PermissionsList`, so a token never carries the wildcard literally
  and downstream services never need to know how it expands.
- `ScopeAccessRule` describes one authorization rule for one route (template, method, required scope,
  `AllowAnonymous`). `CatalogRoutes` / `IdentityRoutes` declare each service's routes beside its
  permissions so the two cannot drift.

### Why permission *names* live in the kernel

A service owns the *meaning* of its permissions, but identity has to be able to grant them into a token,
and identity must never depend on a service. Reading the names from the kernel inverts the dependency:
both depend on the kernel, neither on each other. (Earlier versions of this kernel forbade even names;
that rule was narrowed: behaviour stays out, names come in.)

## Nur.Tricenter.Integration

- `IntegrationMessageContract : IntegrationMessage` (Joseco) — base for every cross-service message. Carries
  an id, creation timestamp, correlation id and source, so a trace crosses a service boundary and still
  reads as one operation. No transport logic.
- Concrete contracts live under `Messages/<Service>/`:
  - `Messages/Catalog/`: `FoodPublished`, `FoodNutritionalInfoUpdated`, `RecipePublished`, `RecipeUpdated`,
    `MealPlanPublished`.
  - `Messages/Identity/`: `UserPublished`, `UserChanged`.
  - `Messages/Logistics/` (ms5, logistics and delivery): `CourierCreated`, `CourierStatusChanged`,
    `RouteCreated`, `CourierAssignedToRoute`, `DeliveryAddedToRoute`, `RouteStarted`,
    `RouteCompleted`, `RouteCancelled`, `DeliveryCompleted`, `DeliveryFailed`. Each one is published
    to its own fanout exchange named in kebab case (`courier-created`, `delivery-failed`, ...).
  - `Messages/Contracting/` (ms3, contracting and production): `PackageValidated` (`package-validated`),
    which carries the `DeliveryDate` and the label's `PatientName` since 1.3.0, and MS4's
    `DeliveryDayId` since 1.4.0 (forwarded by ms5 in `DeliveryAddedToRoute`) and the patient's
    `PatientPhone` since 1.5.0. `DeliveryCompleted` carries the `EvidencePhotoUrl` and
    `DigitalSignature` since 1.5.0 so ms4 can record the proof of delivery.
    MS3 talks to MS4 over REST (`POST /schedule`), so there is no contract for that hop.
- Publisher and consumer compile against the **same** type. Changing a contract is one commit in this
  repository and every consumer picks it up on its next restore. Duplicated contracts drift silently
  (a publisher adding a field would have the consumer's copy silently drop it, with no error anywhere).

## Nur.Tricenter.Security

- `JwtOptions` (`SectionName = "Jwt"`): `SecretKey`, `ValidIssuer`, `ValidAudience`, `ValidateIssuer`,
  `ValidateAudience`, `ValidateLifetime`, `Lifetime`. Every flag defaults to the strict choice except
  `Lifetime`, which has no safe default.
- `AddSecurity(jwtOptions, permissions)` registers the JWT bearer scheme and one named authorization
  policy per permission. Policies read the caller's scopes from `FindAll("scope")` (that literal name).
- `AuthorizationPolicies.Anonymous` is also registered.
- `MapInboundClaims` must be set to `false` by the host (see each service's `Startup`), otherwise JWT claim
  names are rewritten to WS-Federation URIs and every policy denies.

## Nur.Tricenter.Observability

- `AddObservability(serviceName, shouldIncludeHttpInstrumentation)` registers traces, metrics and the meter
  collection list. A meter not on the kernel's list is counted in-process and silently never exported.
- `UseLogging` (extension on `IHostBuilder`) configures Serilog with the ambient span stamped onto every
  event.
- `JaegerSettings` for the tracer endpoint.
- HTTP instrumentation is on by default for HTTP hosts and off for worker hosts (no HTTP traffic).

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

The token needs `read:packages` to restore and `write:packages` to push. Never put it in `NuGet.config` or
in version control. Both secret files must be UTF-8 without a BOM.

## How to modify the kernels when adding a new service

1. **New permission / scope**: add the constants to the new service's permission class (e.g.
   `CatalogPermission`), add its `PermissionsList` to `Permission.PermissionsList` (one `Concat`), and add
   its routes to `Routes.RouteList`. Bump the version (minor if additive).
2. **New integration contract**: add a record under `Messages/<Service>/` extending
   `IntegrationMessageContract`. Both publisher and consumer update their `PackageReference` to the new
   version.
3. **New observability rule**: extend the meter list in `Nur.Tricenter.Observability` so the new meter is
   actually exported.
4. **Security change**: `AddSecurity` / `JwtOptions` live here; every validator depends on them, so a
   breaking change is a major bump.

## Building, testing and publishing

```bash
dotnet build nur-tricenter-shared-kernels.slnx
dotnet test  nur-tricenter-shared-kernels.slnx

dotnet pack nur-tricenter-shared-kernels.slnx -c Release -o ./artifacts
dotnet nuget push ./artifacts/*.nupkg --source "https://nuget.pkg.github.com/Nur-tricenter/index.json" --api-key $NUGET_TOKEN
```

The version lives in `Directory.Build.props` (`<Version>`) and applies to all four packages at once, so they
can never ship mismatched versions. `artifacts/` is git-ignored.

## Versioning (contract)

| Change | Version |
|---|---|
| Adding a member, new overload | minor |
| Removing or changing a signature | major |
| Anything internal | patch |

A major bump breaks every consumer at `dotnet restore`. That is the point: it makes the break visible at
build time instead of at run time.

## How a new microservice consumes and stays in sync

- Pin all four kernels to the **same** version in its `Directory.Packages.props`.
- After a kernel release, bump the pin and rebuild; `dotnet restore` fails loudly if a version is missing.
- Never vendor the kernels (no copy into the service). One published package serves every consumer.
- If restore shows a stale version, clear the NuGet cache: `dotnet nuget locals all --clear`.
- The feed credentials (`NUGET_USERNAME` / `NUGET_TOKEN`) are read from the environment, never from a
  tracked file.

## Consumers today

- `ms2-plans-and-recipes-catalog` — Access, Integration (Catalog messages), Observability, Security.
- `nur-tricenter-identity` — Access, Integration (Identity messages), Observability, Security.
- `nurtricenter-logistics-and-delivery` (ms5) — Integration (Logistics messages), from 1.0.10.

Gateway and orchestrator do not consume the kernels (gateway auth is deferred; orchestrator is compose
only).

## Tests

`SharedKernels.Tests` asserts the kernel has no domain knowledge (`KernelHasNoDomainKnowledgeTests`): a type
or member whose name mentions a domain term fails the build. Run with `dotnet test`.
