# Architecture

Verified against `main` @ `edfd057` on 2026-07-26.

> This document is **prescriptive**. Where the target architecture differs from the current code, the gap is
> marked **⚠ Target** and linked to [roadmap.md](roadmap.md) or [known-issues.md](known-issues.md). Build
> towards the target; do not lower it to match what exists.

## Style

Clean Architecture / Onion, four projects, dependencies pointing inwards, plus a hand-rolled CQRS
dispatch layer. It is **not** vertical slices: folders are grouped by technical role
(`Commands/`, `Handlers/`, `Validators/`) with a per-aggregate subfolder (`Recipes/`).

```
RecipeManager.Api ──► RecipeManager.Application ──► RecipeManager.Domain
        │                                                  ▲
        └──────► RecipeManager.Infrastructure ─────────────┘
                              │
                              └──► RecipeManager.Application (for ICacheService)
```

Verified project references:

| Project | References |
| --- | --- |
| `RecipeManager.Domain` | *(none)* — packages only: `FluentResults`, `Ardalis.GuardClauses` |
| `RecipeManager.Application` | `Domain` |
| `RecipeManager.Infrastructure` | `Application`, `Domain` |
| `RecipeManager.Api` | `Application`, `Infrastructure` |
| `RecipeManager.UnitTests` | `Domain`, `Application` |
| `RecipeManager.IntegrationTests` | `Api` |

**Rule:** never add a reference that reverses an arrow. `Domain` must stay dependency-free at the project level.

## Layer responsibilities

### Domain (`RecipeManager.Domain`)

- `RecipeManager.Domain/Shared/Entity.cs` — abstract base with `Guid Id { get; protected init; }`.
  `Equals` compares **concrete type and id** (`GetType() == other.GetType() && Id == other.Id`);
  `GetHashCode` hashes **the id alone** (`Id.GetHashCode()`). That asymmetry is deliberate and contract-valid —
  equal objects always produce equal hashes — but it means two entities of *different* types sharing an id
  collide in a hash bucket while comparing unequal. Harmless with `Guid` keys; keep it in mind before adding an
  entity type whose ids are drawn from the same sequence as another's.
- `RecipeManager.Domain/Entities/Recipe.cs` — the only aggregate. Private setters, private constructors, static factory
  `Create(...)` returning `Result<Recipe>`, instance `Update(...)` returning `Result`. All invariants live in
  the private `ValidateProperties`.
- `RecipeManager.Domain/Errors/RecipeErrors.cs` — every domain error as a static factory returning a `FluentResults.Error` carrying
  `ErrorCode` (HTTP status) and `field` metadata.
- `RecipeManager.Domain/Interfaces/Repositories/IRecipeRepository.cs` — the persistence port. **Repository interfaces live in
  Domain, implementations in Infrastructure.**

### Application (`RecipeManager.Application`)

- `RecipeManager.Application/Common/Interfaces/Messaging/` — `ICommand<TResult>`, `IQuery<TResult>` (empty marker interfaces),
  `ICommandHandler<,>`, `IQueryHandler<,>`, `ICommandDispatcher`, `IQueryDispatcher`.
- `RecipeManager.Application/Dispatchers/` — `CommandDispatcher` / `QueryDispatcher` resolve the handler from `IServiceProvider` via
  `GetRequiredService` and call `Handle`. No pipeline behaviours, no decorators. Handlers are discovered by
  Scrutor assembly scanning, not listed by hand (ADR-008).
- `RecipeManager.Application/Commands/`, `.../Queries/` — positional `record` types implementing the marker interfaces.
- `RecipeManager.Application/Handlers/Recipes/` — one class per command/query; constructor-injected `IRecipeRepository` (+ `ILogger`
  where used).
- `RecipeManager.Application/DTO/Recipes/` — `RecipeDto`, `UpdateRecipeDto` (records).
- `RecipeManager.Application/Mappings/RecipeMappingExtensions.cs` — hand-written `MapToRecipeDto()` extension. **No AutoMapper.**
- `RecipeManager.Application/Validators/Recipes/` — FluentValidation validators over the *bound request type*, plus shared rule
  extensions in `RecipeValidationRules`.
