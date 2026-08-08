# Known issues & backlog

Every **defect and gap in what already exists**. Planned work that does not exist yet lives in
[roadmap.md](roadmap.md); the two files cross-reference each other.

Verified against `main` @ `edfd057` on 2026-07-26 by running the real toolchain — not by reading code. Build and
test numbers re-measured on 2026-08-04 after `R-02`, and the frontend rows re-measured on 2026-08-08 after `R-03`
and again after the `SEC-03` dependency remediation.

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
| Backend tests | `dotnet test RecipeManager.sln` | **84 passing** (70 unit + 14 integration), 0 failing |
| NuGet vulnerabilities | `dotnet list package --vulnerable --include-transitive` | **none**, all six projects clean |
| Frontend type-check | `npm run typecheck` | **0 errors** |
| Frontend build | `npm run build` | succeeds, and now type-checks first (`tsc -b && vite build`, ADR-012) |
| Frontend lint | `npm run lint` | **0 problems** — runs since `jiti` was added (ADR-012, `R-03`) |
| npm vulnerabilities | `npm audit` | **0** — cleared 2026-08-08 by `npm audit fix`, verified again after `npm ci` (`SEC-03`, [Settled](#settled)) |
| Frontend tests | — | **none exist**, no runner installed ([TEST-01](#test-01)) |
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

---

## Summary

| ID | Severity | Area | Issue |
| --- | --- | --- | --- |
| [BUILD-05](#build-05) | Medium | Perf | `mainPhoto.png` is 2.1 MB — 5.7× the entire JS bundle |
| [BUILD-06](#build-06) | Low | Tooling | `run-coverage.ps1` measures only the unit-test project |
| [BUILD-08](#build-08) | Low | Tooling | `ts-node` is a devDependency nothing uses |
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
| [BUG-01](#bug-01) | **High** | Contract | TS `id: number` vs. server `Guid` |
| [BUG-02](#bug-02) | **High** | Contract | `servings` missing from the TS `Recipe` type |
| [BUG-03](#bug-03) | **High** | Contract | `instructions` missing from the TS `Recipe` type |
| [BUG-04](#bug-04) | Medium | Contract | TS `image?` field does not exist on the server |
| [BUG-05](#bug-05) | Medium | Contract | `updateRecipe` typed as returning a body; API returns 204 |
| [BUG-06](#bug-06) | Medium | Frontend | `/recipes/new` is linked but has no route |
| [BUG-07](#bug-07) | Low | API | `GET /api/recipes/{id}` missing the `:guid` route constraint |
| [BUG-09](#bug-09) | Low | Frontend | Error recovery does a full page reload |
| [BUG-10](#bug-10) | Medium | Frontend | No recipe detail route, so cards are not clickable |
| [TEST-01](#test-01) | **High** | Tests | No frontend test runner or tests |
| [TEST-02](#test-02) | **High** | Tests | Cache invalidation has no dedicated test |
| [TEST-03](#test-03) | Medium | Tests | Instruction ordering never asserted |
| [TEST-04](#test-04) | Low | Tests | `Location` header on 201 never asserted |
| [TEST-05](#test-05) | Low | Tests | No agreed coverage threshold |
| [TEST-06](#test-06) | Medium | Tests | Integration tests cannot reproduce Npgsql behaviour |
| [INFRA-07](#infra-07) | Medium | CI/CD | CI runs on every PR but is not yet *required* to merge |
| [INFRA-02](#infra-02) | Medium | CI/CD | No versioning or tags |
| [INFRA-03](#infra-03) | Medium | CI/CD | No rollback procedure |
| [INFRA-04](#infra-04) | Medium | CI/CD | No frontend deployment target |
| [INFRA-05](#infra-05) | Low | DX | No seed data |
| [QUAL-01](#qual-01) | Low | Quality | `ILogger` called with interpolated strings |
| [QUAL-02](#qual-02) | Low | Quality | `Console.WriteLine` used for startup logging |
| [QUAL-03](#qual-03) | Low | Quality | Deep relative imports for shared assets |
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

Production build output:

```
dist/assets/mainPhoto-DpyCXFCt.png   2,115.48 kB
dist/assets/index-C7UHO9B0.js          373.08 kB │ gzip: 124.88 kB
dist/assets/index-DS_0laZR.css          12.07 kB │ gzip:   2.94 kB
```

The image is **5.7× larger than all JavaScript combined** and is shipped unoptimized. It is used twice — as the
hero image on `HomePage` and as the fallback thumbnail in every `RecipeCard`, so a list of 20 recipes references
a 2 MB asset 20 times (cached, but decoded at full resolution each time).

**Fix.** Re-encode to WebP/AVIF at the sizes actually rendered, and ship a separate small placeholder for the
card fallback rather than reusing the hero image. Consider `srcset` for the hero.

**Owner:** `03-senior-react` + `07-ux-ui` · **Effort:** ~1 h

### BUILD-06
**`run-coverage.ps1` measures only the unit-test project — Low**

The script runs `dotnet test RecipeManager.UnitTests` and reports on that alone, so the 14 integration tests
contribute nothing and the reported percentage understates real coverage — particularly for `Api` and
`Infrastructure`, which unit tests never touch.

**Fix.** Point it at `RecipeManager.sln` and merge both coverage outputs, or rename the script and state the
limitation in `README.md`.

**Owner:** `06-qa-tester` · **Effort:** ~30 min

### BUILD-08
**`ts-node` is a devDependency nothing uses — Low**

`ts-node@^10.9.2` sits in `recipe-manager-frontend/package.json` with no reference anywhere: no `ts-node`
section in `tsconfig.json`, no script that invokes it, no config that loads through it. The most likely
explanation is an earlier attempt to fix `BUILD-03` — ESLint 9 loads a TypeScript flat config through `jiti`,
not `ts-node`, so it never had any effect.

**Fix.** Remove it, then `npm run lint`, `npm run typecheck`, and `npm run build` to confirm nothing regressed.
Left in place by `R-03`, which added `jiti` — removing an unrelated dependency in the same PR would have
obscured which change made lint work.

**Owner:** `03-senior-react` · **Effort:** ~5 min

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

**Fix.** Set the real value, or delete the file so the relative `/api` fallback applies.

---

## Contract & functional defects

Full detail and the corrected TypeScript shape are in [agents/08-api-contract.md](agents/08-api-contract.md).
[BUG-01](#bug-01) through [BUG-03](#bug-03) should be fixed together in one PR.

### BUG-01
**TS `id: number` vs. server `Guid` — High**

`recipe-manager-frontend/src/types/recipe.ts` declares `id: number`; `RecipeDto.Id` is a `Guid`, serialised as a
string. `recipeService.getRecipeById/updateRecipe/deleteRecipe` all take `id: number`.

Nothing has broken yet only because the single existing screen just lists recipes and uses `key={recipe.id}`,
which stringifies. Any detail, edit, or delete screen breaks immediately.

### BUG-02
**`servings` missing from the TS type — High**

The API returns it; the client cannot see or edit it.

### BUG-03
**`instructions` missing from the TS type — High**

The API returns it; the client cannot see or edit it. A recipe app that cannot display its own steps.

### BUG-04
**TS `image?: string` does not exist server-side — Medium**

Always `undefined`, so `RecipeCard` permanently falls back to the bundled placeholder. Needs a product decision:
add an image field to the API (which pulls in upload handling — see the requirements in
[agents/05-security-reviewer.md](agents/05-security-reviewer.md)), or remove the field from the TS type.

### BUG-05
**`updateRecipe` typed as returning a body — Medium**

Typed `Promise<AxiosResponse<Recipe>>`, but `PUT /api/recipes/{id}` returns **204 No Content**. Should be
`AxiosResponse<void>`.

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
switches to `<button>` when it receives one. Note that the detail screen is blocked by the contract defects
[BUG-01](#bug-01)–[BUG-03](#bug-03): it needs the real `Guid` id, `servings`, and `instructions`.

**Owner:** `03-senior-react` + `07-ux-ui`

---

## Testing gaps

### TEST-01
**No frontend test runner — High**

No Vitest, no Jest, no React Testing Library, no `test` script. Zero frontend tests exist.

Recommended: **Vitest + React Testing Library + jsdom** (Vitest reuses `vite.config.ts` aliases directly).
Needs `01-architect` sign-off for the dependency. First tests worth writing are listed in
[agents/06-qa-tester.md](agents/06-qa-tester.md#frontend-testing--not-set-up).

### TEST-02
**Cache invalidation has no dedicated test — High**

The largest gap in the backend suite. Unit tests mock `IRecipeRepository`, so they bypass `CachedRecipeRepository`
entirely; the integration tests assert **database** state after a write rather than issuing a second request
through the API. A broken invalidation in `CachedRecipeRepository` would pass all 84 tests.

**Fix.** Integration tests that write, then re-read **through the HTTP client**: create → `GET /api/recipes`
contains it; update → `GET /api/recipes/{id}` shows new values; delete → `GET /api/recipes/{id}` returns 404.

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

### TEST-06
**Integration tests cannot reproduce Npgsql behaviour — Medium**

`IntegrationTestBase` uses EF InMemory. `text[]` semantics, identifier folding, collation, real constraint
violations, and concurrency do not surface. Anything provider-specific must be verified manually against a real
PostgreSQL, and PRs should say so explicitly.

**Possible fix.** Testcontainers for PostgreSQL — `R-06`, **now unblocked**: it was deferred until CI existed
because it needs Docker in both places, and the `ubuntu-latest` runner provides it (ADR-013).

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

### INFRA-05
**No seed data — Low**

The database starts empty and there is no seeder, so a fresh clone shows an empty app until recipes are created
by hand through Swagger. Integration tests seed only against EF InMemory. A Development-only seeder is planned as `R-13`.

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
| `DEC-05` — generate TS types from OpenAPI? | **Yes**, after the contract defects are fixed by hand. | `R-09` |
| Ingredients: keep free text or structure them? | **Structure them.** The `string[]` shape was an acknowledged temporary shortcut. | `R-10` |
| CQRS: hand-rolled or MediatR? | **Keep hand-rolled**, and remove its one real drawback by auto-registering handlers with Scrutor (already a dependency). **Shipped 2026-08-03.** | ADR-001, ADR-008 |
| Integration tests: EF InMemory or a real database? | **Testcontainers with real PostgreSQL**, deferred until CI exists. | `R-06` |
| `BUILD-01`, `BUILD-02` — 7 backend build warnings | **Fixed**, and made unrepeatable by `TreatWarningsAsErrors` in `Directory.Build.props`. **Shipped 2026-08-04.** | ADR-010 |
| Should warnings-as-errors be Release-only? | **No — every configuration.** There is no CI yet (`INFRA-01`), so a Release-only condition would enforce nothing. | ADR-010 |
| Package versions duplicated across `.csproj` files | **Central package management.** Ten packages were versioned in two projects each; drift resolved nearest-wins with no diagnostic. **Shipped 2026-08-04.** | ADR-011 |
| Should a NuGet advisory fail the local build? | **Yes, accepted.** `TreatWarningsAsErrors` elevates `NU1903`, delivering `R-04`'s vulnerability gate earlier. Escape hatch recorded in ADR-010 if it becomes obstructive. | ADR-010 |
| `RecipeManager.Api.csproj.user` committed | **Untracked**, and `*.user` added to `.gitignore` — it carried one developer's debug profile. Fixed 2026-08-04. | — |
| `BUILD-03` — `npm run lint` could not start | **Fixed** by adding `jiti`; ESLint 9 loads a TypeScript flat config through it. Unable to start since 2025-08-08. **Shipped 2026-08-08.** | ADR-012, `R-03` |
| `BUILD-04` — `npm run build` did not type-check | **Fixed**: `"build": "tsc -b && vite build"`, plus a `typecheck` script. **Shipped 2026-08-08.** | ADR-012, `R-03` |
| `INFRA-01` — no CI pipeline | **Fixed.** `.github/workflows/ci.yml` runs build, test, typecheck, lint, and both vulnerability checks on every PR. Every gate verified by negative test. **Shipped 2026-08-08.** | ADR-013, `R-04`; residual `INFRA-07` |
| `INFRA-06` — Smart App Control blocks the integration tests | **Resolved as designed.** The 14 integration tests run on a clean `ubuntu-latest` runner where no Application Control policy applies. It was always an environment constraint rather than a defect, so the fix was to run them somewhere the constraint does not exist. Local Windows runs remain unreliable straight after an Api change; CI is now the authority. | ADR-013, `R-04` |
| `BUILD-07` — Node version not pinned | **Fixed.** `.nvmrc` (24) and `engines: { node: ">=20" }`. The workflow reads `node-version-file: .nvmrc`, so CI and a developer's machine cannot disagree — the two values say different things deliberately: what is *used* versus what is *supported*. | ADR-013, `R-04` |
| Should CI build Release or Debug? | **Debug.** ADR-005 makes the `IntegrationTest` environment throw in RELEASE builds, so a Release CI build fails all 14 integration tests by design (measured: 84 → 70). `TreatWarningsAsErrors` is unconditional, so the warning gate is identical in Debug. | ADR-013, ADR-005 |
| `SEC-03` — 68 open Dependabot alerts (13 npm advisories, `axios` the largest) | **Fixed** by `npm audit fix`. Every advisory resolved **within the declared semver ranges** — `package.json` did not change, only `package-lock.json`. The entry's fear that `react-router` and `vite` were "majors-adjacent" was wrong: all bumps were minor (`axios` 1.10→1.19, `react-router` 7.7→7.18, `vite` 7.0→7.3). Verified by clean `npm ci` + typecheck + lint + build, and by exercising routing, search, and theming in a browser against a live API. **Shipped 2026-08-08.** | — |
| `BUG-08` — `console.log` in shipped code | **Removed**, and `no-console` added to the ESLint config — the rule had never been configured, so the entry's claim that lint "would have caught" them was wrong. **Shipped 2026-08-08.** | ADR-012, `R-03` |
| Was the ESLint config lintable as written? | **No.** Type-aware rules were applied to `**/*.{ts,tsx}` while `tsconfig.json` includes only `src`, so `eslint.config.ts` and `vite.config.ts` were parse errors. Typed rules now scope to `src/**`; root tooling files lint without type information. | ADR-012 |
