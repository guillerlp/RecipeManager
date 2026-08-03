# Roadmap — planned work

Work that is **decided but not yet built**. Distinct from [known-issues.md](known-issues.md), which lists things
that are *wrong* with what already exists.

> **Project stance.** RecipeManager started as a practice ground for architecture and tooling patterns
> (Clean Architecture, CQRS, caching decorators, `Result`-based error handling, EF migrations, .NET 10 +
> PostgreSQL), and may be deployed for real if it turns into something worth deploying.
>
> Both halves of that matter for how these documents are written:
> - **Because it is practice**, the bar is the production-grade version of each pattern, not the shortest thing
>   that compiles. Docs describe the *right* way even where the code has not caught up.
> - **Because deployment is possible**, the items under [Deploy gate](#deploy-gate) are not optional
>   nice-to-haves. They are the list that must be closed *before* the app is exposed publicly, and nothing on it
>   should be quietly downgraded.
>
> Where current code and the target differ, the docs say so explicitly and point here. Never silently document
> the target as if it were reality.

**Rules for agents**

- Items are `R-nn`. Never reuse an ID.
- Picking one up means writing a spec first ([specs/_template.md](specs/_template.md)) and getting
  `01-architect` sign-off where the item says so.
- Completing one means deleting its entry, updating the docs it changes, and recording the ADR.
- Do not start a **Phase 3** item while **Phase 1** items are open unless the user asks — the ordering is
  deliberate.

---

## Phase 1 — foundations (do these first)

These are cheap, unblock everything else, and each one removes a whole class of future bug.

### R-02
**Treat warnings as errors, centralise project properties** · `02-senior-csharp` · ~1 h

`TargetFramework`, `Nullable`, and `ImplicitUsings` are duplicated across all six `.csproj` files, and nothing
prevents a warning from being committed. Add `RecipeManager/Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

**Blocked by** `BUILD-01` and `BUILD-02` in [known-issues.md](known-issues.md) — fix those 7 warnings first,
then turn this on so they can never come back.

### R-03
**Fix the frontend toolchain** · `03-senior-react` · ~30 min

Three separate holes that together mean the frontend has *no* automated quality gate:

1. Add `jiti` to `devDependencies` so `npm run lint` can actually run (`BUILD-03`).
2. `"build": "tsc -b && vite build"` and add `"typecheck": "tsc --noEmit"` (`BUILD-04`).
3. Run the newly-working lint and fix what it reports — the result is currently unknown.

Everything else on the frontend roadmap depends on this.

### R-04
**CI pipeline** · `01-architect` → implementation · ~3 h

Nothing enforces any checklist today. `BUILD-03` is the proof: a check that nobody runs silently stops working.

Minimum GitHub Actions workflow on every PR:

```
dotnet build (warnings as errors after R-02) · dotnet test
npm ci · npm run typecheck · npm run lint · npm run build
dotnet list package --vulnerable --include-transitive · npm audit
```

Dependabot **alerting** is already on (68 open alerts today) but nothing acts on it. Add Dependabot
**pull requests** for both ecosystems and fail the build on high-severity alerts — that is what turns a
notification into a gate. Closes `INFRA-01` and stops `SEC-03` from silently regrowing.

---

## Phase 2 — correctness and confidence

### R-05
**Decouple domain errors from HTTP status codes** · `01-architect` + `02-senior-csharp` · ~3 h

`RecipeErrors` currently bakes HTTP semantics into the Domain layer:

```csharp
new Error("Title is required").WithCode(422).Field("title");
```

The Domain project should not know that HTTP exists — this is the one place the layering is violated. It also
causes a real bug: `ResultExtensions.CreateProblemDetails` takes the status from `errors.First()` only, so a
`Result` carrying a 404 and a 422 returns whichever happens to be first.

**Target.** Domain errors carry a semantic kind, and the API layer owns the mapping:

```csharp
// Domain
public enum ErrorKind { Validation, NotFound, Conflict }
// Api — ResultExtensions maps ErrorKind -> status, and picks the most severe, not the first
```

**Acceptance:** no `WithCode(<int>)` in `RecipeManager.Domain`; a `Result` with mixed error kinds returns the
correct status; existing status codes are unchanged for all current endpoints (covered by the integration
tests). Supersedes part of ADR-002 — record a new ADR.

### R-06
**Testcontainers for integration tests** · `06-qa-tester` + `01-architect` · ~4 h · **decided, deferred**

`IntegrationTestBase` uses EF InMemory, which the EF Core team explicitly recommends against for integration
testing. It cannot reproduce `text[]` semantics, PostgreSQL identifier folding, real constraint violations, or
concurrency, so `TEST-06` is unfixable while it stays.

**Target.** `Testcontainers.PostgreSql`, one container per test class, `RespawnDb` or a fresh database per
class for isolation. Requires Docker locally and in CI, so it should land **after** `R-04`.

Keep the existing 14 tests passing throughout — this is a swap of the base class, not a rewrite.

### R-07
**Frontend test runner and first tests** · `06-qa-tester` + `03-senior-react` · ~4 h

Zero frontend tests exist (`TEST-01`). **Vitest + React Testing Library + jsdom** — Vitest reuses
`vite.config.ts` aliases directly, so setup is minimal.

First tests, in priority order: `RecipeCard.formatDuration`/`getISODuration` boundaries; `RecipeList` filtering
and its four states; `ThemeContext` persistence and the `useTheme` guard; `NavLink` active-state matching.
Blocked by `R-03`.

### R-08
**Close the cache-invalidation test gap** · `06-qa-tester` · ~2 h

`TEST-02` — the largest gap in the backend suite. Unit tests mock `IRecipeRepository` and so bypass
`CachedRecipeRepository` entirely; integration tests assert database state rather than re-reading through the
API. A broken invalidation passes all 84 tests today.

Add integration tests that write and then **re-read through the HTTP client**: create → list contains it;
update → detail shows new values; delete → detail returns 404.

### R-09
**Generate TypeScript types from OpenAPI** · `08-api-contract` + `01-architect` · ~2 h

Nothing detects contract drift, which is exactly how `BUG-01`–`BUG-05` accumulated: the TS `Recipe` declares
`id: number` against a `Guid` API and is missing `servings` and `instructions`.

Add an `openapi-typescript` step producing a generated, committed types file, with a CI check that fails when
the generated output differs from what is committed. Fix `BUG-01`–`BUG-05` first (they are defects, not
roadmap), then make recurrence impossible.

---

## Phase 3 — domain evolution

### R-10
**Structured ingredients** · `01-architect` (ADR required) → `02-senior-csharp` → full stack · ~2–3 days · **decided**

The largest latent change in the model, and an acknowledged temporary shortcut. `Ingredients` is
`IReadOnlyList<string>` of free text, which makes all of these impossible: quantities, unit conversion, serving
scaling, shopping lists, and querying "recipes containing tomato" in SQL (the frontend filters the whole list
client-side instead).

**Target shape** — settle these in the ADR before any code:

- An `Ingredient` value object or entity: `Quantity` (decimal), `Unit`, `Name`, optional `Notes`.
- A `Unit` value object or enum covering metric and imperial, with an explicit conversion policy — decide
  whether conversion is a domain service or a presentation concern, and what the canonical stored unit is.
- Whether an ingredient **catalogue** exists (a shared `Ingredient` table enabling "what can I cook with X")
  or ingredients stay owned by their recipe.
- Migration for existing rows: `text[]` free text cannot be parsed reliably into structured data. Decide
  between a best-effort parse, a nullable structured column alongside the text one, or accepting data loss.

**Knock-on effects to plan in the same ADR:** the recipe form becomes substantially more complex
(see [agents/07-ux-ui.md](agents/07-ux-ui.md)); serving-scaling becomes possible and will be requested;
`RecipeDto` changes, so `R-09` should land first.

Consider doing the same for `Instructions` (per-step duration, image, grouping) — decide together, implement
separately.

### R-11
**Pagination and server-side search** · `01-architect` → `02-senior-csharp` + `03-senior-react` · ~1 day

`GET /api/recipes` returns the entire table, unpaginated, mapped in full, cached under a single `IMemoryCache`
key (`SEC-07`), and the SPA filters it in the browser. This is fine at 20 recipes and untenable at 2,000.

Design the pagination contract and the cache-key strategy **together** — paginating invalidates the current
single-key `recipes_all` approach. PostgreSQL `text[]` is queryable, so ingredient search can move server-side
here even before `R-10`.

### R-12
**Recipe images** · `01-architect` + `05-security-reviewer` (both required) → full stack · ~1 day

`RecipeDto` has no image field, but `recipe-manager-frontend/src/types/recipe.ts` declares an unused `image?: string` and every card
falls back to a 2.1 MB bundled placeholder (`BUG-04`, `BUILD-05`).

**Do not start without the security requirements** in
[agents/05-security-reviewer.md](agents/05-security-reviewer.md#recipe-image-upload--none-exists-yet-requirements-if-one-is-added):
content validation by magic bytes, server-side re-encode, size and rate limits, format allow-list, generated
filenames, storage outside the web root, EXIF stripping.

Decide first whether images are uploaded or referenced by URL — a URL field is a fraction of the work and may
be enough.

### R-13
**Development-only data seeder** · `02-senior-csharp` · ~2 h

A fresh clone shows an empty app until recipes are created by hand through Swagger (`INFRA-05`). A
Development-environment-only seeder with a handful of realistic recipes improves first-run experience and manual
testing. Must be gated on `app.Environment.IsDevelopment()` and must never run in production.

---

## Deploy gate

**Nothing here is optional if the app is exposed publicly.** These are not "someday" items — they are the
definition of ready-to-deploy. Re-read this list before the first deployment.

| Must be true | Tracked as |
| --- | --- |
| Authentication exists and every write endpoint requires it | `SEC-01` |
| Recipes have an owner, and authorization is enforced in the query, not the UI | `SEC-02` |
| npm vulnerabilities resolved (currently 68 open Dependabot alerts, 32 high; `axios` is 29 of them) | `SEC-03` |
| Rate limiting on write endpoints | `SEC-04` |
| Exception messages no longer returned to clients | `SEC-05`, `SEC-06` |
| Security headers and HSTS enabled | `SEC-10` |
| Length limits enforced at the database, not only in FluentValidation | `SEC-08`, `SEC-09` |
| `GET /api/recipes` paginated | `SEC-07`, `R-11` |
| Health/readiness endpoint | `SEC-11` |
| CI green on every PR | `INFRA-01`, `R-04` |
| Versioning scheme and a tested rollback procedure — migrations apply themselves at startup | `INFRA-02`, `INFRA-03` |
| Real production `VITE_API_URL` and a documented frontend host | `INFRA-04`, `SEC-12` |

`R-14` **Authentication and ownership** · `01-architect` + `05-security-reviewer` · ~3–5 days — the largest
single item on this list. Requires choosing the identity source (ASP.NET Core Identity vs. an external IdP),
adding a `User` aggregate, adding `OwnerId` to `Recipe` with a migration for existing rows, filtering every
query, and wiring auth through the SPA. Do not start it as a side effect of another feature.

---

## Deliberately not planned

Recorded so they are not repeatedly re-proposed. Revisit only if the stated reason stops holding.

- **MediatR.** The hand-rolled CQRS is intentional (ADR-001, commit `05656ed`). `R-01` removes its only real
  drawback. Reconsider only if pipeline behaviours become genuinely necessary.
- **AutoMapper.** One hand-written mapping extension is clearer and faster than a mapping configuration.
- **A global frontend store (Redux/Zustand).** TanStack Query owns server state and Context owns UI state;
  there is no client state that needs either.
- **A MUI `ThemeProvider`.** The app themes via `data-theme` plus CSS variables. Adopting MUI theming would
  mean maintaining two token systems.
- **Distributed cache.** `IMemoryCache` behind `ICacheService` is correct for a single instance. The port
  already exists, so swapping `MemoryCacheService` is the only change needed if the app ever scales out.