- `RecipeManager.Application/Common/Interfaces/Caching/ICacheService.cs` — the caching port (implementation lives in Infrastructure).

### Infrastructure (`RecipeManager.Infrastructure`)

- `RecipeManager.Infrastructure/Context/AppDbContext.cs` — a single `DbSet<Recipe>`; **no `OnModelCreating`, no `IEntityTypeConfiguration`**.
  EF Core 10 + Npgsql map `IReadOnlyList<string>` to a native PostgreSQL `text[]` column.
- `RecipeManager.Infrastructure/Repositories/Recipes/RecipeRepository.cs` — EF implementation. Reads use `AsNoTracking()`; every write calls
  `SaveChangesAsync` immediately (no unit-of-work abstraction).
- `RecipeManager.Infrastructure/Repositories/Recipes/CachedRecipeRepository.cs` — decorator implementing the same interface.
- `RecipeManager.Infrastructure/Services/MemoryCacheService.cs` — `IMemoryCache` adapter, plus a `ConcurrentDictionary` key registry.
- `RecipeManager.Infrastructure/Constants/CacheKeys.cs`, `RecipeManager.Infrastructure/Constants/CacheDuration.cs`.
- `RecipeManager.Infrastructure/Migrations/` — one migration, `20260725173218_InitialCreate`.

### Api (`RecipeManager.Api`)

- `RecipeManager.Api/Controllers/RecipesController.cs` — the only controller. Builds a command/query, dispatches it, converts the
  `Result` with `ToActionResult()` / `ToCreatedAtActionResult()`.
- `RecipeManager.Api/Startup/ServiceInitializer.cs` — all DI registration, as chained `IServiceCollection` extensions.
- `RecipeManager.Api/Startup/ApplicationInitializer.cs` — Swagger setup, pipeline order, startup migration.
- `RecipeManager.Api/Startup/CustomObjects/DatabaseConnectionConfiguration.cs` — bound from the `ConnectionStrings` section.
- `RecipeManager.Api/Extensions/ResultExtensions.cs` — `Result` → `ActionResult` + `ProblemDetails`.
- `RecipeManager.Api/Middlewares/ErrorHandlerMiddleware.cs` — last-resort exception → `ProblemDetails`.

## Request flow (`PUT /api/recipes/{id}`)

1. ASP.NET binds `UpdateRecipeDto`; `AddFluentValidationAutoValidation` runs `UpdateRecipeDtoValidator`.
   Shape failure → **400** with the framework's `ValidationProblemDetails`; the controller never runs.
2. `RecipesController.Update` maps the DTO + route id into `UpdateRecipeCommand`.
3. `ICommandDispatcher.Dispatch<UpdateRecipeCommand, Result>` resolves `UpdateRecipeHandler`.
4. Handler loads via `IRecipeRepository.GetByIdAsync` — the **`CachedRecipeRepository` decorator** answers first,
   falling through to `RecipeRepository`. Missing → `RecipeErrors.RecipeNotFound` (**404**).
5. `recipe.Update(...)` enforces domain invariants → failure returns `RecipeErrors.*` (**422**).
6. `UpdateAsync` persists, then invalidates `recipes_all` and `recipe_{id}`.
7. `result.ToActionResult()` → **204 No Content** on success, `ProblemDetails` otherwise.

## Cross-cutting concerns

### Error handling — three distinct channels

| Channel | Triggered by | Status | Body |
| --- | --- | --- | --- |
| FluentValidation auto-validation | null / length / range on the bound type | 400 | `ValidationProblemDetails` |
| `Result` + `RecipeErrors` → `ResultExtensions` | domain invariants, not-found | 422 / 404 (from `ErrorCode` metadata, default 400) — **⚠ Target:** from a semantic error kind, ADR-009 | `ProblemDetails` + `field`, plus an `errors[]` extension when more than one error |
| `ErrorHandlerMiddleware` | unhandled exception | 404 / 400 / 401 / 500 by exception type | `ProblemDetails` |

