# Known issues & backlog

Every **defect and gap in what already exists**. Planned work that does not exist yet lives in
[roadmap.md](roadmap.md); the two files cross-reference each other.

Verified against `main` @ `edfd057` on 2026-07-26 by running the real toolchain — not by reading code. Build and
test numbers re-measured on 2026-08-04 after `R-02`, and the frontend rows re-measured on 2026-08-08 after `R-03`
and again after the `SEC-03` dependency remediation. The npm audit row re-measured 2026-09-12. The frontend
tests row was added 2026-09-18 after `R-07` and re-measured 2026-09-20 after `R-16` PR 3 shipped in full, by
running `npm test` directly (10 files, 77 passed). Backend test numbers re-measured 2026-09-24 after `R-10`
(ADR-022), from **CI run 36051107842**, which shows 104 unit + 34 integration passed, 0 failed, 0 skipped, and
77 frontend tests across 10 files. They were measured on CI rather than locally on purpose: Docker is not
installed on the author's machine, and Smart App Control intermittently blocks freshly-built assemblies there
(`INFRA-06`), so CI is the only place these numbers can be taken honestly.

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
| Backend tests | `dotnet test RecipeManager.sln` | **138 passing** (104 unit + 34 integration), 0 failing, 0 skipped on CI. Of the 34 integration tests, **30 need Docker** and report as skipped without it (ADR-017); the other 4 (`OpenApiContractTests`, ADR-019) need none, but on a Windows machine under Smart App Control (`INFRA-06`) they **fail** with `FileLoadException` rather than skip |
| NuGet vulnerabilities | `dotnet list package --vulnerable --include-transitive` | **none**, all six projects clean |
| Frontend type-check | `npm run typecheck` | **0 errors** |
| Frontend build | `npm run build` | succeeds, and type-checks `src/` and `vite.config.ts` first (`tsc -b tsconfig.json tsconfig.node.json && vite build`, ADR-012, `BUILD-10`) |
| Frontend lint | `npm run lint` | **0 problems** — Oxlint, 159 rules: the 71 type-aware ones on `src/**` plus the `correctness` category everywhere (ADR-016; ESLint until then, ADR-012) |
| npm vulnerabilities | `npm audit --audit-level=high` | **0** — re-cleared 2026-09-12 by `npm audit fix` after two new transitive dev-only advisories surfaced post-`SEC-03` (`GHSA-2883-xcg3-v3hh`, `GHSA-p498-v437-472g`). A clean audit expires: it is a claim about the advisory database on the day it ran, not a property of the lock file (`SEC-03`, [Settled](#settled)). |
| Frontend tests | `npm test` | 77 pass — Vitest + RTL under jsdom (ADR-018) |
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
| [BUILD-06](#build-06) | Low | Tooling | `run-coverage.ps1` measures only the unit-test project |
| [SEC-01](#sec-01) | **Critical** | Security | No authentication at all |
| [SEC-02](#sec-02) | **Critical** | Security | No authorization / no recipe ownership |
| [SEC-04](#sec-04) | **High** | Security | No rate limiting on unauthenticated write endpoints |
| [SEC-05](#sec-05) | **High** | Security | Exception messages returned to the client on 500s |
| [SEC-06](#sec-06) | Medium | Security | `DeleteRecipeHandler` echoes `ex.Message` to the client |
| [SEC-07](#sec-07) | Medium | Security | `GET /api/recipes` is unbounded and cached whole |
| [SEC-08](#sec-08) | Medium | Security | No length limits in the database |
| [SEC-10](#sec-10) | Medium | Security | No security headers, no HSTS |
| [SEC-11](#sec-11) | Low | Ops | No health/readiness endpoint |
| [SEC-12](#sec-12) | Low | Config | `.env.production` points at a placeholder host |
| [BUG-07](#bug-07) | Low | API | `GET /api/recipes/{id}` missing the `:guid` route constraint |
| [BUG-10](#bug-10) | Medium | Frontend | No recipe detail route, so cards are not clickable |
| [BUG-12](#bug-12) | Low | Frontend | A paused recipe query renders "No recipes available" |
| [BUG-13](#bug-13) | Low | Frontend | Whitespace-only search query shows a misleading "matching" heading |
| [BUG-14](#bug-14) | Low | Caching | The cache still hands out shared entity instances; the write path no longer takes one |
| [BUG-15](#bug-15) | Medium | API | A client-supplied ingredient id can trigger a duplicate-key 500 on create, on update, or within one payload |
| [BUG-16](#bug-16) | Low | API | `"ingredients": [null]` reaches the handler and fails as a 500 instead of a 400 |
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
| [QUAL-04](#qual-04) | Low | Quality | Routes, verbs, and status codes are still hand-typed on the client |
| [QUAL-05](#qual-05) | Low | Quality | Frontend indentation is mixed and no formatter enforces it |
| [UX-05](#ux-05) | Medium | UX | The canonical design says only the title is required; the domain requires more |
| [UX-06](#ux-06) | Medium | UX | `--rule` is a decorative hairline, not a control boundary |
| [UX-07](#ux-07) | Low | UX | No visual-regression tooling |
| [DEC-03](#dec-03) | — | Decision | `Ardalis.GuardClauses` is referenced but unused |
| [DEC-04](#dec-04) | — | Decision | `UseErrorHandler` position in the pipeline |
| [DEC-07](#dec-07) | — | Decision | 24 h cap excludes slow-cooked and fermented recipes |
| [DEC-08](#dec-08) | — | Decision | Split the reconstructed ADRs into individual files? |

Resolved decisions and items promoted to planned work are recorded in [Settled](#settled) at the end.

---

## Build & tooling

### BUILD-06
**`run-coverage.ps1` measures only the unit-test project — Low**

The script runs `dotnet test RecipeManager.UnitTests` and reports on that alone, so the 34 integration tests
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

`R-16` PR 3 added a second caller: `HomePage` renders the live recipe count via `useRecipes`, so Home now also
issues a full `GET /api/recipes` just to read `recipes.length`. Harmless today — TanStack Query shares the cache
with the Recipes list, so Home's call effectively just prefetches it — but it means this endpoint gets expensive
before the list screen does, not only when the list screen is opened.

### SEC-08
**No length limits in the database — Medium**

`Title` and `Description` are unbounded `text`. The 200/1000-character
caps exist **only** in FluentValidation, so anything writing outside the API — a future bulk import, a direct
psql session, a second service — can store unbounded values.

`"RecipeIngredients"."Name"` and `"Notes"` are the exception: ADR-022 bounded both at `varchar(200)` in the
database as well as in the validator, which is what this entry asks for everywhere else. So is
`"RecipeInstructionSteps"."Text"` (`varchar(2000)`, `R-17`, 2026-09-26), which replaced the unbounded
`Instructions` `text[]` this entry used to list.

**Fix.** Add `HasMaxLength` for `Title` and `Description` to `RecipeConfiguration` and generate a migration.

**Update 2026-09-24.** The architecture blocker is gone. `AppDbContext.OnModelCreating` and
`RecipeManager.Infrastructure/Context/Configurations/RecipeConfiguration.cs` now exist (ADR-022), so this is
one `HasMaxLength` pair plus a migration — no architecture work, no sign-off, and nowhere left to put it but
the file that is already there. `Title` and `Description` stayed out of
[spec 010](specs/010-structured-ingredients.md) because altering an **existing** column is a different
migration risk from creating a new one, and mixing the two would have hidden it.

**Owner:** `02-senior-csharp` · **Effort:** ~30 min

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

### BUG-07
**`GET /api/recipes/{id}` missing the `:guid` constraint — Low**

`[HttpGet("{id}")]` while `[HttpPut("{id:guid}")]` and `[HttpDelete("{id:guid}")]` are constrained. A malformed
id reaches model binding instead of being rejected by routing, producing an inconsistent error shape.

### BUG-10
**No recipe detail route, so cards are not clickable — Medium**

`App.tsx` routes only `/`, `/recipes`, and `/profile`. There is no `/recipes/:id` screen, so there is nothing
for a `RecipeCard` click to navigate to.

Until `R-03`, `RecipeList` passed an `onClick` whose entire body was `console.log('Clicked recipe:', …)`. That
made every card render as `<button aria-label="View {title} recipe">` — focusable, announced to a screen reader
as an action, and doing nothing when activated. Removing the debug log (`BUG-08`) left a no-op handler, so the
`onClick` was dropped and cards now render as `<article>`, which is what `RecipeCard`'s element switch is for.

**Fix.** Build the detail screen, add the `/recipes/:id` route, and pass `onClick` again — `RecipeCard` already
switches to `<button>` when it receives one. The screen is planned as `R-18`.

**`R-18` also has to decide what makes a row look clickable.** Since `7ae44b5` the row has no outline, no fill
and no `:hover` — a clickable row differs from an inert one only by `cursor: pointer`, which says nothing to a
touch user and nothing at rest. The app-wide `:focus-visible` outline in `globals.css` covers keyboard users;
the gap is the resting and hover state for pointer and touch.

That is not a measurable WCAG 1.4.11 failure — the criterion governs the visual information an author provides
to identify a control, and here there is none to measure. It is the problem one step earlier: the control is
not identifiable at all. Whatever `R-18` adds becomes that visual information the moment it exists, and then
has to clear 3:1 against what sits behind it. A fill change alone will not carry it — `--paper-2` on `--paper`
is 1.09:1 in both themes — and neither will `--rule` (1.29:1 light, 1.36:1 dark). `--field-border` does
(3.58:1 light, 3.67:1 dark). See `UX-06`.

**Owner:** `03-senior-react` + `07-ux-ui`

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
**The cache still hands out shared entity instances — Low** *(was Medium; the write path was the severe half and it is fixed)*

`IMemoryCache` stores object references, not copies, and `CachedRecipeRepository` caches `Recipe` **entities**
rather than an immutable read model. `GetAllAsync` and `GetByIdAsync` therefore return the same object to every
caller until the entry expires, while the cache is a singleton and the repository is scoped.

Found while designing `R-08` (spec 006).

**What closed on 2026-09-24** (`R-10`, ADR-022 — incidentally, while fixing the `PUT` 500s). The original
mechanism was that `UpdateRecipeHandler` loaded through `GetByIdAsync`, got the cached instance on a hit, and
`Recipe.Update(...)` mutated it in place before anything was saved; a failed `SaveChangesAsync` then skipped
`InvalidateRecipeRelatedCaches` and the API served values that were never persisted. **The write path no longer
touches the cache at all**: `UpdateRecipeHandler` loads through `IRecipeRepository.GetByIdForUpdateAsync`, and
`CachedRecipeRepository` implements that method by delegating straight to the database with no cache read and no
cache write, with the reason stated in a comment on the method. A failed save now leaves the cache holding the
*previously persisted* values, which is stale but not fictitious.

**What is still open.** Handing out a shared entity is still the design. Its last concrete exposure — the former
`BUG-11`, a cached recipe's `Instructions` materialised as a castable `List<string>` — **closed with `R-17`**
(2026-09-26): `Ingredients`, `Instructions`, and every step's `IngredientIds` are now read-only copies of private
backing fields. The in-memory side is pinned by unit tests; the materialised-from-PostgreSQL side by
`RecipesControllerTests.RecipeReadFromPostgres_ShouldRoundTripStepReferencesAndExposeNoMutableList`, which needs
Docker and had **not yet run on CI** when this was written — confirm it green there before relying on it. What remains
is structural rather than a known hole: the entity's safety depends on every future member staying read-only,
and nothing enforces that. Severity stays Low — nothing reachable over HTTP triggers it.

**Fix.** Cache an immutable read model (`RecipeDto`) instead of the entity, which makes the guarantee
structural instead of a property every entity member has to keep. Then tighten the detail assertion in
`RecipeCacheTests.UpdateRecipe_AfterListAndDetailWereCached_ShouldReturnNewValuesFromBoth` so it detects a
missing `recipe_{id}` invalidation — the entry previously noted that this was impossible because the cached
object already held the new values. That is no longer true, so the assertion can now be written.

### BUG-15
**A client-supplied ingredient id can trigger a duplicate-key 500 by three separate routes — Medium**

Found 2026-09-24 while closing `R-10`. `IngredientInputDto.Id` exists so a client can echo back the ids the API
gave it, keeping an ingredient's identity stable across an edit; a null means "this ingredient is new".

**Updated 2026-09-26 (`R-17`, ADR-023).** Step references do not travel on this id: a request addresses
ingredients by index into its own payload, and the server resolves indexes to the ids it has just minted or
accepted. So the echo no longer carries step references, and a client that omits it gets correctly re-wired steps.
**All three routes below still stand** — `R-17` neither fixed nor widened them (index resolution can only name
an ingredient in the same payload, so it adds no cross-recipe route). Fixing this is the next PR after `R-17`.
`Ingredient.Create` takes that id and uses it verbatim — `id ?? Guid.NewGuid()` — and nothing between the
controller and `SaveChangesAsync` checks it. Three independent routes reach the same duplicate-key crash;
an earlier version of this entry named only (b) and its stated fix would have left (a) and (c) open.

**(a) Two ingredients in one payload sharing the same id.** `IngredientMappingExtensions.ToIngredients` builds
one `Ingredient` per `IngredientInputDto` with no de-duplication, so two DTOs carrying the same id produce two
distinct `Ingredient` instances that both hold it. `Recipe.ReplaceIngredients` (called from both `Recipe.Create`
and `Recipe.Update`) adds every instance to `_ingredients` unconditionally, so this is reachable from **both**
`POST` and `PUT` — both route through `ToIngredients`. EF then tracks two owned entities on one key in the same
`SaveChanges` call.

**(b) An update supplying an id that belongs to a different recipe.** `Recipe.Update` never checks a supplied id
against its own `_ingredients`, so an id copied from another recipe's row is accepted as-is. EF inserts it rather
than updating an existing row, colliding with the primary key that row already occupies on `"RecipeIngredients"`.
This is the route the original entry described, and the only one its stated fix ("reject any supplied id not
already in `_ingredients`") would have closed.

**(c) A create supplying any pre-existing id.** `Recipe.Create` has no existing ingredient set to check a
supplied id against at all — there is nothing before it — so `POST /api/recipes` with a client-chosen id that
already exists on `"RecipeIngredients"` (belonging to any recipe) collides on insert. This route never touches
`Recipe.Update`, so a fix scoped to that method cannot see it.

**Consequences.**
- All three routes end the same way: EF's `SaveChangesAsync` throws (`InvalidOperationException` for two tracked
  entities sharing one key within a single `SaveChanges` call — route (a); `DbUpdateException` for a primary-key
  violation against already-persisted data — routes (b) and (c)), and the client gets a **500** where a 422
  naming `ingredients` is what the contract implies.
- A `PUT` or `POST` can therefore be used to probe whether a given `Guid` exists as an ingredient anywhere in the
  database, by distinguishing 204/201 from 500. That is a weak oracle, but it is one.

**Severity: Medium.** Not Low, because the failure is a 500 rather than a handled error and the underlying rule
("an ingredient id is assigned by the server and belongs to exactly one recipe") is a domain invariant the
aggregate is supposed to own — CLAUDE.md global rule 5 and rule 6 both point at it. Not High: there is no
authentication and no ownership yet ([SEC-01](#sec-01), [SEC-02](#sec-02)), so "another user's row" does not mean
anything today — every recipe is already world-writable, and an attacker who wants to damage another recipe can
simply `PUT` it directly. That changes the moment `R-14` lands, and this must be closed **before** it: after
ownership exists, a cross-recipe id is a genuine cross-tenant write attempt, and today's code would answer it
with a database error rather than a refusal.

**Fix.** Closing this needs all three routes covered:
- Reject a payload containing duplicate, non-null ingredient ids before it reaches persistence — in
  `IngredientMappingExtensions.ToIngredients`, or in `Recipe` itself — closing route (a) on both `POST` and
  `PUT`.
- In `Recipe.Update`, reject any supplied non-null ingredient id that is not already in `_ingredients` — closing
  route (b). The check belongs in the domain, not the validator: it is a rule about the aggregate's contents, not
  about payload shape.
- On create, never honour a client-supplied id — `Recipe.Create` (via `Ingredient.Create`) should mint every id
  itself, since a brand-new recipe has no existing ingredient for a client-supplied id to legitimately reference
  — closing route (c).

All three need a `RecipeErrors` factory in the `Validation`/`ingredients` shape, consistent with the existing
error style; a single factory reused for all three is plausible, but that is an implementation choice for
whoever picks this up, not something this entry should pre-decide.

**Owner:** `02-senior-csharp` · `01-architect` if the error's `field` or kind is contested

### BUG-16
**A `null` element in `ingredients` is a 500, not a 400 — Low**

Found 2026-09-26 by the whole-branch review of `R-17`. JSON allows `"ingredients": [null]`. FluentValidation's
child validator (`SetValidator` inside `ForEach`) skips null elements, and ASP.NET's implicit-required check for
non-nullable reference types covers properties, not list elements — so nothing rejects the null before
`IngredientMappingExtensions.ToIngredients` dereferences it. The `NullReferenceException` reaches
`ErrorHandlerMiddleware` and the client gets a 500 for what is a malformed request.

Present since `R-10`. `R-17` hit the identical flaw on `instructions` and fixed it there (`NotNull()` per item in
`RecipeValidationRules.ValidateInstructions`, pinned by
`CreateRecipeCommandValidatorTests.Validate_ANullInstructionStep_ShouldFailInsteadOfReachingTheHandler`); the
ingredient side was left alone because it predates that change (CLAUDE.md rule 9).

**Fix.** The same one line in `ValidateIngredients` — `.ForEach(item => item.NotNull().SetValidator(...))` — plus
the matching validator test.

**Owner:** `02-senior-csharp` · **Effort:** ~10 min

---

## Testing gaps

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

### QUAL-06
**A Node ambient-types directive leaks into the browser program — Low**

`recipe-manager-frontend/src/styles/themes/tokenParity.test.ts` opens with `/// <reference types="node" />`. The
comment above it is accurate — it widens the whole `tsconfig.json` program's ambient types (`process`,
`__dirname`, `Buffer`, and the Node module resolutions the test needs) rather than scoping to this one file —
but that is exactly what `BUILD-10` drew a boundary against: Node globals must not leak into `src/`. It is
currently necessary: without the directive, `npm run typecheck` fails with `TS2591` on the test's `node:fs` and
`node:url` imports (verified). No other file under `src/` relies on a Node global today, so the leak has not yet
been exploited, but nothing stops a later file from reading `process.env` under the widened program without
Oxlint or the compiler objecting.

**Fix.** Either give this test its own scoped `tsconfig` (an `include`/`references` split so the directive's
reach is one file, not the whole program), or import the CSS files as raw text (`import lightCss from
'./light.css?raw'`) with `test: { css: true }` added to `vite.config.ts` — rejected for this PR because Vitest
does not process CSS imports without that flag, and turning it on changes CSS handling for every test in the
suite to fix one file's typing.

---

## UX & accessibility

### UX-05
**The canonical design states a validation rule the domain does not enforce — Medium**

The editorial design's Add/Edit screen (3d, see [agents/07-ux-ui.md](agents/07-ux-ui.md#canonical-design-reference))
says: "Nothing here is required except the title — the same rule your validator already enforces." That is
false. `Recipe.ValidateProperties` also requires a description, at least one non-zero time, `Servings >= 1`, and
at least one ingredient and one instruction ([domain-model.md](domain-model.md#invariants-recipevalidateproperties)).
A form built to the design as drawn would let the user save something the API rejects with 422.

This is contract drift of a new kind. It sits between the design and the domain rather than between the C# and
TypeScript types, so neither `R-09`'s generated types nor any test can catch it.

**Fix.** `R-19` (draft recipes) makes the claim true for drafts. Until it ships, `R-21` must not be built to the
design's copy. Update the design's helper text when `R-19` lands, so it describes drafts and publishing.

### UX-06
**`--rule` is a decorative hairline, not a control boundary — Medium**

`--rule` measures **1.29:1** against `--paper` (1.36:1 in dark) — nowhere near WCAG 1.4.11's 3:1 requirement for
the visual boundary that identifies a control. It is exactly what the design's own search field used to bound
itself, until `--field-border` was introduced to replace it there (spec
[009](specs/009-editorial-design-system-and-shell.md) §8.2, ADR-021).

`--rule` stays legitimate for what it already is — a decorative separator between list rows, sections, or a
footer rule — where nothing needs to be identified as clickable. Recorded so a future screen does not reach for
`--rule` to bound an input, a button outline, or any other control and reintroduce the problem `--field-border`
was added to fix.

**It has already happened once, and it was not caught — it was outgrown.** `RecipeCard.module.css` bounded
`.recipesList` with `--rule` while `RecipeCard.tsx` rendered that same element as a `<button>` whenever it was
given an `onClick`, so the hairline was the boundary of a control at 1.29:1 (1.18:1 against the card's own
`--paper-2` fill). It entered in `e482720` — **the same commit that introduced `--rule` and wrote the checklist
item in [agents/07-ux-ui.md](agents/07-ux-ui.md)** — and survived until `7ae44b5` (`R-16` PR 3) replaced the
boxed card with an editorial row carrying no outline at all. The selector is gone, so the instance is gone. That
is weaker than a fix: nothing detected it, and nothing would have stopped it persisting had the redesign not
happened to delete it.

Two things are worth keeping:

- **The checklist did not survive its own first outing.** `e482720` converted `HomePage`'s `.actionButton` to
  `--field-border` correctly and `RecipeCard` to `--rule` incorrectly, in one commit.
- **It was invisible to CSS review**, because the offending selector and the element switch that makes it a
  control live in different files. Nothing in `RecipeCard.module.css` said that border ever bounded a `<button>`.

Every `--rule` border on `main` today is legitimate: `Header`, `Footer`, `BottomNav`, `RecipeList` and
`ProfilePage` use it as a container edge, and `RecipeCard`'s is the separator between rows — which stays a
separator even when the row is a `<button>`, because it is not what identifies the control. The live
consequence now sits in `BUG-10`.

**Fix.** Still open, still a guardrail rather than a live defect. Note that a review checklist item does **not**
satisfy the close condition: one existed and did not work. Close this when a lint rule makes the pattern
unrepresentable — bearing in mind a CSS-only rule cannot, since six files use `--rule` on a border legitimately.
The check has to know whether a selector can land on an interactive element, which means reading the CSS and
the TSX together.

### UX-07
**No visual-regression tooling — Low**

Nothing in the toolchain renders a screen and diffs it against a previous version. A token change — a hex value,
a type-scale size, a spacing value — that breaks one screen's layout is caught only by a human looking at it in
both themes, and that is the entire verification story for the editorial token migration (`R-16`,
[spec 009](specs/009-editorial-design-system-and-shell.md) §12).

**Fix.** Adopt a snapshot/visual-diff tool (e.g. Playwright's screenshot assertions, Chromatic) once there are
enough screens for the cost to pay for itself. Not proposed for `R-16` — it is its own dependency decision and
needs `01-architect`.

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
| Ingredients: keep free text or structure them? | **Structure them.** The `string[]` shape was an acknowledged temporary shortcut. **Shipped 2026-09-24** as `R-10`, which is consequently gone from the roadmap. | ADR-022, [spec 010](specs/010-structured-ingredients.md) |
| Ingredients: shared catalogue or owned by the recipe? | **Owned by the recipe.** No second aggregate, so ADR-006 (no unit of work) still holds. The cost is that "tomato" and "tomatoes" are unrelated names. Settled 2026-09-19; **shipped 2026-09-24**. Note the wording "as value objects" recorded here was superseded before it was built: ADR-022 made `Ingredient` an owned **entity** with its own `Guid Id`, because indices are not stable references for `R-17`'s steps. Ownership was the part that was settled; the value-object phrasing was not. | ADR-022, [spec 010](specs/010-structured-ingredients.md) |
| `DEC-06` — follow the OS colour-scheme preference on first visit? | **Yes, as an explicit choice, and now implemented.** Settled 2026-09-19 by the editorial design (screen 3e); **implemented the same day in PR 1 of `R-16`**: `ThemePreference` (`light`/`dark`/`system`, persisted) is now separate from the derived `Theme` (`light`/`dark`, rendered), `ThemeProvider` subscribes to `prefers-color-scheme` so `system` follows the OS live, and a visitor with nothing stored defaults to `system` rather than `light`. The segmented Light/Dark/System control itself, and its move into Settings, **shipped 2026-09-20 in `R-16` PR 3**: `ThemeControl` (three native radios in a `role="radiogroup"` fieldset) now lives in `ProfilePage`, the footer's binary switch is deleted, and `toggleTheme` is gone from the context. | ADR-021, `R-16` |
| Only a title required (design) vs. full invariants (domain)? | **Draft recipes**: an explicit Draft/Published status, where drafts need only a title. Chosen over drafting only in the browser, and over relaxing the aggregate. Settled 2026-09-19. | `R-19`, `UX-05` |
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
| `INFRA-06` — Smart App Control blocks the integration tests | **Resolved as designed.** The 14 integration tests that existed then run on a clean `ubuntu-latest` runner where no Application Control policy applies. It was always an environment constraint rather than a defect, so the fix was to run them somewhere the constraint does not exist. Local Windows runs remain unreliable straight after an Api change; CI is now the authority. **Corrected 2026-09-24** — the scope recorded above was too narrow. It is **not** limited to the contract tests, nor to the integration suite: Smart App Control intermittently blocks **any freshly-built assembly** on that machine, `dotnet ef` included, so migration scaffolding can fail the same way. It is not fixable while Smart App Control is on, and turning it off is permanent and irreversible on Windows — the repository owner has declined, which is a reasonable trade rather than a deferral. The consequence to plan around: **numbers and green runs are taken from CI, not from a local Windows run**, and a local failure naming `FileLoadException` is not evidence of a defect. | ADR-013, `R-04` |
| `BUILD-07` — Node version not pinned | **Fixed.** `.nvmrc` (24) and `engines: { node: ">=20" }`. The workflow reads `node-version-file: .nvmrc`, so CI and a developer's machine cannot disagree — the two values say different things deliberately: what is *used* versus what is *supported*. | ADR-013, `R-04` |
| Should CI build Release or Debug? | **Debug.** ADR-005 makes the `IntegrationTest` environment throw in RELEASE builds, so a Release CI build fails all 14 integration tests by design (measured: 84 → 70). `TreatWarningsAsErrors` is unconditional, so the warning gate is identical in Debug. | ADR-013, ADR-005 |
| `SEC-03` — 68 open Dependabot alerts (13 npm advisories, `axios` the largest) | **Fixed** by `npm audit fix`. Every advisory resolved **within the declared semver ranges** — `package.json` did not change, only `package-lock.json`. The entry's fear that `react-router` and `vite` were "majors-adjacent" was wrong: all bumps were minor (`axios` 1.10→1.19, `react-router` 7.7→7.18, `vite` 7.0→7.3). Verified by clean `npm ci` + typecheck + lint + build, and by exercising routing, search, and theming in a browser against a live API. **Shipped 2026-08-08.** | — |
| `BUG-08` — `console.log` in shipped code | **Removed**, and `no-console` added to the ESLint config — the rule had never been configured, so the entry's claim that lint "would have caught" them was wrong. **Shipped 2026-08-08.** | ADR-012, `R-03` |
| Was the ESLint config lintable as written? | **No.** Type-aware rules were applied to `**/*.{ts,tsx}` while `tsconfig.json` includes only `src`, so `eslint.config.ts` and `vite.config.ts` were parse errors. Typed rules now scope to `src/**`; root tooling files lint without type information. | ADR-012 |
| `UX-04` — React Query Devtools button covered the theme switch in development | **Fixed, then reopened, then fixed again.** Originally fixed with `buttonPosition="bottom-left"` on 2026-09-16, where only the non-interactive copyright text in the `Footer` sat beneath it. `R-16`'s `BottomNav` (PR 2) invalidated that fix: below 768px the footer and the new mobile tab bar both occupy the bottom of the viewport, so `bottom-left` landed squarely on `BottomNav`'s Home destination — measured at 375×812, the devtools button covered (16,756)–(56,796) against Home's (15.7,762)–(74.5,812), so `elementFromPoint` on Home's centre returned the devtools button, not the link. `bottom-right` was checked too and fares no better: it lands on the Profile ("You") tab in the same way. **Re-fixed 2026-09-20** with `buttonPosition="top-right"`: `top-left` was tried first and rejected because the header brand sits there at every width; `top-right` is clear of the brand and of every `BottomNav` item at 375×812 (verified with `elementFromPoint` at each item's centre). Trade-off accepted: at desktop widths the button's 48px box clips a ~20px sliver of the header's "New recipe" pill, but the pill's centre still resolves to the link — a corner had to lose, and desktop was not the width the reopened defect was reported at. Dev-only in both cases; production output unaffected. | `main.tsx` |
