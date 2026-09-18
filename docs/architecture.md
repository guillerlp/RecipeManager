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
| `RecipeManager.UnitTests` | `Domain`, `Application`, `Api` |
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
- `RecipeManager.Domain/Errors/RecipeErrors.cs` — every domain error as a static factory returning a `DomainError`
  (`RecipeManager.Domain/Errors/DomainError.cs`) — a `FluentResults.Error` carrying an `ErrorKind` (`Validation`,
  `NotFound`) plus `field` metadata. The Domain knows *what* failed, never which HTTP status reports it.
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
- `RecipeManager.Api/Extensions/ResultExtensions.cs` — `Result` → `ActionResult` + `ProblemDetails`. Owns the only
  `ErrorKind` → HTTP status mapping.
- `RecipeManager.Api/Middlewares/ErrorHandlerMiddleware.cs` — last-resort exception → `ProblemDetails`.

## Request flow (`PUT /api/recipes/{id}`)

1. ASP.NET binds `UpdateRecipeDto`; `AddFluentValidationAutoValidation` runs `UpdateRecipeDtoValidator`.
   Shape failure → **400** with the framework's `ValidationProblemDetails`; the controller never runs.
2. `RecipesController.Update` maps the DTO + route id into `UpdateRecipeCommand`.
3. `ICommandDispatcher.Dispatch<UpdateRecipeCommand, Result>` resolves `UpdateRecipeHandler`.
4. Handler loads via `IRecipeRepository.GetByIdAsync` — the **`CachedRecipeRepository` decorator** answers first,
   falling through to `RecipeRepository`. Missing → `RecipeErrors.RecipeNotFound` (kind `NotFound` → **404**).
5. `recipe.Update(...)` enforces domain invariants → failure returns `RecipeErrors.*` (kind `Validation` → **422**).
6. `UpdateAsync` persists, then invalidates `recipes_all` and `recipe_{id}`.
7. `result.ToActionResult()` → **204 No Content** on success, `ProblemDetails` otherwise.

## Cross-cutting concerns

### Error handling — three distinct channels

| Channel | Triggered by | Status | Body |
| --- | --- | --- | --- |
| FluentValidation auto-validation | null / length / range on the bound type | 400 | `ValidationProblemDetails` |
| `Result` + `RecipeErrors` → `ResultExtensions` | domain invariants, not-found | 422 / 404 / 400 — `ErrorKind` mapped in `ResultExtensions` (`Validation` 422, `NotFound` 404, no kind 400), ADR-009 | `ProblemDetails` + `field`, plus an `errors[]` extension when more than one error |
| `ErrorHandlerMiddleware` | unhandled exception | 404 / 400 / 401 / 500 by exception type | `ProblemDetails` |

`ResultExtensions.CreateProblemDetails` derives the status from the **most severe** error — no kind (3) >
`NotFound` (2) > `Validation` (1), first error on a tie — and takes `Detail` and `field` from that same error.
Every error is listed under the `errors` extension key, in original order, each with its own mapped `code`.
Handlers do not need to order their errors. An error that is not a `DomainError` ranks highest and maps to
400: it is a failure the domain did not model, and must not hide behind a 422.

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
- Cached values are **entity instances**. The intended safety argument is that reads are `AsNoTracking()` and
  `Recipe` exposes read-only collections. **Only the first half holds today:** a recipe read from the database
  carries a mutable `List<string>` behind `IReadOnlyList<string>`, and the cache serves that same instance to
  every later request. See [BUG-11](known-issues.md#bug-11).

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
  arbitrary status). Implemented 2026-09-16 (`R-05`).
- **Status update 2026-09-13 — FluentResults 4.0.** The contract above is unchanged (same `Result`/`Result<T>`,
  same `ErrorCode`/`field` metadata, same `ProblemDetails` mapping). Two library semantics changed and are now
  part of this ADR:
  - `Result.Errors` is `IReadOnlyList<IError>`, so code that consumes errors takes `IReadOnlyList<IError>` (see
    `ResultExtensions.CreateProblemDetails`). A failed `Result` is never edited through `Errors`.
  - `Result.Fail(IEnumerable<IError>)` **throws `ArgumentException` on an empty collection**. In 3.16 the same
    call returned a *successful* result, because `IsFailed` is computed as "has any error". Every collection
    passed to `Fail` must be provably non-empty — guard with `IsFailed` or a count check, as `Recipe.Create`,
    `Recipe.ValidateProperties`, and `CreateRecipeHandler` do. A violation now surfaces as a 500 through
    `ErrorHandlerMiddleware` rather than as a silent success.

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
- **Consequences:** tests can inject their own `AppDbContext` registration through `WebApplicationFactory`; the
  escape hatch cannot be abused in production. Do not remove the `#if DEBUG` guard.