`ResultExtensions.CreateProblemDetails` derives the status from **`errors.First()`** only; remaining errors are
appended under the `errors` extension key. Until ADR-009 lands this is a real defect, not just a quirk — a
`Result` mixing a 404 and a 422 returns an arbitrary status. Order the errors deliberately when returning
several.

### Persistence

- PostgreSQL via `options.UseNpgsql(...)` in `ServiceInitializer.RegisterDbContext`.
- Column types: `Id uuid` (PK), `Title`/`Description` `text`, times and `Servings` `integer`,
  `Ingredients`/`Instructions` **`text[]`** (native PostgreSQL arrays, not JSON).
- Ids are generated in the entity constructor with `Guid.NewGuid()`, not by the database.
- Migrations run at startup via `app.MigrateDatabase()`.

### Caching

- Registered as `services.AddScoped<IRecipeRepository, RecipeRepository>().Decorate<IRecipeRepository, CachedRecipeRepository>()` (Scrutor).
- `GetAllAsync` → key `recipes_all`; `GetByIdAsync` → key `recipe_{guid}`. Durations from `CacheDuration`
  (default 10 min absolute / 5 min sliding; long 30 / 15 for freshly-added recipes).
- Writes invalidate both `recipes_all` and the per-id key. `AddAsync` invalidates the list and warms the item.
- Cache set/remove failures are swallowed and logged as warnings — caching is best-effort and must never fail a
  request.
- Cached values are **entity instances**, safe today because reads are `AsNoTracking()` and `Recipe` exposes
  read-only collections.

### Logging

`ILogger<T>` via constructor injection, used in `RecipesController`, `ErrorHandlerMiddleware`,
`DeleteRecipeHandler`, `GetRecipeByIdHandler`, `CachedRecipeRepository`. No Serilog, no structured sinks;
default ASP.NET console logging configured in `appsettings.json`.

Two known deviations: `RecipesController` logs with interpolated strings instead of message templates, and
`ApplicationInitializer.MigrateDatabase` uses `Console.WriteLine` — `QUAL-01` and `QUAL-02` in
[known-issues.md](known-issues.md).

### Authentication & authorization

**None.** `ApplicationInitializer.ConfigurePipeline` calls `app.UseAuthorization()` without
`UseAuthentication()`, there are no `[Authorize]` attributes, no identity provider, and no `User` entity. Every
endpoint is anonymous and every recipe is world-writable. See
[agents/05-security-reviewer.md](agents/05-security-reviewer.md).

### Pipeline order (`ConfigurePipeline`)

`UseCors("AllowReactApp")` → `UseHttpsRedirection` → `UseErrorHandler` → `UseRouting` → `UseAuthorization` →
`MapControllers`.

`UseErrorHandler` sits before `UseRouting`, so exceptions thrown in routing or CORS are **not** wrapped into
`ProblemDetails`. Whether that is intentional is an open decision — `DEC-04` in
[known-issues.md](known-issues.md).

---

## Decision log (condensed ADRs)

### ADR-001 — Hand-rolled CQRS instead of MediatR

- **Status:** accepted (commit `05656ed` "Fix CQRS without MediatR").
- **Decision:** custom `ICommand`/`IQuery` markers with `CommandDispatcher`/`QueryDispatcher` resolving handlers
  from `IServiceProvider`.
- **Consequences:** no third-party licence/version risk (MediatR is commercially licensed from v12). No
  pipeline behaviours, so cross-cutting logic (logging, validation, transactions) must be added by decorating
  handlers — Scrutor's `Decorate` already does this for the repository, so the pattern exists.
- **Confirmed 2026-07-26.** The decision stands; its one real drawback (manual handler registration failing at
  runtime rather than compile time) was removed by ADR-008 rather than by adopting MediatR.

### ADR-002 — `FluentResults` for expected failures

- **Status:** accepted (commits `1a1f6de`, `2f3fb86`).
- **Decision:** domain and application failures are `Result`/`Result<T>` carrying `RecipeErrors` errors with
  `ErrorCode` and `field` metadata; `ResultExtensions` translates them to `ProblemDetails`.
