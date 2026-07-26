# Known issues & backlog

Every **defect and gap in what already exists**. Planned work that does not exist yet lives in
[roadmap.md](roadmap.md); the two files cross-reference each other.

Verified against `main` @ `edfd057` on 2026-07-26 by running the real toolchain — not by reading code.

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
| Backend build | `dotnet build RecipeManager.sln` | 0 errors, **7 warnings** — all in `RecipeManager.UnitTests` |
| Backend tests | `dotnet test RecipeManager.sln` | **78 passing** (70 unit + 8 integration), 0 failing |
| NuGet vulnerabilities | `dotnet list package --vulnerable --include-transitive` | **none**, all six projects clean |
| Frontend type-check | `npx tsc --noEmit` | **0 errors** |
| Frontend build | `npm run build` | succeeds — but does **not** type-check ([BUILD-04](#build-04)) |
| Frontend lint | `npm run lint` | **fails to start** on a clean install ([BUILD-03](#build-03)) |
| npm vulnerabilities | `npm audit` · Dependabot API | **17 packages** (12 high) · **68 open alerts** — 32 high, 32 medium, 4 low ([SEC-03](#sec-03)) |
| Frontend tests | — | **none exist**, no runner installed ([TEST-01](#test-01)) |

**Goal: zero warnings across every project.** The seven backend warnings ([BUILD-01](#build-01),
[BUILD-02](#build-02)) are all in test code, all trivially fixable, and all introduced by the package upgrades
in the .NET 10 migration — they were not present on the .NET 8 package set.

---

## Summary

| ID | Severity | Area | Issue |
| --- | --- | --- | --- |
| [BUILD-01](#build-01) | Medium | Build | 5× `CS8602` in unit tests — NSubstitute 6 nullable `Arg.Is<T>` |
| [BUILD-02](#build-02) | Medium | Build | 2× `xUnit1012` — `[InlineData(null)]` on a non-nullable `string` |
| [BUILD-03](#build-03) | **High** | Tooling | `npm run lint` cannot run — `jiti` missing from `devDependencies` |
| [BUILD-04](#build-04) | **High** | Tooling | `npm run build` does not type-check |
| [BUILD-05](#build-05) | Medium | Perf | `mainPhoto.png` is 2.1 MB — 5.7× the entire JS bundle |
| [BUILD-06](#build-06) | Low | Tooling | `run-coverage.ps1` measures only the unit-test project |
| [BUILD-07](#build-07) | Low | Tooling | Node version not pinned |
| [SEC-01](#sec-01) | **Critical** | Security | No authentication at all |
| [SEC-02](#sec-02) | **Critical** | Security | No authorization / no recipe ownership |
| [SEC-03](#sec-03) | **High** | Security | 68 open Dependabot alerts across 14 npm packages; `axios` alone is 29 of them |
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
| [BUG-08](#bug-08) | Low | Frontend | `console.log` left in shipped code |
| [BUG-09](#bug-09) | Low | Frontend | Error recovery does a full page reload |
| [TEST-01](#test-01) | **High** | Tests | No frontend test runner or tests |
| [TEST-02](#test-02) | **High** | Tests | Cache invalidation has no dedicated test |
| [TEST-03](#test-03) | Medium | Tests | Instruction ordering never asserted |
| [TEST-04](#test-04) | Low | Tests | `Location` header on 201 never asserted |
| [TEST-05](#test-05) | Low | Tests | No agreed coverage threshold |
| [TEST-06](#test-06) | Medium | Tests | Integration tests cannot reproduce Npgsql behaviour |
| [INFRA-01](#infra-01) | **High** | CI/CD | No CI pipeline |
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

### BUILD-01
**5× `CS8602: Dereference of a possibly null reference` — Medium**

| File | Line |
| --- | --- |
| `RecipeManager.UnitTests/Application/Handlers/CreateRecipeHandlerTests.cs` | 55 |
| `RecipeManager.UnitTests/Application/Handlers/DeleteRecipeHandlerTests.cs` | 56 |
| `RecipeManager.UnitTests/Application/Handlers/UpdateRecipeHandlerTest.cs` | 63, 233, 275 |

**Cause.** All five are `Arg.Is<Recipe>(r => r.Title == …)` inside a `Received(1)` assertion. NSubstitute 6.0
annotates the `Arg.Is<T>` predicate parameter as nullable, so `r` is `Recipe?` and every member access warns.
Introduced by the NSubstitute 5.3 → 6.0 upgrade in the .NET 10 migration.

**Fix.** Guard inside the predicate — this also makes the assertion honest:
```csharp
Arg.Is<Recipe>(r => r != null && r.Title == command.Title)
```
Do **not** silence it with `r!` or `#pragma warning disable`.

**Owner:** `02-senior-csharp` · **Effort:** ~15 min

### BUILD-02
**2× `xUnit1012: Null should not be used for type parameter` — Medium**

`RecipeManager.UnitTests/Domain/Entities/RecipeTests.cs:76` (`invalidTitle`) and `:96` (`invalidDescription`).

**Cause.** `[InlineData(null)]` on a `[Theory]` whose parameter is a non-nullable `string`. The test is
deliberately exercising the null case — the signature is what is wrong.

**Fix.** Widen the parameter and pass it through:
```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public void Create_WithInvalidTitle_ShouldReturnFailureResult(string? invalidTitle)
```
`Recipe.Create` already takes a non-nullable `string`, so the call site needs `invalidTitle!` — which is
correct here, since passing null *is* the scenario under test.

**Owner:** `02-senior-csharp` · **Effort:** ~10 min

### BUILD-03
**`npm run lint` cannot run on a clean install — High**

```
Error: The 'jiti' library is required for loading TypeScript configuration files.
```

**Cause.** The ESLint config is `eslint.config.ts` (TypeScript). ESLint 9 needs `jiti` to load a TS config, and
`jiti` is **not** in `devDependencies` and is not pulled in transitively. Reproduced from a clean
`npm install` on `main` @ `edfd057` (2026-07-26).

**When it broke.** The config was `eslint.config.js` until commit `3474a8b` (2025-08-08, "Convert from
javascript to typescript"), which renamed it to `.ts` without adding `jiti`. `git log -S'jiti'` shows the
package has never appeared in `package.json`, so lint has been unable to start since that commit.

**Impact.** Every ESLint rule the project configured — `@typescript-eslint/no-unused-vars`, `react-hooks`, and
the type-checked rule sets — has been unenforced since then. This is why [BUG-08](#bug-08) survived.

**Fix.** Either add `jiti` to `devDependencies`, or rename the config to `eslint.config.js`/`.mjs`. Adding
`jiti` preserves the type-checked config and is the smaller change. Then run `npm run lint` and fix whatever it
reports — that result is currently unknown and may add entries to this file.

**Owner:** `03-senior-react` · **Effort:** ~10 min + unknown follow-up

### BUILD-04
**`npm run build` does not type-check — High**

`"build": "vite build"` in `package.json`. Vite transpiles with esbuild and **strips types without checking
them**, so a TypeScript error does not fail the build.

**Impact.** The frontend has no automated type safety gate. Combined with [BUILD-03](#build-03), *nothing*
currently validates the frontend in CI-equivalent terms. This matters most for the contract bugs
([BUG-01](#bug-01)–[BUG-03](#bug-03)): fixing the TS types will surface real errors that must be caught
somewhere.

**Verified.** `npx tsc --noEmit` currently passes with 0 errors — the code is type-clean today, it is just
unguarded.

**Fix.** `"build": "tsc -b && vite build"` (the Vite React-TS template default), and add a
`"typecheck": "tsc --noEmit"` script for fast local checks.

**Owner:** `03-senior-react` · **Effort:** ~10 min

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

The script runs `dotnet test RecipeManager.UnitTests` and reports on that alone, so the 8 integration tests
contribute nothing and the reported percentage understates real coverage — particularly for `Api` and
`Infrastructure`, which unit tests never touch.

**Fix.** Point it at `RecipeManager.sln` and merge both coverage outputs, or rename the script and state the
limitation in `README.md`.

**Owner:** `06-qa-tester` · **Effort:** ~30 min

### BUILD-07
**Node version not pinned — Low**

No `engines` field in `package.json`, no `.nvmrc`. `README.md` asks for "20 LTS or newer"; the machine this was
verified on runs Node 24.18 / npm 11.16. The .NET SDK *is* pinned (`global.json`), so this is an inconsistency.

**Fix.** Add `"engines": { "node": ">=20" }` and an `.nvmrc`.

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

### SEC-03
**68 open Dependabot alerts across 14 npm packages — High**

Two tools, two numbers, both correct — they count different things:

| Source | Reports | Counts |
| --- | --- | --- |
| `npm audit` | **17 vulnerabilities** (12 high, 3 moderate, 2 low) | affected *packages* |
| Dependabot (`gh api repos/guillerlp/RecipeManager/dependabot/alerts`) | **68 open alerts** — 32 high, 32 medium, 4 low | one alert per *advisory per package* |

Quote the Dependabot figure when talking about the repo, since that is what the GitHub UI shows. All 68 are
**npm**, all in `RecipeManager/recipe-manager-frontend/package-lock.json`. A further 13 alerts are
`auto_dismissed`.

**Where the risk actually sits.** 46 of the 68 are `runtime`-scope — they ship to the browser. The remaining 22
are `development`-scope, reachable only through the build toolchain:

| Package | Alerts | Severity | Scope | Direct? |
| --- | --- | --- | --- | --- |
| `axios` | **29** | high, medium, low | runtime | **direct** |
| `react-router` | 14 | high, medium | runtime | transitive (via `react-router-dom`) |
| `vite` | 7 | high, medium, low | development | **direct** |
| `js-yaml`, `postcss` | 3 each | high, medium | development | transitive |
| `brace-expansion`, `minimatch`, `picomatch` | 2 each | high / medium | development | transitive |
| `follow-redirects`, `form-data`, `yaml` | 1 each | high / medium | runtime | transitive |
| `@babel/core`, `flatted`, `rollup` | 1 each | low / high | development | transitive |

**`axios` alone is 43% of the total** and is the app's only HTTP client, on a direct runtime dependency.
Advisories include prototype-pollution gadgets enabling response hijacking and full MitM via `config.proxy`.

**Why High and not Critical.** The headline `axios` advisory is credential theft — and this app has no
authentication, so there are no credentials to steal (see [SEC-01](#sec-01)). The MitM and prototype-pollution
paths remain real. If [SEC-01](#sec-01) is ever closed, **re-rate this to Critical**, because auth tokens would
then be exactly what the advisory targets.

**Fix.** `npm audit fix` claims to resolve them. Do it in its own PR, then `npm run build`, `npx tsc --noEmit`,
and manually exercise the app — `react-router` and `vite` are majors-adjacent and this is the kind of upgrade
that breaks routing silently.

**Backend is clean:** `dotnet list package --vulnerable --include-transitive` reports no vulnerable packages in
any of the six projects, and Dependabot opened zero NuGet alerts. Confirmed by both tools independently.

**Owner:** `03-senior-react` + `05-security-reviewer` · **Effort:** ~1 h including verification

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

### BUG-08
**`console.log` in shipped code — Low**

`components/common/SearchBar/SearchBar.tsx` logs on every keystroke; `components/ui/Recipe/RecipeList/RecipeList.tsx`
logs on every card click. Both would have been caught by ESLint had it been runnable ([BUILD-03](#build-03)).

### BUG-09
**Error recovery does a full page reload — Low**

`RecipeList`'s retry button calls `window.location.reload()`, discarding all client state. TanStack Query's
`refetch()` is already available from `useRecipes`.

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
through the API. A broken invalidation in `CachedRecipeRepository` would pass all 78 tests.

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

**Possible fix.** Testcontainers for PostgreSQL — a dependency and a CI decision ([INFRA-01](#infra-01)).

---

## Infrastructure & process

### INFRA-01
**No CI pipeline — High**

No `.github/` directory, no workflow, nothing. Every check in
[workflows/release-workflow.md](workflows/release-workflow.md) is manual and therefore skippable —
[BUILD-03](#build-03) is direct evidence that an unenforced check silently rots.

**Minimum useful pipeline:** `dotnet build` (warnings as errors once [BUILD-01](#build-01)/[BUILD-02](#build-02)
are fixed), `dotnet test`, `npm ci`, `npm run typecheck`, `npm run lint`, `npm run build`. Add
`dotnet list package --vulnerable` and `npm audit` so [SEC-03](#sec-03) cannot silently regrow. Dependabot
*alerting* is already enabled and currently reports 68 open alerts, but nothing acts on it — enabling Dependabot
**pull requests** and failing CI on high-severity alerts is what turns it from a notification into a control.

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

`RecipeManager.Api/Startup/ApplicationInitializer.MigrateDatabase` writes migration progress with `Console.WriteLine`,
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
| CQRS: hand-rolled or MediatR? | **Keep hand-rolled**, and remove its one real drawback by auto-registering handlers with Scrutor (already a dependency). | `R-01`, ADR-001 |
| Integration tests: EF InMemory or a real database? | **Testcontainers with real PostgreSQL**, deferred until CI exists. | `R-06` |
