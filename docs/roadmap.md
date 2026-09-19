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

**Phase 1 is complete.** `R-02` (build hygiene), `R-03` (frontend toolchain), and `R-04` (CI) all shipped;
`R-04` on 2026-08-08 as ADR-013, closing `INFRA-01`, `INFRA-06`, and `BUILD-07`. Phase 2 and Phase 3 items are
therefore no longer gated by the ordering rule above.

One residual is tracked as `INFRA-07`: the workflow runs on every PR but is not yet **required** to merge, which
is a branch-protection setting on `main` rather than anything a PR can contain. Until it is enabled, the
pipeline reports honestly and can be merged past.

---

## Phase 2 — correctness and confidence

`R-06` (Testcontainers for the integration tests) shipped 2026-09-17 as ADR-017, closing `TEST-06`. `R-07`
(frontend test runner) shipped 2026-09-18 as ADR-018, closing `TEST-01`. `R-08` (cache-invalidation tests)
shipped 2026-09-18 as spec 006, closing `TEST-02`. It needed no ADR because it changed no decision, and it
surfaced `BUG-14`. `R-09` (generated TS types) shipped 2026-09-19 as ADR-019, after `BUG-01`–`05` were fixed by
hand. `R-15` (`.editorconfig`) shipped 2026-09-19 as ADR-020: formatting, unused usings, and file-scoped
namespaces now fail the build, and it opened `QUAL-05` for the frontend.

**Phase 2 is complete.**

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
`RecipeDto` changes, so `R-09` (shipped) will flag every client site the change touches.

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

`RecipeDto` has no image field, and `recipe-manager-frontend/src/types/recipe.ts` no longer declares one
(`BUG-04` resolved by dropping it — spec 007), so every card falls back to a 2.1 MB bundled placeholder
(`BUILD-05`).

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
| CI green on every PR — **workflow shipped**; still to make the checks *required* to merge | `INFRA-07` (was `INFRA-01`, `R-04`) |
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
- **A component library (MUI or similar).** The app themes via `data-theme` plus CSS variables, and ADR-014
  removed MUI because its whole footprint was two `Box`es and four icons. Re-adopting one would mean two token
  systems and runtime-injected styles competing with CSS Modules. Reconsider only when a screen needs components
  that are genuinely expensive to build accessibly (a date picker, a modal dialog).
- **Distributed cache.** `IMemoryCache` behind `ICacheService` is correct for a single instance. The port
  already exists, so swapping `MemoryCacheService` is the only change needed if the app ever scales out.