- **Consequences:** no exception-driven control flow — this part is correct and stays.
- **Partially superseded by ADR-009.** Putting the *HTTP status* in domain metadata was a mistake: it makes
  `RecipeManager.Domain` depend on HTTP semantics, which is the one place the layering is violated, and it
  causes a real bug (`ResultExtensions` reads the status from `errors.First()`, so mixed error kinds return an
  arbitrary status).

### ADR-003 — Caching as a repository decorator

- **Status:** accepted (commit `1f7e8c0`).
- **Decision:** `CachedRecipeRepository` decorates `IRecipeRepository` via Scrutor rather than caching inside
  handlers.
- **Consequences:** handlers stay cache-agnostic; every new repository method must consider invalidation.
  `IMemoryCache` is per-process — **this does not survive scale-out**. Moving to a distributed cache means
  swapping `MemoryCacheService` only.

### ADR-004 — Ingredients and instructions as `IReadOnlyList<string>`

- **Status:** accepted (initial migration).
- **Decision:** no `Ingredient`, `Step`, `Unit` or `Quantity` entities; both are primitive string collections
  persisted as PostgreSQL `text[]`.
- **Consequences:** trivially simple, and `text[]` *is* queryable in PostgreSQL — but there is no quantity,
  unit, or ingredient catalogue, so unit conversion, serving scaling, and shopping lists remain impossible.
  Search is currently done **client-side** in `RecipeList.tsx`.
- **Status update 2026-07-26:** confirmed as an **acknowledged temporary shortcut, not a permanent design.**
  The project intends to move to structured ingredients — `R-10` in [roadmap.md](roadmap.md). Do not build
  features that entrench the free-text shape, and do not treat this ADR as a reason to reject structuring.

### ADR-005 — `IntegrationTest` environment guarded by `#if DEBUG`

- **Status:** accepted (commit `898c9ce` "Fix security issues integration tests").
- **Decision:** `Program.Main` skips DbContext registration and startup migration when
  `EnvironmentName == "IntegrationTest"`, and **throws in RELEASE builds** if that environment name is used.
- **Consequences:** tests can inject EF InMemory through `WebApplicationFactory`; the escape hatch cannot be
  abused in production. Do not remove the `#if DEBUG` guard.

### ADR-006 — No unit-of-work abstraction

- **Status:** accepted (implicit).
- **Decision:** `RecipeRepository` calls `SaveChangesAsync` inside every write method.
- **Consequences:** fine for a single aggregate; **a multi-entity transaction is impossible today**. Introducing
  a second aggregate that must change atomically with `Recipe` requires an architecture decision first.

### ADR-007 — .NET 10 + PostgreSQL

- **Status:** accepted (commit `d2d490d`, merged as `edfd057`).
- **Decision:** upgrade all projects from `net8.0` to `net10.0`, replace
  `Microsoft.EntityFrameworkCore.SqlServer` with `Npgsql.EntityFrameworkCore.PostgreSQL`, and pin the SDK in
  `RecipeManager/global.json` (`10.0.302`, `rollForward: latestFeature`).
- **Consequences:** the connection string format changed to Npgsql keywords and the committed value is now a
  **password-less template** (SQL Server integrated auth is gone, so a password is required at runtime and must
  come from user-secrets or environment). `Ingredients`/`Instructions` became `text[]`. `nvarchar(max)` is now
  `text`, `uniqueidentifier` is `uuid`. PostgreSQL identifier folding means the table must be quoted in psql.
  Docker base images moved to `mcr.microsoft.com/dotnet/{aspnet,sdk}:10.0`.

### ADR-008 — Auto-register CQRS handlers with Scrutor

- **Status:** accepted 2026-07-26, **implemented 2026-08-03**.
- **Context:** ADR-001's hand-rolled CQRS required every handler to be listed in
  `ServiceInitializer.RegisterCqrsHandlers`. A missing line compiled cleanly and threw at runtime on
  `GetRequiredService`. This was the most likely bug class in the backend and it scaled with every new use case.
- **Decision:** discover `ICommandHandler<,>` and `IQueryHandler<,>` implementations by assembly scanning with
  Scrutor — **already a dependency**, previously used only for `Decorate`. `RegisterCqrsHandlers` was deleted
  and the scan lives in `ServiceInitializer.RegisterCqrsDispatchers`.
