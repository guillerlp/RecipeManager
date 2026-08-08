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

### R-04
**CI pipeline** · `01-architect` → implementation · ~3 h

Nothing enforces any checklist today. `R-03` is the proof: `npm run lint` could not start for eleven months and
nothing reported it, because a check that nobody runs and a check that passes look the same from outside.

Minimum GitHub Actions workflow on every PR:

```
dotnet build (already warnings-as-errors, ADR-010) · dotnet test
npm ci · npm run typecheck · npm run lint · npm run build
dotnet list package --vulnerable --include-transitive · npm audit
```

Every one of those npm scripts now exists and passes — `R-03`/ADR-012 made them runnable. This item is what
makes anyone actually run them.

Dependabot **alerting** is already on but nothing acts on it. Add Dependabot **pull requests** for both
ecosystems and fail the build on high-severity alerts — that is what turns a notification into a gate. Closes
`INFRA-01`.

**`SEC-03` was remediated first, deliberately, so this gate can land blocking.** With 68 alerts open, a
blocking `npm audit` step would have gone red on the PR that introduced it, and every subsequent PR would have
merged over a red check — which teaches people to ignore red, the exact failure mode `BUILD-03` demonstrated.
The audit baseline is now 0, so any failure here is a genuine regression.

**Decide NuGet lock files here, not before.** The frontend restores from a committed `package-lock.json`; the
backend has no equivalent, so the transitive graph is resolved fresh on every restore and CI could legitimately
get a different closure than a developer did. `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`
in `Directory.Build.props` plus committed `packages.lock.json` files fixes that, and `--locked-mode` in CI makes
an unexpected change fail rather than pass silently. It is deliberately deferred to this item because a lock
file only earns its maintenance cost once something reproducible consumes it.

Also closes `INFRA-06`: on a Windows machine with Smart App Control enabled the 14 integration tests cannot load
the freshly built `RecipeManager.Api.dll` at all, so they are unreliable locally right after a backend change. A
Linux runner has no such policy, which makes CI the **only** trustworthy place to run them until then.

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
and its four states; `ThemeProvider` persistence and the `useTheme` guard; `NavLink` active-state matching.
Unblocked — `R-03` shipped 2026-08-08.

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

### R-15
**Adopt an `.editorconfig`, or drop `EnforceCodeStyleInBuild`** · `01-architect` → `02-senior-csharp` · ~2 h

`Directory.Build.props` sets `EnforceCodeStyleInBuild` (ADR-010), but there is no `.editorconfig` anywhere in
the repo, so IDE style rules sit at their default suggestion severity and **nothing is currently enforced**. The
property is a latch, not a control: it does nothing until an `.editorconfig` exists, and then it does a great
deal at once.

That is the whole difficulty. With `TreatWarningsAsErrors` also on, any rule set to `warning` becomes a **build
error in every file simultaneously**. This is the same shape as `UX-02` — a change that shifts every existing
file and must therefore be one deliberate pass, never a side effect of another PR.

**Decide in this order:**

1. Which rule families are wanted (`IDE0055` formatting, `IDE0005` unused usings, naming rules, `var` preference)
   — the repo's existing style is the reference, not a blog post's.
2. What severity each gets. `suggestion` enforces nothing; `warning` is a build break under ADR-010. Starting
   everything at `suggestion` and promoting deliberately is the low-risk path.
3. Whether the one-off reformat lands as its own commit, so review can separate it from behaviour changes.

Ending with "we do not want this" is a legitimate outcome — then **delete `EnforceCodeStyleInBuild`**, because a
property that enforces nothing while looking like a gate is worse than no property at all.

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
| ~~npm vulnerabilities resolved~~ — **closed 2026-08-08**, `npm audit` reports 0 | `SEC-03`, settled |
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

- **MediatR.** The hand-rolled CQRS is intentional (ADR-001, commit `05656ed`). ADR-008 removed its only real
  drawback by auto-registering handlers with Scrutor. Reconsider only if pipeline behaviours become genuinely
  necessary.
- **AutoMapper.** One hand-written mapping extension is clearer and faster than a mapping configuration.
- **A global frontend store (Redux/Zustand).** TanStack Query owns server state and Context owns UI state;
  there is no client state that needs either.
- **A MUI `ThemeProvider`.** The app themes via `data-theme` plus CSS variables. Adopting MUI theming would
  mean maintaining two token systems.
- **Distributed cache.** `IMemoryCache` behind `ICacheService` is correct for a single instance. The port
  already exists, so swapping `MemoryCacheService` is the only change needed if the app ever scales out.