- **Still current after ADR-017.** What the tests inject is now a real PostgreSQL container's connection rather
  than EF InMemory, and they apply the migrations themselves because `Program` still does not. ADR-017
  considered routing the connection string through configuration so the normal startup path would run, and
  rejected it precisely to leave this guard alone.

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

- **Status:** accepted 2026-07-26, **implemented 2026-09-16** (`R-05`, spec [003](specs/003-domain-error-kinds.md)).
  Partially supersedes ADR-002.
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
- **Implementation refinements (2026-09-16).** The kind is carried by a typed `sealed DomainError : Error`
  rather than a metadata key, so a misspelt key cannot silently fall through to a default status. `ErrorKind`
  holds only `Validation` and `NotFound`; `Conflict` is added with the first feature that produces one. Errors
  without a kind keep their previous 400 and rank as most severe. An unmapped kind throws, so a new kind
  cannot ship without a status. `RecipeManager.UnitTests` now references `RecipeManager.Api` to test the
  mapping directly, because no endpoint can yet produce mixed kinds.

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

- **Status:** accepted and **implemented 2026-08-08** (`R-03`). The ESLint/`jiti` half is **superseded by
  ADR-016** (Oxlint); the type-checking build (`tsc -b && vite build`, `typecheck`) still stands.
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

### ADR-014 — Remove MUI; the app owns its four icons