- **Consequences:** new handlers need no DI change, removing the failure mode entirely, at no dependency cost.
  Registration is implicit, so `RecipeManager.IntegrationTests/DependencyInjection/CqrsHandlerRegistrationTests.cs`
  resolves every closed handler interface found in the Application assembly — that test is what keeps the
  registration verifiable, and it is not optional. Explicitly chosen over adopting MediatR, which would add a
  commercially-licensed dependency to solve the same problem.

### ADR-009 — Domain errors carry a semantic kind, not an HTTP status

- **Status:** accepted 2026-07-26, **not yet implemented** (`R-05` in [roadmap.md](roadmap.md)). Partially
  supersedes ADR-002.
- **Context:** `RecipeErrors` writes HTTP status codes into domain error metadata
  (`.WithCode(422)`), so `RecipeManager.Domain` — the layer that must depend on nothing — encodes HTTP
  semantics. `ResultExtensions.CreateProblemDetails` then derives the response status from `errors.First()`,
  so a `Result` carrying both a 404 and a 422 returns whichever error happens to be first.
- **Decision:** domain errors carry a semantic kind (`Validation`, `NotFound`, `Conflict`, …). The API layer
  owns the kind → HTTP mapping and selects the most severe error rather than the first.
- **Consequences:** the Domain project becomes transport-agnostic and the layering violation disappears;
  mixed-kind results return a correct status. Requires touching every `RecipeErrors` factory and
  `ResultExtensions`, with the existing integration tests as the regression net — externally visible status
  codes must not change.

### ADR-010 — Warnings are errors, and project properties are centralised

- **Status:** accepted and **implemented 2026-08-04** (`R-02`). Closes `BUILD-01` and `BUILD-02`.
- **Context:** `TargetFramework`, `Nullable`, and `ImplicitUsings` were duplicated across all six `.csproj`
  files, and nothing stopped a warning from being committed. Seven had accumulated since the .NET 10 upgrade and
  survived several PRs, because a warning is only a note in scrollback that everyone learns to skip.
- **Decision:** `RecipeManager/Directory.Build.props` owns `TargetFramework`, `Nullable`, `ImplicitUsings`,
  `TreatWarningsAsErrors`, and `EnforceCodeStyleInBuild` for every project in the solution. The six `.csproj`
  files keep only what is genuinely project-specific (`UserSecretsId`, `IsTestProject`, package references).
  `TreatWarningsAsErrors` is **unconditional**, not scoped to Release: with no CI yet (`INFRA-01`, `R-04`), the
  local Debug build is the only gate that exists, so a Release-only condition would enforce nothing.
- **Consequences:** a warning now stops the build, so it must be fixed or explicitly suppressed with a stated
  reason rather than accumulating. The cost is real: an unused local while mid-refactor fails the build, which
  is friction exactly when iterating. That friction is the mechanism, not a side effect — the alternative is a
  warning count that only ever goes up.
  `EnforceCodeStyleInBuild` currently reports nothing, because IDE style rules default to suggestion severity
  and there is no `.editorconfig`. It is a latch: the day an `.editorconfig` is added, style violations become
  build failures without a further change — which is why adding one is tracked as `R-15` rather than done
  incidentally. Verified by a deliberate negative test — an unused local produced `error CS0219` and failed the
  build.
- **Also elevates NuGet restore warnings**, which is easy to miss: `TreatWarningsAsErrors` is honoured by
  restore, not only by the compiler. Verified — a package with a published advisory produces
  `error NU1903 … Warning As Error` and fails the build, and `NuGetAuditMode` defaults to `all` on .NET 10, so
  **transitive** advisories count too. All six projects are clean today, so nothing breaks now; the consequence
  is that a *newly published* advisory against any dependency will fail the build with no code change.
  Accepted deliberately: it delivers, earlier and harder, the `dotnet list package --vulnerable` gate that
  `R-04` plans for CI, and `SEC-03` (68 npm alerts nobody acts on) is the failure mode it prevents on the .NET
  side. If it ever becomes obstructive the escape hatch is
  `<WarningsNotAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>` — do not reach for it without
  recording why.

