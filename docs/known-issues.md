# Known issues & backlog

Every **defect and gap in what already exists**. Planned work that does not exist yet lives in
[roadmap.md](roadmap.md); the two files cross-reference each other.

Verified against `main` @ `edfd057` on 2026-07-26 by running the real toolchain — not by reading code. Build and
test numbers re-measured on 2026-08-04 after `R-02`, and the frontend rows re-measured on 2026-08-08 after `R-03`
and again after the `SEC-03` dependency remediation. The npm audit row re-measured 2026-09-12. The frontend
tests row was added 2026-09-18 after `R-07`. Backend test numbers re-measured 2026-09-19 after `R-09` (ADR-019),
from CI run 35438379053, which shows 85 + 22 passed, 0 skipped.

> **Rules for agents**
> - Do not leave inline TODO markers scattered in the docs or the code. Add an entry here instead.
> - When you fix something, delete its entry in the same PR and update whichever `docs/` file described it.
> - Add new findings with the next free ID in the relevant category. Never reuse an ID.
> - "Severity" assumes the app is exposed publicly. This project is practice-with-deployment-intent
>   (see [roadmap.md](roadmap.md)), so Critical items are **not** dismissed — they are sequenced behind the
>   [Deploy gate](roadmap.md#deploy-gate) and must all be closed before the app is exposed.

---

## Measured baseline

| Check | Command | Result |
| --- | --- | --- |
| Backend build | `dotnet build RecipeManager.sln` | 0 errors, **0 warnings** — enforced by `TreatWarningsAsErrors` (ADR-010) |
| Backend tests | `dotnet test RecipeManager.sln` | **107 passing** (85 unit + 22 integration), 0 failing. Of the 22, 18 need Docker and are reported as skipped without it (ADR-017); the other 4 (`OpenApiContractTests`, ADR-019) need no Docker, but on a Windows machine under Smart App Control (`INFRA-06`) they **fail** with `FileLoadException` rather than skip |
| NuGet vulnerabilities | `dotnet list package --vulnerable --include-transitive` | **none**, all six projects clean |
| Frontend type-check | `npm run typecheck` | **0 errors** |
| Frontend build | `npm run build` | succeeds, and type-checks `src/` and `vite.config.ts` first (`tsc -b tsconfig.json tsconfig.node.json && vite build`, ADR-012, `BUILD-10`) |
| Frontend lint | `npm run lint` | **0 problems** — Oxlint, 159 rules: the 71 type-aware ones on `src/**` plus the `correctness` category everywhere (ADR-016; ESLint until then, ADR-012) |
| npm vulnerabilities | `npm audit --audit-level=high` | **0** — re-cleared 2026-09-12 by `npm audit fix` after two new transitive dev-only advisories surfaced post-`SEC-03` (`GHSA-2883-xcg3-v3hh`, `GHSA-p498-v437-472g`). A clean audit expires: it is a claim about the advisory database on the day it ran, not a property of the lock file (`SEC-03`, [Settled](#settled)). |
| Frontend tests | `npm test` | 40 pass — Vitest + RTL under jsdom (ADR-018) |
| CI | `.github/workflows/ci.yml` | runs every row above on each PR (ADR-013, `R-04`). Not yet *required* to merge — [INFRA-07](#infra-07) |

**Zero warnings across every backend project, enforced.** `RecipeManager/Directory.Build.props` sets
`TreatWarningsAsErrors`, so a warning is a build failure rather than a note somebody may or may not read
(ADR-010, `R-02`). The seven warnings that existed until then — introduced by the .NET 10 package upgrades, not
present on the .NET 8 set — were `BUILD-01` and `BUILD-02`, both fixed in the same PR.

**The frontend now has the equivalent gate**, closed by `R-03`/ADR-012 on 2026-08-08: `jiti` makes
`eslint.config.ts` loadable, `npm run build` runs `tsc -b` before Vite, and `npm run typecheck` exists. Both
gates were verified by negative test — a deliberate type error fails the build before Vite runs, and a
deliberate `console.log` is reported by lint. What this does **not** buy is automatic enforcement: nothing runs
these commands for you — **until `R-04`, which now does**: `.github/workflows/ci.yml` runs both gates on every
PR (ADR-013). The remaining gap is that a red run does not yet *block* a merge ([INFRA-07](#infra-07)).
ADR-016 later replaced ESLint with Oxlint (and `jiti` went with it); the lint gate was re-verified by the same
kind of negative test.

---

## Summary

| ID | Severity | Area | Issue |
| --- | --- | --- | --- |
| [BUILD-05](#build-05) | Medium | Perf | `mainPhoto.png` is 2.1 MB — 6× the entire JS bundle |
| [BUILD-06](#build-06) | Low | Tooling | `run-coverage.ps1` measures only the unit-test project |
| [SEC-01](#sec-01) | **Critical** | Security | No authentication at all |
| [SEC-02](#sec-02) | **Critical** | Security | No authorization / no recipe ownership |
| [SEC-04](#sec-04) | **High** | Security | No rate limiting on unauthenticated write endpoints |
| [SEC-05](#sec-05) | **High** | Security | Exception messages returned to the client on 500s |
| [SEC-06](#sec-06) | Medium | Security | `DeleteRecipeHandler` echoes `ex.Message` to the client |
| [SEC-07](#sec-07) | Medium | Security | `GET /api/recipes` is unbounded and cached whole |
| [SEC-08](#sec-08) | Medium | Security | No length limits in the database |
| [SEC-09](#sec-09) | Medium | Security | No per-item length cap on ingredient/instruction strings |
| [SEC-10](#sec-10) | Medium | Security | No security headers, no HSTS |
| [SEC-11](#sec-11) | Low | Ops | No health/readiness endpoint |
| [SEC-12](#sec-12) | Low | Config | `.env.production` points at a placeholder host |
| [BUG-06](#bug-06) | Medium | Frontend | `/recipes/new` is linked but has no route |
| [BUG-07](#bug-07) | Low | API | `GET /api/recipes/{id}` missing the `:guid` route constraint |
| [BUG-09](#bug-09) | Low | Frontend | Error recovery does a full page reload |
| [BUG-10](#bug-10) | Medium | Frontend | No recipe detail route, so cards are not clickable |
| [BUG-11](#bug-11) | Low | Domain | Recipes loaded from the database expose a mutable `List<string>` ([#8](https://github.com/guillerlp/RecipeManager/issues/8)) |
| [BUG-12](#bug-12) | Low | Frontend | A paused recipe query renders "No recipes available" |
| [BUG-13](#bug-13) | Low | Frontend | Whitespace-only search query shows a misleading "matching" heading |
| [BUG-14](#bug-14) | Medium | Caching | An update mutates the cached `Recipe` instance before it is persisted |
| [TEST-03](#test-03) | Medium | Tests | Instruction ordering never asserted |
| [TEST-04](#test-04) | Low | Tests | `Location` header on 201 never asserted |
| [TEST-05](#test-05) | Low | Tests | No agreed coverage threshold |
| [INFRA-07](#infra-07) | Medium | CI/CD | CI runs on every PR but is not yet *required* to merge |
| [INFRA-02](#infra-02) | Medium | CI/CD | No versioning or tags |
| [INFRA-03](#infra-03) | Medium | CI/CD | No rollback procedure |
| [INFRA-04](#infra-04) | Medium | CI/CD | No frontend deployment target |
| [INFRA-08](#infra-08) | Medium | CI/CD | Dependabot NuGet PRs arrive red: lock files only partly updated (NU1004) |
| [INFRA-05](#infra-05) | Low | DX | No seed data |
| [QUAL-01](#qual-01) | Low | Quality | `ILogger` called with interpolated strings |
| [QUAL-02](#qual-02) | Low | Quality | `Console.WriteLine` used for startup logging |
| [QUAL-03](#qual-03) | Low | Quality | Deep relative imports for shared assets |
| [QUAL-04](#qual-04) | Low | Quality | Routes, verbs, and status codes are still hand-typed on the client |
| [QUAL-05](#qual-05) | Low | Quality | Frontend indentation is mixed and no formatter enforces it |
| [UX-01](#ux-01) | Medium | UX | Dark-theme status colours never contrast-checked |
| [UX-02](#ux-02) | Medium | UX | Global heading sizes ignore the type scale; `h3` clips descenders |
| [UX-03](#ux-03) | Low | UX | No shared breakpoint tokens |
| [DEC-03](#dec-03) | — | Decision | `Ardalis.GuardClauses` is referenced but unused |
| [DEC-04](#dec-04) | — | Decision | `UseErrorHandler` position in the pipeline |
| [DEC-06](#dec-06) | — | Decision | Follow OS colour-scheme preference on first visit? |
| [DEC-07](#dec-07) | — | Decision | 24 h cap excludes slow-cooked and fermented recipes |
| [DEC-08](#dec-08) | — | Decision | Split the reconstructed ADRs into individual files? |

Resolved decisions and items promoted to planned work are recorded in [Settled](#settled) at the end.

---

## Build & tooling

### BUILD-05
**`mainPhoto.png` is 2.1 MB — Medium**

Production build output (2026-09-16, React 19.3 on Vite 8):

```
dist/assets/mainPhoto-DpyCXFCt.png   2,115.47 kB
dist/assets/index-DnV4jc7g.js          353.62 kB │ gzip: 113.93 kB
dist/assets/index-D9pZeVVo.css          12.02 kB │ gzip:   2.97 kB
```

The image is **6× larger than all JavaScript combined** and is shipped unoptimized. It is used twice — as the
hero image on `HomePage` and as the fallback thumbnail in every `RecipeCard`, so a list of 20 recipes references
a 2 MB asset 20 times (cached, but decoded at full resolution each time).

**Fix.** Re-encode to WebP/AVIF at the sizes actually rendered, and ship a separate small placeholder for the
card fallback rather than reusing the hero image. Consider `srcset` for the hero.

**Owner:** `03-senior-react` + `07-ux-ui` · **Effort:** ~1 h

### BUILD-06
**`run-coverage.ps1` measures only the unit-test project — Low**

The script runs `dotnet test RecipeManager.UnitTests` and reports on that alone, so the 22 integration tests
contribute nothing and the reported percentage understates real coverage — particularly for `Api` and
`Infrastructure`, which unit tests never touch.

**Fix.** Point it at `RecipeManager.sln` and merge both coverage outputs, or rename the script and state the
limitation in `README.md`.

**Owner:** `06-qa-tester` · **Effort:** ~30 min

---

## Security

Full context and the standing posture table live in
[agents/05-security-reviewer.md](agents/05-security-reviewer.md).

### SEC-01
**No authentication — Critical (if deployed publicly)**

`ApplicationInitializer.ConfigurePipeline` calls `app.UseAuthorization()` **without** `UseAuthentication()`.
There is no `[Authorize]` attribute anywhere, no identity provider, and no `User` entity. Every endpoint is
anonymous.

On the [deploy gate](roadmap.md#deploy-gate) — must be closed before public exposure. Planned as `R-14`; requires `01-architect` to choose an identity approach before any code.

### SEC-02
**No authorization / no ownership — Critical (if deployed publicly)**

No `User` entity and no `OwnerId` on `Recipe`. Any caller who knows a `Guid` can `PUT` or `DELETE` that recipe.
Guid ids prevent enumeration but are not an access control.

On the [deploy gate](roadmap.md#deploy-gate). Depends on [SEC-01](#sec-01); planned as `R-14`.

### SEC-04
**No rate limiting — High**

`AddRateLimiter` is never called. `POST /api/recipes` is an unauthenticated write endpoint with no throttle,
and each write invalidates the cache — so a write flood also degrades read performance.

**Owner:** `01-architect` (design) → `02-senior-csharp`

### SEC-05
**Exception messages returned to the client — High**

`RecipeManager.Api/Middlewares/ErrorHandlerMiddleware.cs` sets, for **every** unhandled exception including 500s:

```csharp
Type   = exception.GetType().Name,
Detail = exception.Message,
```

An Npgsql failure therefore returns connection details, host names, or SQL fragments to the caller.

**Fix.** Log the full exception server-side; return a generic `Detail` for 500s. Keep specific messages only for
the deliberately mapped 400/401/404 cases, and only where the message is known to be safe.

**Owner:** `02-senior-csharp` · **Effort:** ~20 min

### SEC-06
**`DeleteRecipeHandler` echoes `ex.Message` — Medium**

```csharp
return Result.Fail("Error while deleting the recipe").WithError(ex.Message);
```

The nested error reaches the client through `ResultExtensions.CreateProblemDetails`. Same class of leak as
[SEC-05](#sec-05). The `_logger.LogError(ex, …)` call above it already captures what is needed.

**Fix.** Drop `.WithError(ex.Message)`.

When fixing this, also decide the status: the error is not a `DomainError`, so `ResultExtensions` maps it to
400 and ranks it above every domain kind (ADR-009). An unexpected persistence failure is arguably a 500; that
change was deliberately left out of `R-05`, which had to keep every status unchanged.

**Owner:** `02-senior-csharp` · **Effort:** ~5 min

### SEC-07
**`GET /api/recipes` is unbounded — Medium**

Returns the whole table with no pagination, maps every row, and caches the entire list under one `IMemoryCache`
key. Memory grows linearly with the recipe count with no ceiling. Pagination is planned as `R-11` and is on the [deploy gate](roadmap.md#deploy-gate).

### SEC-08
**No length limits in the database — Medium**

`Title` and `Description` are unbounded `text`; `Ingredients`/`Instructions` are `text[]`. The 200/1000-character
caps exist **only** in FluentValidation, so anything writing outside the API — a future bulk import, a direct
psql session, a second service — can store unbounded values.

**Fix.** Add `HasMaxLength` in an `IEntityTypeConfiguration` and a migration. Note that adding
`OnModelCreating`/entity configuration is itself a small architecture change ([architecture.md](architecture.md)
records that none exists today).

**Owner:** `01-architect` → `02-senior-csharp`

### SEC-09
**No per-item length cap on array elements — Medium**

`RecipeValidationRules` caps the ingredient and instruction **lists** at 50 items but never limits the length of
each string. A single 10 MB ingredient string passes validation.

**Fix.** Add `.ForEach(item => item.MaximumLength(<n>))` to `ValidateIngredients` / `ValidateInstructions`.

**Owner:** `02-senior-csharp` · **Effort:** ~15 min

### SEC-10
**No security headers — Medium**

`UseHsts()` is not called. No CSP, no `X-Content-Type-Options: nosniff`, no `Referrer-Policy`.
`UseHttpsRedirection()` is present, which is good but not sufficient.

**Owner:** `01-architect` → `02-senior-csharp`

### SEC-11
**No health or readiness endpoint — Low**

`AddHealthChecks` is never called. Since the API refuses to start when PostgreSQL is unreachable, a container
orchestrator has no way to distinguish "starting" from "dead" other than by probing a real endpoint.

### SEC-12
**`.env.production` points at a placeholder — Low**

`VITE_API_URL=https://your-production-api.com/api`. A production build would ship pointing at a domain the
project does not control. See [INFRA-04](#infra-04).

Observed 2026-09-16: a plain `npm run build` served with `vite preview` requested
`https://your-production-api.com/api/Recipes` (the name did not resolve: `ERR_NAME_NOT_RESOLVED`), and the page
showed "No recipes available" instead of an error — the empty-state defect recorded as `BUG-12`. So the
placeholder is not inert: every production build would send its API traffic, recipe writes included, to whoever
registers that name, and a local production check silently shows an empty catalogue. For local checks, set
`VITE_API_URL=/api` in the environment; Vite ranks real environment variables above `.env.production`.

**Fix.** Set the real value, or delete the file so the relative `/api` fallback applies.

---

## Contract & functional defects

Full detail on the contract seam is in [agents/08-api-contract.md](agents/08-api-contract.md).

### BUG-06
**`/recipes/new` is linked but has no route — Medium**

`pages/Home/HomePage.tsx` renders `<NavLink to='/recipes/new'>Add Recipe</NavLink>`, but `App.tsx` defines only
`/`, `/recipes`, and `/profile`. Clicking "Add Recipe" navigates to a blank page — no route, no 404 fallback.

**Fix.** Build the create-recipe screen (see the form requirements in
[agents/07-ux-ui.md](agents/07-ux-ui.md)), or remove the link. Adding a catch-all `*` route with a NotFound page
is worthwhile regardless.

### BUG-07
**`GET /api/recipes/{id}` missing the `:guid` constraint — Low**

`[HttpGet("{id}")]` while `[HttpPut("{id:guid}")]` and `[HttpDelete("{id:guid}")]` are constrained. A malformed
id reaches model binding instead of being rejected by routing, producing an inconsistent error shape.

### BUG-09
**Error recovery does a full page reload — Low**

`RecipeList`'s retry button calls `window.location.reload()`, discarding all client state. TanStack Query's
`refetch()` is already available from `useRecipes`.

### BUG-10
**No recipe detail route, so cards are not clickable — Medium**

`App.tsx` routes only `/`, `/recipes`, and `/profile`. There is no `/recipes/:id` screen, so there is nothing
for a `RecipeCard` click to navigate to.

Until `R-03`, `RecipeList` passed an `onClick` whose entire body was `console.log('Clicked recipe:', …)`. That
made every card render as `<button aria-label="View {title} recipe">` — focusable, announced to a screen reader
as an action, and doing nothing when activated. Removing the debug log (`BUG-08`) left a no-op handler, so the
`onClick` was dropped and cards now render as `<article>`, which is what `RecipeCard`'s element switch is for.

**Fix.** Build the detail screen, add the `/recipes/:id` route, and pass `onClick` again — `RecipeCard` already
switches to `<button>` when it receives one.

**Owner:** `03-senior-react` + `07-ux-ui`

### BUG-11
**Recipes loaded from the database expose a mutable `List<string>` behind `IReadOnlyList<string>` — Low**

Tracked on GitHub as [#8](https://github.com/guillerlp/RecipeManager/issues/8).

`Recipe.Ingredients` and `Recipe.Instructions` are auto-properties with a `private set`
(`RecipeManager.Domain/Entities/Recipe.cs:14-15`). `Recipe.Create` and `Recipe.Update` assign
`ToList().AsReadOnly()`, which is genuinely read-only. EF Core, however, materializes a primitive collection as a
`List<string>` and assigns it straight through the private setter. Nothing in `AppDbContext` redirects it. So the
same entity is protected when constructed and unprotected when read:

```
[construct] System.Collections.ObjectModel.ReadOnlyCollection`1[System.String]
[read]      System.Collections.Generic.List`1[System.String]
[cast]      castable to List<string>: True
```

Reproduced 2026-09-13 against local PostgreSQL on `main` @ `84226dc` (EF Core 10.0.12, Npgsql 10.0.3) with a
throwaway probe that created, re-read, and deleted one recipe. That confirms the July finding in #8 still holds.

**Consequences.**
- `((List<string>)recipe.Ingredients).Add(…)` compiles and bypasses the validation in `Recipe.Update`, which
  breaks the rule that the domain owns every invariant (CLAUDE.md global rule 5). On a tracked entity the
  change persists.
- **The cache makes it worse (found while verifying #8, not stated in it).** Both repository reads use
  `AsNoTracking()`, and `MemoryCacheService` stores that same instance. A mutated cached recipe therefore never
  reaches the database. It is served to every later request until the entry expires. The safety argument in
  [architecture.md → Caching](architecture.md#caching) relied on these collections being read-only.
- The `02-senior-csharp` checklist item "assigned with `.ToList().AsReadOnly()`" covers the construction path
  only. Following it does not prevent this defect.

**Severity.** Low: nothing reachable over HTTP triggers it, since it needs a deliberate downcast in this repo's
own code. It is recorded because it silently falsifies an encapsulation guarantee two documents relied on.

**Fix (as proposed in #8).** Private backing fields, with the properties exposing a read-only view:

```csharp
private readonly List<string> _ingredients = new();
public IReadOnlyList<string> Ingredients => _ingredients.AsReadOnly();
```

EF is then pointed at the fields (`UsePropertyAccessMode(PropertyAccessMode.Field)` in
`AppDbContext.OnModelCreating`). The issue also sets two constraints. They are recorded here as its author's
reasoning and were not re-verified for this entry:
- no `ValueConverter` on these collections, which would likely collapse the native `text[]` mapping into a
  serialized scalar;
- no switch to `string[]`, because an array behind `IReadOnlyList<string>` is still castable and writable.

**Verification must use real PostgreSQL.** The integration tests now run against one (ADR-017), so the
regression test for this can finally be written there. Also check that a schema diff produces no migration,
since the column should not change.

**Owner:** `02-senior-csharp` (fix) · `01-architect` if the property-access-mode choice needs an ADR

### BUG-12
**A paused recipe query renders "No recipes available" — Low**

Found 2026-09-16 while smoke-testing React 19.3. `RecipeList` shows its loading state only for `isLoading`, which
TanStack Query v5 defines as `isPending && isFetching`. A query that is pending but **paused** — no data, no error,
`fetchStatus: 'paused'` — is neither loading nor errored, so it falls through to the empty state and tells the user
they have no recipes. Observed directly by reading the query result from the component: `status: 'pending'`,
`fetchStatus: 'paused'`, `failureCount: 1`, page text "No recipes available", with the API stopped.

React Query pauses a query when it believes the browser is offline (from the first fetch) and pauses retries while
the page is hidden. The observation above was the second case, in a background tab, which resumes once the tab is
visible. The first case is the real exposure: a user who opens the page offline is told the collection is empty.
The visible-page error branch itself was not observed in this test.

**Fix.** Gate the loading state on `isPending` rather than `isLoading`, and consider a distinct message when
`fetchStatus === 'paused'` ("You appear to be offline"). The same check belongs in any future list that reuses the
pattern.

**Owner:** `03-senior-react` · **Effort:** ~15 min

### BUG-13
**Whitespace-only search query shows a misleading heading — Low**

`RecipeList` trims the query before filtering, so `"   "` is treated as no filter and every recipe is shown —
but the render path tests the raw `searchQuery` for truthiness, so the list is headed
`Found N recipes matching "   "`. Found while writing `R-07`'s tests; deliberately not pinned by a test.
Fix: derive one trimmed query and use it for both the filter and the render branches.

### BUG-14
**An update mutates the cached `Recipe` instance before it is persisted — Medium**

`IMemoryCache` stores object references, not copies. `UpdateRecipeHandler` loads the recipe through
`GetByIdAsync`. On a cache hit, `CachedRecipeRepository` returns *the instance held in the `recipe_{id}` entry*,
and `Recipe.Update(...)` then mutates it in place before `UpdateAsync` saves anything. If `SaveChangesAsync`
throws, the exception skips `InvalidateRecipeRelatedCaches`, so `GET /api/recipes/{id}` serves values that were
never persisted until the entry expires (`CacheDuration`, up to 10 minutes). The same instance is also shared
across concurrent requests, because the cache is a singleton while the repository is scoped.

Found while designing `R-08` (spec 006). It is also why `RecipeCacheTests` cannot prove that `UpdateAsync`
removes `recipe_{id}`: the cached object already holds the new values, so the detail read passes either way.

**Fix.** Never hand out a cached entity for mutation. Either load the entity for writes from the undecorated
repository, or cache an immutable read model (for example `RecipeDto`) instead of the entity. Then tighten the
detail assertion in `RecipeCacheTests.UpdateRecipe_AfterListAndDetailWereCached_ShouldReturnNewValuesFromBoth`
so it detects a missing `recipe_{id}` invalidation.

---

## Testing gaps

### TEST-03
**Instruction ordering never asserted — Medium**

Instructions are an ordered `text[]` and order is semantically essential, but every assertion uses
`BeEquivalentTo`, which is order-**insensitive**. A bug that reversed or shuffled steps would pass.

**Fix.** Use `Should().Equal(...)` where order is the property under test.

### TEST-04
**`Location` header never asserted — Low**

`POST /api/recipes` returns 201 via `ToCreatedAtActionResult(nameof(Get), new { id = … })`. Tests check the
status code and body but never the `Location` header, so a wrong action name or route value would go unnoticed.

### TEST-05
**No agreed coverage threshold — Low**

`run-coverage.ps1` produces a report but no number has ever been agreed or enforced. A sensible starting point
is line coverage on `Domain` + `Application` handlers, since `Api` and `Infrastructure` are thin. Needs a
decision from the user.

---

## Infrastructure & process

### INFRA-07
**CI checks are not yet *required* to merge — Medium**

`.github/workflows/ci.yml` exists and runs on every PR (ADR-013, `R-04`), but nothing makes a red run block a
merge. Requiring a status check is a **branch-protection rule on `main`** — a GitHub repository setting, not a
file in the repo, so it could not be delivered by the PR that added the workflow.

Until it is enabled, the pipeline is advisory: it reports honestly and can be merged past. That is a strictly
better position than no CI, and strictly worse than the item's goal.

**Fix.** Repository → Settings → Branches → add a rule for `main` requiring the `Backend` and `Frontend` checks,
and requiring branches to be up to date before merging. Confirm Dependabot **pull requests** are enabled in the
same visit — `dependabot.yml` configures them, but the repository-level toggle governs.

**Owner:** repository owner (not automatable from within the repo) · **Effort:** ~5 min

### INFRA-02
**No versioning or tags — Medium**

No git tags, no `<Version>` in any `.csproj`, `package.json` pinned at `0.0.0`. Nothing identifies what is
deployed.

### INFRA-03
**No rollback procedure — Medium**

`app.MigrateDatabase()` applies migrations at startup, including in production. With no versioning and no
documented restore path, redeploying an older image against an already-migrated database is not guaranteed to
work. A destructive migration deploys itself with no gate.

### INFRA-04
**No frontend deployment target — Medium**

`npm run build` produces `dist/`, but nothing documents where it is hosted or how `VITE_API_URL` is set in
production. See [SEC-12](#sec-12).

### INFRA-08
**Dependabot NuGet PRs arrive red: lock files only partly updated (NU1004) — Medium**

ADR-013 accepted that "every dependency change must regenerate the lock files or CI fails at `--locked-mode`".
What it did not anticipate is that Dependabot regenerates them **incompletely**. It updates some
`"type": "CentralTransitive"` entries and leaves others on the old version, so restore fails before build or
test ever run:

```
error NU1004: Mistmatch between the requestedVersion of a lock file dependency marked as CentralTransitive and
the the version specified in the central package management file.
```

Observed twice. #21 (FluentResults, an *ungrouped* PR) was missing entries in four lock files; #26 (the
minor/patch group) was missing two entries in `RecipeManager.IntegrationTests/packages.lock.json` only. So this
is not caused by grouping. Which entries Dependabot misses is **not fully characterised**: #26 bumped
`Microsoft.EntityFrameworkCore.Relational`, which is also `CentralTransitive` in two lock files, and those
entries *were* updated correctly.

**Exposure.** 11 of the 12 production packages in `RecipeManager/Directory.Packages.props` appear as
`CentralTransitive` somewhere (all except `Microsoft.EntityFrameworkCore.Design`). None of the 9 test-only
packages do, which is why #24 (`xunit.runner.visualstudio`) passed untouched. The packages that matter most are
exactly the ones affected. It follows that a Dependabot *security* update to one of them would arrive red too.
That is inferred from the mechanism; it has not been observed.

**Why it matters.**
- A red check on these PRs carries **no compatibility signal**: restore failed, so nothing was compiled or
  tested. #21 looked like a breaking upgrade and was actually unevaluated.
- The NU1004 message recommends "Disable the RestoreLockedMode MSBuild property". That advice would silently
  undo ADR-013. Do not follow it.
- The fix is a manual push to Dependabot's branch. Per Dependabot's own PR text, editing the branch stops it
  resolving conflicts automatically, so each fixed PR becomes a human's to maintain.
- The comment in `.github/dependabot.yml` ("Each PR must also update the six packages.lock.json files or CI
  fails at --locked-mode; that is the intended behaviour") is accurate about the gate. It reads as though
  Dependabot does that update in full, which it does not.

**Workaround (current).** On the PR branch, from `RecipeManager/`:
`dotnet restore RecipeManager.sln --force-evaluate`. Commit only the `packages.lock.json` changes. Confirm with
`dotnet restore RecipeManager.sln --locked-mode` before pushing. Check the result against CI on the PR's head SHA.

**Fix options.**
- *(a)* Document the workaround in [workflows/release-workflow.md](workflows/release-workflow.md) and correct
  the `dependabot.yml` comment. Cheap, and the per-PR manual step remains.
- *(b)* A workflow that regenerates the lock files on Dependabot PRs and pushes the result. It removes the manual
  step, but it needs write access from a Dependabot-triggered run, which GitHub restricts by default. The obvious
  route (`pull_request_target` checking out the PR head) is a known code-injection pattern. Needs
  `05-security-reviewer` before it is built. Verify GitHub's current Dependabot token rules at that time rather
  than from memory.
- *(c)* Drop lock files or `--locked-mode`. **Rejected**: this reverses ADR-013 and reopens the silent
  transitive-divergence failure it closed.

Recommended: *(a)* now; *(b)* only if the manual step proves frequent enough to justify the security surface.

**Owner:** `01-architect` (CI structure) · **Effort:** *(a)* ~15 min; *(b)* not estimated

### INFRA-05
**No seed data — Low**

The database starts empty and there is no seeder, so a fresh clone shows an empty app until recipes are created
by hand through Swagger. Integration tests seed only into their own throwaway container database. A
Development-only seeder is planned as `R-13`.

---

## Code quality

### QUAL-01
**`ILogger` called with interpolated strings — Low**

`RecipeManager.Api/Controllers/RecipesController.cs` uses `_logger.LogInformation($"Fetching recipe with ID {id}...")`. This
defeats structured logging: the value is baked into the message string and cannot be queried as a field.

**Fix.** Message templates: `_logger.LogInformation("Fetching recipe {RecipeId}", id)`. Other files
(`DeleteRecipeHandler`, `CachedRecipeRepository`) already do this correctly.

### QUAL-02
**`Console.WriteLine` for startup logging — Low**

`MigrateDatabase` in `RecipeManager.Api/Startup/ApplicationInitializer.cs` writes migration progress with `Console.WriteLine`,
bypassing the logging pipeline, log levels, and any structured sink.

### QUAL-03
**Deep relative imports for shared assets — Low**

`RecipeCard.tsx` uses `import Logo from '../../../../assets/mainPhoto.png'` and `HomePage.tsx` uses
`'../../assets/mainPhoto.png'`, while every other import in the codebase uses the `@/` aliases. Add an
`@assets` alias (to **both** `vite.config.ts` and `tsconfig.json`) or use `@/assets`.

### QUAL-04
**Routes, verbs, and status codes are still hand-typed on the client — Low**

ADR-019 generates the DTO *shapes*, but `recipe-manager-frontend/src/services/recipeService.ts` still hand-writes each path
(`/Recipes/${id}`), verb, and response wrapper (`AxiosResponse<void>` for 204s). A renamed route or a changed
status code passes every check and fails at runtime.

**Fix.** Type the service against the generated `paths` interface, or adopt `openapi-fetch` (rejected for R-09 as
out of scope, see ADR-019).

### QUAL-05
**Frontend indentation is mixed and no formatter enforces it — Low**

`/.editorconfig` (ADR-020) sets `indent_size = 2` for TS/TSX/CSS/JSON, but that only guides editors. Oxlint does
not format, and no formatter runs. Measured 2026-09-19: of the files under `recipe-manager-frontend/src/`
(generated files excluded), 24 indent with 2 spaces and 14 with 4, so diffs will keep mixing both.

**Fix.** Adopt a formatter (Oxc's, to match Oxlint per ADR-016, or Prettier) with a CI check, and reformat in its
own blame-ignored commit, the same pattern ADR-020 used for C#. A new dependency needs `01-architect` sign-off.

---

## UX & accessibility

### UX-01
**Dark-theme status colours never contrast-checked — Medium**

`--color-error` (`#ef4444`) and `--color-warning` (`#f59e0b`) are identical in `light.css` and `dark.css`, but
were only ever chosen against the light background. Against `--color-background: #111827` they have not been
verified for WCAG AA.

**Fix.** Measure both against their actual surfaces; adjust the dark values if they fail 4.5:1.

### UX-02
**Global heading sizes ignore the type scale — Medium**

`styles/globals.css` sets `h1 { font-size: 4.2em }`, `h2 { 3rem }`, `h3 { 1.7rem; line-height: 0.5 }`. These
bypass the `--font-size-*` tokens entirely, and `h3`'s `line-height: 0.5` is below 1 so descenders are clipped.

**Fix.** Reconcile with the token scale as one deliberate pass — it will shift every existing screen, so it
should not be done incidentally.

### UX-03
**No shared breakpoint tokens — Low**

Each CSS module defines its own media queries with ad-hoc values. Define `--breakpoint-*` tokens, or document
the standard breakpoints, before the next screen is built.

---

## Open decisions

These need a product answer before the related work can be scoped. Owner: `00-leader` to ask,
`01-architect` to record the outcome.

### DEC-03
**`Ardalis.GuardClauses` is referenced but unused**

`RecipeManager.Domain.csproj` references it; there is not a single `Guard.` call in the codebase. Either adopt
it in `Recipe`/`Entity` or drop the reference. Currently it is a dependency paying no rent.

### DEC-04
**`UseErrorHandler` position in the pipeline**

`ConfigurePipeline` order is `UseCors` → `UseHttpsRedirection` → `UseErrorHandler` → `UseRouting` →
`UseAuthorization` → `MapControllers`. Because the error handler sits before `UseRouting`, exceptions thrown in
routing or CORS are not wrapped into `ProblemDetails`. Confirm this is intentional.

### DEC-06
**Follow OS colour-scheme preference on first visit?**

`ThemeProvider` defaults to `light` and only reads `localStorage`. There is no `prefers-color-scheme` detection.

### DEC-07
**24 h cap excludes slow-cooked and fermented recipes**

`RecipeValidationRules` rejects `preparationTime` or `cookingTime` ≥ 1440 minutes. Sourdough, cold brew,
overnight marinades, and slow-cooker recipes legitimately exceed this. Confirm the cap is intended, or raise it.

### DEC-08
**Split the reconstructed ADRs into individual files?**

The seven ADRs in [architecture.md](architecture.md#decision-log-condensed-adrs) were reconstructed from code
and commit messages — no ADR files existed before. Confirm they are accurate, and decide whether the team wants
one file per ADR under `docs/adr/`.

---

## Settled

Decisions that were open and are now answered, kept so they are not re-litigated.

| Was | Outcome | Now tracked as |
| --- | --- | --- |
| `DEC-01` — local tool or deployed product? | **Practice project with deployment intent.** The production-grade bar applies; Critical security items are sequenced behind the deploy gate rather than waived. | [roadmap.md](roadmap.md) project stance + [Deploy gate](roadmap.md#deploy-gate) |
| `DEC-02` — Development-only seeder? | **Yes**, gated on `IsDevelopment()`. | `R-13` |
| `DEC-05` — generate TS types from OpenAPI? | **Yes**, after the contract defects are fixed by hand. **Shipped 2026-09-19.** | ADR-019 |
| Ingredients: keep free text or structure them? | **Structure them.** The `string[]` shape was an acknowledged temporary shortcut. | `R-10` |
| CQRS: hand-rolled or MediatR? | **Keep hand-rolled**, and remove its one real drawback by auto-registering handlers with Scrutor (already a dependency). **Shipped 2026-08-03.** | ADR-001, ADR-008 |
| Integration tests: EF InMemory or a real database? | **Testcontainers with real PostgreSQL.** Deferred until CI existed, since it needs Docker in both places; ADR-013 provided it. **Shipped 2026-09-17**, closing `TEST-06`: one container per test assembly, a database per test class, schema by `Database.Migrate()`, and a skip rather than a failure when Docker is missing. | ADR-017, `R-06` |
| `BUILD-01`, `BUILD-02` — 7 backend build warnings | **Fixed**, and made unrepeatable by `TreatWarningsAsErrors` in `Directory.Build.props`. **Shipped 2026-08-04.** | ADR-010 |
| `BUILD-09` — backend package versions in the docs had drifted from `Directory.Packages.props` | **Fixed**: every backend and test row in `docs/tech-stack.md` and the `CLAUDE.md` stack line re-checked against `Directory.Packages.props`, and the `Scrutor` row now describes the assembly scanning ADR-008 introduced. Recurrence is handled by process: release-workflow item 6 now covers Dependabot PRs. That is the weaker of the two proposed fixes (it reminds rather than removes the cause), chosen because exact versions in the docs are worth having while every dependency PR is superseded anyway. **Shipped 2026-09-16.** | release-workflow item 6 |
| Should warnings-as-errors be Release-only? | **No — every configuration.** There is no CI yet (`INFRA-01`), so a Release-only condition would enforce nothing. | ADR-010 |
| Package versions duplicated across `.csproj` files | **Central package management.** Ten packages were versioned in two projects each; drift resolved nearest-wins with no diagnostic. **Shipped 2026-08-04.** | ADR-011 |
| Should a NuGet advisory fail the local build? | **Yes, accepted.** `TreatWarningsAsErrors` elevates `NU1903`, delivering `R-04`'s vulnerability gate earlier. Escape hatch recorded in ADR-010 if it becomes obstructive. | ADR-010 |
| `RecipeManager.Api.csproj.user` committed | **Untracked**, and `*.user` added to `.gitignore` — it carried one developer's debug profile. Fixed 2026-08-04. | — |
| `BUILD-03` — `npm run lint` could not start | **Fixed** by adding `jiti`; ESLint 9 loads a TypeScript flat config through it. Unable to start since 2025-08-08. **Shipped 2026-08-08.** | ADR-012, `R-03` |
| `BUILD-04` — `npm run build` did not type-check | **Fixed**: `"build": "tsc -b && vite build"`, plus a `typecheck` script. **Shipped 2026-08-08.** | ADR-012, `R-03` |
| `BUILD-08` — `ts-node` was a devDependency nothing used | **Removed.** Lint, typecheck and build unchanged; installed packages 83 → 66. **Shipped 2026-09-16.** | — |
| `BUILD-10` — root tooling files were linted by no rule and type-checked by nothing | **Fixed.** Oxlint's `correctness` category is on for every file (overrides cannot set categories, so it applies to `src/` too — 88 more rules, 0 findings), and `tsconfig.node.json` type-checks `vite.config.ts` with Node types kept out of `src/`. Verified by negative tests: `use-isnan` + `no-debugger` on a root probe file, `TS2769` on a bad `server.port`, `TS2591` still on `process` in `src/`. This also corrects the "root tooling files lint without type information" claim in the ESLint row below — that block enabled no rules. **Shipped 2026-09-16.** | ADR-016 (amended) |
| `INFRA-01` — no CI pipeline | **Fixed.** `.github/workflows/ci.yml` runs build, test, typecheck, lint, and both vulnerability checks on every PR. Every gate verified by negative test. **Shipped 2026-08-08.** | ADR-013, `R-04`; residual `INFRA-07` |
| `INFRA-06` — Smart App Control blocks the integration tests | **Resolved as designed.** The 14 integration tests run on a clean `ubuntu-latest` runner where no Application Control policy applies. It was always an environment constraint rather than a defect, so the fix was to run them somewhere the constraint does not exist. Local Windows runs remain unreliable straight after an Api change; CI is now the authority. | ADR-013, `R-04` |
| `BUILD-07` — Node version not pinned | **Fixed.** `.nvmrc` (24) and `engines: { node: ">=20" }`. The workflow reads `node-version-file: .nvmrc`, so CI and a developer's machine cannot disagree — the two values say different things deliberately: what is *used* versus what is *supported*. | ADR-013, `R-04` |
| Should CI build Release or Debug? | **Debug.** ADR-005 makes the `IntegrationTest` environment throw in RELEASE builds, so a Release CI build fails all 14 integration tests by design (measured: 84 → 70). `TreatWarningsAsErrors` is unconditional, so the warning gate is identical in Debug. | ADR-013, ADR-005 |
| `SEC-03` — 68 open Dependabot alerts (13 npm advisories, `axios` the largest) | **Fixed** by `npm audit fix`. Every advisory resolved **within the declared semver ranges** — `package.json` did not change, only `package-lock.json`. The entry's fear that `react-router` and `vite` were "majors-adjacent" was wrong: all bumps were minor (`axios` 1.10→1.19, `react-router` 7.7→7.18, `vite` 7.0→7.3). Verified by clean `npm ci` + typecheck + lint + build, and by exercising routing, search, and theming in a browser against a live API. **Shipped 2026-08-08.** | — |
| `BUG-08` — `console.log` in shipped code | **Removed**, and `no-console` added to the ESLint config — the rule had never been configured, so the entry's claim that lint "would have caught" them was wrong. **Shipped 2026-08-08.** | ADR-012, `R-03` |
| Was the ESLint config lintable as written? | **No.** Type-aware rules were applied to `**/*.{ts,tsx}` while `tsconfig.json` includes only `src`, so `eslint.config.ts` and `vite.config.ts` were parse errors. Typed rules now scope to `src/**`; root tooling files lint without type information. | ADR-012 |
| `UX-04` — React Query Devtools button covered the theme switch in development | **Fixed** with `buttonPosition="bottom-left"`, where only the non-interactive copyright text sits beneath it. Verified at 1024×768 and 375×812: the switch centre is hit-testable and toggles under real clicks. Production output is byte-identical. **Shipped 2026-09-16.** | `main.tsx` |