- **Status:** accepted and **implemented 2026-09-13**. Supersedes Dependabot #17 (`@mui/icons-material` 7 → 9).
- **Context:** MUI 7 → 9 was due (#17 could not merge: icons 9 peers on `@mui/material` 9). The entire usage was
  two `Box` elements (`HomePage`, `AppLayout`) and four icons, and the conventions already forbade anything more
  (`sx`, `styled`, `ThemeProvider`). That surface carried `@mui/material`, `@mui/icons-material`,
  `@emotion/react`, `@emotion/styled` and 49 transitive packages, plus Emotion styles injected at runtime that
  out-ranked the CSS Modules — the `!important`s in `Logo.module.css` and `Footer.module.css` existed only to win.
- **Decision:** remove all four packages. `Box` becomes `<div>` / `<main>`. The icons become
  `components/ui/Icon/`: MUI's exact Material Icons path data (Apache-2.0) in plain `<svg>`, with SvgIcon's
  defaults in a zero-specificity `:where(.icon)` rule so a consumer class always wins regardless of bundle order.
- **Alternatives:** *(a)* upgrade to MUI 9 — verified zero code changes, but every future MUI and Emotion major
  repeats the exercise for four glyphs, and MUI 9 raises the browser floor to Safari 17 / Chrome 117. *(b)* A
  lighter icon library (lucide) — still a dependency to render four SVGs, with different glyphs. *(c)* Stay on
  MUI 7 — v7's last release was 7.3.11 in May 2026.
- **Consequences:** JS bundle 401.95 → 327.66 kB (gzip 133.95 → 107.28), 279 → 226 installed packages, and one
  styling system instead of two. Harder: a new icon is copied by hand from the Material Icons catalogue, and any
  future need for real MUI components (dialogs, pickers) means re-adopting it as a deliberate decision rather than
  finding it already installed. One visible change, verified: at ≤420 px the search icon now honours the 18 px
  `SearchBar.module.css` always declared — Emotion's injected `width: 1em` had silently kept it at 24 px.

### ADR-015 — Vite 8 and `@vitejs/plugin-react` 6

- **Status:** accepted and **implemented 2026-09-13**. Supersedes Dependabot #16.
- **Context:** #16 bumped `@vitejs/plugin-react` to 6, which peers on `vite ^8` while `vite` stayed on 7, so it
  never installed. Vite 8 replaces the build toolchain underneath the same config: Rolldown instead of Rollup and
  esbuild, Oxc for transforms and React Fast Refresh (plugin-react 6 drops Babel), and Lightning CSS for CSS
  minification. `vite.config.ts` is small (one plugin, aliases, a dev proxy), so the config surface was not the
  risk; changed output was.
- **Decision:** `vite` 8.3 + `@vitejs/plugin-react` 6.1. `__dirname` in `vite.config.ts` becomes
  `import.meta.dirname` (Vite 8 warns that its planned native config loader cannot provide `__dirname`).
  `engines` tightens from `>=20` to `^20.19.0 || >=22.12.0`, copied from Vite's own. A `vite` Dependabot group,
  ordered before the minor/patch group, keeps `vite` and `@vitejs/*` in one PR from now on.
- **Alternatives:** *(a)* defer — Vite 7 keeps working, but #16 re-raises weekly and plugin-react 4 falls further
  behind the toolchain `R-07` (Vitest) will build on. *(b)* stay on Vite 7 and try `rolldown-vite` first — a
  staging step that suits a large config; here it would be two migrations instead of one. *(c)* keep
  `engines: >=20` — fewer characters, but a claim the build tool contradicts.
- **Consequences:** production build 991 ms → 201 ms, JS 327.66 → 324.39 kB, 226 → 183 packages. The production
  CSS was proven **semantically identical**, not merely similar: both builds parsed with the browser's CSSOM and
  compared by longhand property per selector and media condition. Every textual difference was a rewrite with the
  same meaning (declaration order, `(max-width: 768px)` → `(width <= 768px)`, `#ffffff` → `#fff`, two adjacent
  `opacity: 1` rules merged in place). Computed styles on every element of both routes in both themes matched too,
  and Fast Refresh was observed preserving component state. Harder: Babel plugins can no longer be passed through
  `react({ babel })`, and the default `build.target` rises to Chrome 111 / Firefox 114 / Safari 16.4.

### ADR-016 — TypeScript 7, linted by Oxlint instead of ESLint

- **Status:** accepted and **implemented 2026-09-16**. Supersedes Dependabot #19 (`typescript` 5.9 → 7.0.2), and
  the ESLint/`jiti` half of ADR-012.
- **Context:** TypeScript 7 is the Go-native compiler. `typescript@7` ships a `tsc` binary and **no JavaScript
  API**, and typescript-eslint is built on that API: its latest release (8.70) peers on `typescript <6.1.0`, and
  upstream closed the requests to support 7 as not planned (tracking: typescript-eslint#10940). So #19 could
  never install, and no ESLint setup with type-aware rules can run on TypeScript 7 today. The two TS 7 errors
  that were ours to fix (`baseUrl`, `process.env`) were removed first, in #32.
- **Decision:** replace ESLint with **Oxlint** in type-aware mode (`oxlint` + `oxlint-tsgolint`, which embeds
  its own typescript-go) and move to `typescript` 7.0.2. `.oxlintrc.json` carries exactly the 71 rules ESLint
  enforced, translated by `@oxlint/migrate`, with Oxlint's default `correctness` category turned off so nothing
  else runs. The three packages are **pinned exactly** and share a `typescript` Dependabot group, because
  `oxlint-tsgolint@7.0.2001` is versioned after the TypeScript it embeds — letting `typescript` float alone would
  let `tsc` and the linter type-check with different compilers.
- **Alternatives:** *(a)* stay on TypeScript 5.9/6 and keep ESLint — works today, but blocks on a dependency
  whose maintainers have declined the work, and #19 re-raises weekly. *(b)* Biome — a single tool, but it
  mapped only 46 of the 72 rules and has none of the `no-unsafe-*` family, `unbound-method`,
  `restrict-template-expressions` or `await-thenable`, which are the rules that justify type-aware linting at
  all. *(c)* Oxlint without type-aware mode — drops those same rules. *(d)* keep ESLint for non-typed rules and
  Oxlint for typed ones — two configs to keep in sync for no rule the single Oxlint config lacks.
- **Consequences:** 183 → 83 installed packages; `tsc --noEmit` ≈0.5–0.7 s and lint ≈0.7 s, against 1.6 s and
  2.95 s before. The production bundle is byte-identical (`tsc` does not feed Vite). Verified by negative test:
  a probe file tripped `no-console`, `no-explicit-any`, `no-floating-promises`, `no-unsafe-assignment`,
  `no-unsafe-call` and `no-unsafe-member-access`, and `tsc` rejected a `TS2322`. What it costs: one rule,
  `prefer-optional-chain`, exists only in Oxlint's nursery and is **not** enforced; Oxlint's type-aware rule
  coverage is described upstream as close to, not equal with, typescript-eslint's, and it was declared stable
  only in July 2026; and rule names are spelled differently in config (`typescript/…`, `react/…` instead of
  `@typescript-eslint/…`, `react-hooks/…`, `react-refresh/…`). Root
  tooling files (`vite.config.ts`) still get no lint rules — which, it turned out, was already true under
  ADR-012: that ESLint block only *disabled* the typed rules and enabled nothing (`BUILD-10`).
- **Amended 2026-09-16 (`BUILD-10`).** The `correctness` category is now **on** for every file, and
  `tsconfig.node.json` type-checks `vite.config.ts`. "Exactly the ESLint rule set" was the right bar for the
  swap itself — one change at a time — but not a reason to leave root files unchecked afterwards. Oxlint
  overrides cannot set categories, so `src/` gains the category's 88 rules too; they found nothing. Keeping
  `vite.config.ts` in its own tsconfig, rather than adding Node types to `tsconfig.json`, is what stops
  `process` from type-checking in browser code again (#32).

### ADR-017 — Integration tests run against real PostgreSQL in a container

- **Status:** accepted and **implemented 2026-09-17** (`R-06`,
  [specs/004-testcontainers-integration-tests.md](specs/004-testcontainers-integration-tests.md)). Closes
  `TEST-06`.
- **Context:** `IntegrationTestBase` used `Microsoft.EntityFrameworkCore.InMemory`, which is not a relational
  provider — no SQL, no constraints, no collation, no `text[]`. The 14 integration tests proved the pipeline
  (routing, binding, validation, DI, handlers) and nothing about the database the application actually runs on.
  The EF Core team recommends against InMemory for exactly this use. The item was deferred until CI existed,
  because a Docker-backed suite needs Docker in both places; ADR-013 closed that, since `ubuntu-latest`
  provides Docker.
- **Decision:** `Testcontainers.PostgreSql` starts **one** `postgres:18-alpine` container per test assembly,
  owned by an xUnit collection fixture. Each test class gets its own database on that container, created
  together with the schema by `Database.Migrate()` — the project's own migrations, not `EnsureCreated()`, so
  the tests run against the schema that is actually deployed. When Docker is unavailable the integration tests
  **skip** with a message naming Docker; the unit tests are unaffected.
  `Microsoft.EntityFrameworkCore.InMemory` is removed rather than left installed.
- **Skipping needs a second package.** xUnit 2.x has no dynamic skip — `Assert.Skip` is v3 only — so
  `Xunit.SkippableFact` supplies it: `Skip.If(...)` in the base constructor throws, and `[SkippableFact]` /
  `[SkippableTheory]` translate that failure into a skip. This is the entire reason the integration tests carry
  those attributes instead of `[Fact]`/`[Theory]`. Moving the test stack to xUnit v3 would delete the
  dependency; that is not this decision.
- **ADR-005 is unchanged and still stands.** The `IntegrationTest` environment still skips DbContext
  registration and `app.MigrateDatabase()`, and the `#if DEBUG` guard stays. The alternative — injecting
  `ConnectionStrings__DefaultConnection` so `Program`'s normal path runs — would also cover the startup
  migration, but it changes production code and erodes that guard for a test-only benefit.
- **Alternatives:** *(a)* stay on InMemory — fast and Docker-free, but `TEST-06` is unfixable and `BUG-11`
  unverifiable. *(b)* SQLite in-memory — real SQL, but a different dialect with no `text[]`, so the mapping
  this exists to exercise still would not run. *(c)* a GitHub Actions `services:` container — configures CI
  only, so the local and CI setups diverge, which is what `.nvmrc` and `--locked-mode` were adopted to prevent.
  *(d)* one container per test class (the roadmap's sketch) — pays a container start per class for isolation a
  per-class database already gives. *(e)* a shared database reset by Respawn — a second dependency for 14 tests
  that do not need it. *(f)* failing hard without Docker — turns an environment constraint into a red local
  suite (`INFRA-06`), which teaches developers to ignore red.
- **Consequences:** `text[]`, identifier folding, real constraints, and the migrations themselves are now
  exercised on every run — a broken migration fails the suite instead of surfacing when someone next starts the
  API. What it costs: the suite needs Docker, and a developer without it sees skips rather than results, which
  is a silent test by design; CI pays one image pull and one container start; the PostgreSQL version becomes a
  string literal in test code that Dependabot does not manage and that can drift from `README.md` and from
  production; the image is pinned by tag, weaker than the SHA pinning `ci.yml` applies to third-party actions;
  and a migration mistake now presents as 14 failures at once. `app.MigrateDatabase()` itself remains untested,
  by the ADR-005 decision above. This is also the repo's first xUnit fixture of any kind — `conventions.md`
  previously recorded that none were in use.

### ADR-018 — Frontend tests run on Vitest + React Testing Library under jsdom

- **Status:** accepted and **implemented 2026-09-18** (`R-07`,
  [specs/005-frontend-test-runner.md](specs/005-frontend-test-runner.md)). Closes `TEST-01`.
- **Context:** the SPA had no test runner and no tests. CI's frontend job proved the code type-checked, linted,
  and bundled — never that it behaved — and Dependabot's npm majors were merged on that evidence alone.
- **Decision:** `vitest`, `jsdom`, `@testing-library/react` and its peer `@testing-library/dom`, all dev-only.
  Vitest is configured by a `test` block in `recipe-manager-frontend/vite.config.ts` (with `defineConfig` imported from
  `vitest/config`), so tests resolve aliases, the React plugin, CSS Modules, and assets exactly as the build
  does. Globals are **off**: tests import `describe`/`it`/`expect`/`vi` from `vitest`, which keeps them
  ordinary TypeScript that `tsc -b` and the type-aware Oxlint rules already understand;
  `recipe-manager-frontend/src/test/setup.ts` registers RTL's `cleanup()` because RTL auto-cleans only when
  globals are on. Tests are colocated
  (`Foo.test.tsx` beside `Foo.tsx`). CI runs `npm test` (`vitest run`) between `Lint` and `Build`.
- **Data seam:** component tests that fetch mock `@/services` with `vi.mock` and render inside a fresh
  `QueryClient`, so the real hooks and TanStack Query's state machine run. `useRecipes` hard-codes `retry: 2`,
  which overrides any client default, so error-state tests advance fake timers through the backoff.
- **Alternatives:** *(a)* Jest — the largest ecosystem, but a separate transform and a second copy of the eight
  path aliases. *(b)* happy-dom — faster, less spec-complete; immeasurable at this size. *(c)* a separate
  `vitest.config.ts` — cleaner split, but one more tooling file and a `mergeConfig` indirection that can drift.
  *(d)* mocking `useRecipes` itself — simplest, but tests the component against a hand-written hook shape.
  *(e)* MSW — most realistic, most setup; reconsider when mutation hooks exist. *(f)* `jest-dom` and
  `user-event` — not needed by any current test; `getBy*` already throws on no match.
- **Consequences:** behaviour of the five highest-value frontend areas is asserted on every PR. What it costs:
  `npm ci` installs jsdom's tree and the CI frontend job is slower; test files sit inside `recipe-manager-frontend/tsconfig.json`'s
  `include`, so a type error in a test fails `npm run build` — deliberate, but coupling; the `RecipeList`
  error-state test is coupled to `useRecipes`' retry policy and changes with it; jsdom has no layout engine, so
  nothing visual, and no real browser behaviour, is covered. `formatDuration`/`getISODuration` moved from
  `RecipeCard` into `recipe-manager-frontend/src/utils/duration.ts`, the repo's first `utils/` module.

---

These ADRs were **reconstructed from code and commit messages** — no ADR files existed before, so ADR-001
through ADR-007 are documentation of decisions already made, while ADR-008 and ADR-009 are new decisions taken
during the documentation review. Whether to split them into individual files under `docs/adr/` is open:
`DEC-08` in [known-issues.md](known-issues.md).