### ADR-011 — Central Package Management

- **Status:** accepted and **implemented 2026-08-04**.
- **Context:** ADR-010 removed duplicated *properties* from the `.csproj` files but left duplicated *versions*:
  ten packages declared their version in two projects each — `Microsoft.EntityFrameworkCore` 10.0.10 and
  `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 in both `Api` and `Infrastructure`, `FluentResults` in both
  `Domain` and `Application`, and the whole xunit/FluentAssertions/coverlet set in both test projects.
- **The failure this prevents.** Bumping EF Core in one project and not the other does not warn and does not
  fail: NuGet resolves the conflict by nearest-wins and the mismatch surfaces at runtime, in whichever project
  lost. That is strictly worse than the warning problem ADR-010 solved, because there is no diagnostic at all.
- **Decision:** `RecipeManager/Directory.Packages.props` sets `ManagePackageVersionsCentrally` and holds every
  version as a `PackageVersion` item. A `.csproj` names the packages it needs — `<PackageReference Include="…" />`
  with **no** `Version` attribute — and keeps only per-project metadata (`PrivateAssets`, `IncludeAssets`).
- **Consequences:** a version exists in exactly one place, so drift becomes unrepresentable rather than merely
  discouraged — verified by a negative test: adding `Version=` back to a `.csproj` produces
  `error NU1008` and fails the build. Upgrading a shared package is now a one-line edit, which also makes the
  `R-04` vulnerability remediation loop single-edit. The cost is indirection: reading a `.csproj` no longer
  tells you which version you get, and a newcomer who adds a `PackageReference` the usual way (with a version)
  gets an error until they learn the convention — an error being the point.
- **Rejected:** leaving versions in the `.csproj` files and relying on review to keep them aligned. That is the
  same class of rule ADR-008 abandoned for handler registration: enforced only by attention, and it fails
  silently.
- **Rejected:** *(a)* Release-only enforcement — frictionless locally, but enforces nothing until CI exists.
  *(b)* Listing specific rule IDs in `WarningsAsErrors` — surgical, but it is a list somebody must maintain and
  it cannot stop a new class of warning. *(c)* Fixing the seven warnings without the gate — leaves the count
  free to grow back, which is the `BUILD-03` failure mode.

### ADR-012 — The frontend gets a build gate: `jiti` and a type-checking build

- **Status:** accepted and **implemented 2026-08-08** (`R-03`).
- **Context:** ADR-010 made every backend warning a build failure, but stops at the solution boundary. On the
  frontend `npm run lint` could not *start* — `eslint.config.ts` is a TypeScript flat config and ESLint 9 loads
  those through `jiti`, which was never a dependency (`BUILD-03`, since commit `3474a8b`, 2025-08-08) — and
  `"build": "vite build"` transpiles with esbuild, which strips types without checking them (`BUILD-04`). So no
  ESLint rule and no type error could fail anything for eleven months.
- **Decision:** add `jiti` to `devDependencies`; make `"build": "tsc -b && vite build"` and add
  `"typecheck": "tsc --noEmit"`. Scope the type-checked ESLint rule sets to the files `tsconfig.json` actually
  includes, and lint the root config files with a non-type-checked config instead.
- **Alternatives:** *(a)* rename the config to `eslint.config.js` — no new dependency, but it discards type
  checking of the config and the typed `tseslint.config()` helper the project already uses. *(b)*
  `tsc --noEmit && vite build` — one fewer TS mode to understand, but not incremental; `tsc -b` is the Vite
  React-TS template default and both are valid against a single non-composite `tsconfig.json`. *(c)* leave it
  and rely on `R-04`'s CI to run `npx tsc` directly — but CI cannot run a script that does not exist, and the
  local loop would still have no gate.
- **Consequences:** the frontend now has the enforcement ADR-010 gave the backend, so `R-04` has something to
  call and `R-07` (Vitest) has a working toolchain to attach to. The cost is real and daily: every frontend PR
  must satisfy `recommendedTypeChecked` + `stylisticTypeChecked`, and a build that used to succeed with a type
  error now fails. Verified by negative test in both directions — a deliberate type error fails `npm run build`,
  and a deliberate `console.log` is reported by `npm run lint`. Note what is **not** bought: nothing forces
  anyone to run either command until CI exists (`INFRA-01`, `R-04`). This makes the gate possible, not
  automatic.

### ADR-013 — CI enforces the checklist, and NuGet restores from a lock file

- **Status:** accepted and **implemented 2026-08-08** (`R-04`). Closes `INFRA-01`, `INFRA-06`, `BUILD-07`.
- **Context:** ADR-010 and ADR-012 built gates on both halves of the codebase and neither is *run* by anything.
  `BUILD-03` is the proof that this matters: `npm run lint` could not start for eleven months unnoticed. Ten
  package versions were also centralised by ADR-011, but the **transitive** closure was still resolved fresh on
  every restore, so CI could legitimately get a different graph than a developer did, with no diagnostic.
- **Decision:** a GitHub Actions workflow on every PR to `main` and every push to `main`, in two parallel jobs
  (backend: locked restore → build → test → vulnerable-package check; frontend: `npm ci` → typecheck → lint →
  build → audit). `RestorePackagesWithLockFile` in `Directory.Build.props` with six committed
  `packages.lock.json`, restored in CI with `--locked-mode`. Dependabot for `npm`, `nuget`, and
  `github-actions`, grouped weekly. Node pinned in `.nvmrc`, read by the workflow.
- **Alternatives:** *(a)* One sequential job — simpler, but a frontend lint error hides behind a five-minute
  backend run, and the two are genuinely independent. *(b)* Trusting `dotnet list package --vulnerable`'s exit
  code — it exits **0 even when it finds vulnerabilities** (verified), so the step could never fail; its output
  is parsed instead. *(c)* Pinning actions by tag — tags are mutable and can be repointed at new code, which is
  how the 2025 `tj-actions/changed-files` compromise leaked secrets; actions are pinned by commit SHA, and the
  `github-actions` Dependabot ecosystem is what stops those pins rotting. *(d)* Skipping lock files — smallest
  diff, but leaves the silent-divergence failure ADR-011 removed for direct versions.
- **Consequences:** the checklist in [workflows/release-workflow.md](workflows/release-workflow.md) stops being
  a document people are trusted to follow. `R-06` (Testcontainers) is unblocked, since Docker exists on the
  runner. The costs are real: every PR now waits on CI; every dependency change must regenerate the lock files
  or CI fails at `--locked-mode` with an unhelpful message; and CI tempts people to stop running checks locally,
  which makes the individual loop slower even as the repo's guarantee gets stronger.
- **CI builds `Debug`, not `Release`, and that is load-bearing.** ADR-005 makes `Program.Main` throw
  `"IntegrationTest environment is not allowed in RELEASE builds"` — a deliberate guard against setting that
  environment on a real deployment. `WebApplicationFactory` uses exactly that environment name, so a Release
  build fails **all 14** integration tests by design; measured, 84 passing drops to 70. Nothing is lost, because
  `TreatWarningsAsErrors` is unconditional rather than Release-only (ADR-010), so the warning gate is identical
  in Debug. What is **not** covered is a Release-only compilation difference. Accepted — and it is worth noting
  that a guard added for *security* reasons in 2025 turned out to constrain the CI design three items later.
- **Verified by negative test**, per the 2026-08-04 entry — each gate was observed rejecting:
  `NU1004` on a lock-file mismatch, `NU1903` on a deliberately vulnerable package, `CS0219` on an unused local,
  a non-zero exit on an inverted assertion, `TS2322` on a bad annotation, and `no-console` on a `console.log`.
- **What this does not buy.** The workflow makes the checks *exist*; only a branch-protection rule makes them
  *required*, and that is a repository setting, not a file. Until it is enabled, a red pipeline is advisory.

---

These ADRs were **reconstructed from code and commit messages** — no ADR files existed before, so ADR-001
through ADR-007 are documentation of decisions already made, while ADR-008 and ADR-009 are new decisions taken
during the documentation review. Whether to split them into individual files under `docs/adr/` is open:
`DEC-08` in [known-issues.md](known-issues.md).
