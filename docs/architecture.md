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

- `RecipeManager.Domain/Shared/Entity.cs` — abstract base with `Guid Id { get; protected init; }` plus `Equals`/`GetHashCode` by
  concrete type + id.
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
  `GetRequiredService` and call `Handle`. No pipeline behaviours, no decorators.
  **⚠ Target:** handlers will be discovered by Scrutor assembly scanning instead of the current manual
  registration list — `R-01` in [roadmap.md](roadmap.md), ADR-008.
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
  runtime rather than compile time) is removed by ADR-008 rather than by adopting MediatR.

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

- **Status:** accepted 2026-07-26, **not yet implemented** (`R-01` in [roadmap.md](roadmap.md)).
- **Context:** ADR-001's hand-rolled CQRS requires every handler to be listed in
  `ServiceInitializer.RegisterCqrsHandlers`. A missing line compiles cleanly and throws at runtime on
  `GetRequiredService`. This is the most likely bug class in the backend and it scales with every new use case.
- **Decision:** discover `ICommandHandler<,>` and `IQueryHandler<,>` implementations by assembly scanning with
  Scrutor — **already a dependency**, currently used only for `Decorate`. Delete `RegisterCqrsHandlers`.
- **Consequences:** new handlers need no DI change, removing the failure mode entirely, at no dependency cost.
  Registration becomes implicit, so a container-resolution test is required to keep it verifiable. Explicitly
  chosen over adopting MediatR, which would add a commercially-licensed dependency to solve the same problem.

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

---

These ADRs were **reconstructed from code and commit messages** — no ADR files existed before, so ADR-001
through ADR-007 are documentation of decisions already made, while ADR-008 and ADR-009 are new decisions taken
during the documentation review. Whether to split them into individual files under `docs/adr/` is open:
`DEC-08` in [known-issues.md](known-issues.md).
