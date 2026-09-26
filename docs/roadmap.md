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

## Phase 3 — the editorial app and domain evolution

Re-sequenced on 2026-09-19 around the **editorial design**: the canonical visual reference for every screen,
kept in Claude Design (see [agents/07-ux-ui.md](agents/07-ux-ui.md#canonical-design-reference)). It covers six
screens (Home, Recipes, Detail, Add/Edit, Profile & settings, Cooking mode) in light and dark, plus mobile for
three of them. It assumes structured ingredients throughout, so most of its screens **depended on** `R-10` (now
shipped) rather than preceding it. The reasoning is in [decisions-log.md](decisions-log.md#2026-09-19--a-ui-design-is-a-dependency-graph-in-disguise).

`R-16` (editorial design system and shell) shipped 2026-09-20 as ADR-021
([spec 009](specs/009-editorial-design-system-and-shell.md)), closing `UX-01`, `UX-02`, `UX-03`, `BUG-06`,
`BUILD-05`, `DEC-06`, `QUAL-03`, and `BUG-09`. It opened `UX-06` and `UX-07`, and left `BUG-10` open by
design: recipe rows stay `<article>` rather than `<button>` until `R-18` gives them a detail route to link to.

`R-10` (structured ingredients) shipped 2026-09-24 as **ADR-022**
([spec 010](specs/010-structured-ingredients.md)), which supersedes ADR-004. It closed the ingredient half of
`BUG-11` and of `SEC-09`, and known limitation #1 in [domain-model.md](domain-model.md#known-limitations); it
removed `SEC-08`'s architecture blocker by introducing `OnModelCreating` and the first
`IEntityTypeConfiguration<T>`; and it opened `BUG-15` and `TEST-07`. Its entry is gone from this file, and what
the domain looks like now is described as current state in [domain-model.md](domain-model.md). **Every item that
waited on it is now unblocked** — the whole of Phase 3 was gated on this one.

`R-17` (structured instructions) shipped 2026-09-26 as **ADR-023**
([spec 011](specs/011-structured-instructions.md)), implementing the `InstructionStep` shape ADR-022 had fixed.
It closed `BUG-11` (with GitHub [#8](https://github.com/guillerlp/RecipeManager/issues/8)), `SEC-09`, and
`TEST-03`, and removed the last `text[]` from the schema. ADR-023 is the one thing ADR-022 did not foresee: a
create request has no ingredient ids to reference, so requests address ingredients **by index into the same
payload** and responses carry ids. `R-23` is now waiting only on `R-22`.

Build order. Each item names what it waits on, so a later item can move up if its dependencies are met:

| Order | Item | Waits on |
| --- | --- | --- |
| ~~1~~ | ~~`R-10` Structured ingredients~~ — **shipped 2026-09-24**, ADR-022 | — |
| ~~2~~ | ~~`R-17` Structured instructions~~ — **shipped 2026-09-26**, ADR-023 | — |
| 3 | `R-18` Recipe detail screen — **PR 1 shipped 2026-09-26** (ADR-024); PR 2 (units) open | ~~`R-10`~~ — none |
| 4 | `R-19` Draft recipes | ~~`R-10`~~ — none |
| 5 | `R-20` Tags | — |
| 6 | `R-21` Add/edit form | ~~`R-10`~~, ~~`R-17`~~, `R-19`, `R-20` |
| 7 | `R-22` Cook log | — |
| 8 | `R-23` Cooking mode | ~~`R-17`~~, `R-22` |
| 9 | `R-24` Export and import | `SEC-08`, ~~`SEC-09`~~ |

`R-11`, `R-12`, `R-13`, and `R-14` keep their IDs and are unordered relative to the list above; each notes what
the design asks of it. `R-13` is worth doing early, since every screen above is easier to check against realistic
data.

### R-18
**Recipe detail screen** · `07-ux-ui` → `03-senior-react` · ~1 day

Design screen 3c: method in the wide column, ingredients in a sticky rail, and a servings stepper that
rescales every quantity on the client. Closes `BUG-10` (no detail route). Rescaling is presentation only and
never writes back. It rescales `IngredientDto.Quantity`, a `decimal?`; only a null quantity ("salt to taste")
stays unscaled. A quantity with no `unit` ("2 lemons") **scales** — decided 2026-09-26 in
[spec 012](specs/012-recipe-detail-screen.md), correcting an earlier sentence here that gave no reason and
contradicted design 3c. The editorial design's metric/imperial preference also lands here: ADR-022 made
conversion a presentation concern with no canonical stored unit, so the conversion table is written here, on the
client, over the `Unit` enum. (`R-16` did not ship that preference — spec 009 §4 left it out.)

**PR 1 (the screen) shipped 2026-09-26 as ADR-024**, closing `BUG-07` and `BUG-10`. **PR 2 (the As written /
Metric / Imperial preference and the conversion table) remains**, after which this entry is deleted.

### R-19
**Draft recipes** · `01-architect` (ADR required) → `02-senior-csharp` → full stack · ~1 day

The design lets a recipe be saved with only a title and finished later. Today `Recipe.ValidateProperties`
also requires a description, a non-zero time, servings, and at least one ingredient and one step (`UX-05`).
Decided 2026-09-19: add an explicit Draft/Published status. A draft needs only a title. The full invariants
apply on publish and on every update to a published recipe.

The ADR must decide whether drafts appear in `GET /api/recipes` (and the `recipes_all` cache key), whether a
published recipe can return to draft, and what the existing rows become (published, since they already
satisfy the full invariants). The rejected alternatives were keeping the rules and drafting in the browser
only, and relaxing the aggregate to require only a title.

### R-20
**Tags** · `01-architect` → full stack · ~1 day

Freeform labels ("roast", "breakfast", "feeds a table") on a recipe, shown in the list and on the detail
screen, and edited in the form. Removes known limitation #6. Decide the storage (a `text[]` primitive
collection, as `InstructionStep.IngredientIds` is a `uuid[]`, or an owned child table as `Ingredients` and
`Instructions` now are) and normalisation (case, whitespace, duplicates) in
the ADR. ADR-022's reasoning transfers: a `text[]` is cheaper and keeps order for free, a child table is what
makes "every recipe tagged *roast*" an indexable SQL query rather than a client-side scan. Comes before `R-21`
so the form is built once.

### R-21
**Add/edit form** · `07-ux-ui` → `03-senior-react` · ~2 days

Design screen 3d. The title field is typeset as the page title, there is a live preview of the list row, and
ingredients are entered one per line and parsed into quantity, unit, and name. The parser runs **on the client**
and produces an `IngredientInputDto`; the server never parses free text. Its rules are already fixed — spec 010
§9 and ADR-022 — against the `Unit` enum that now exists. Adds the SPA's first mutation hooks (the `['recipes']`
invalidation pattern in the feature workflow) and gives `/recipes/new` a real screen in place of the `R-16` 404
fallback. The existing checklist in [agents/07-ux-ui.md](agents/07-ux-ui.md) still applies: keyboard reorder,
visible limits, cross-field errors, and 400/422 mapping. Three things the form must not get wrong. **Step
references are sent as indexes, read as ids** (ADR-023): at submit time, translate each step's selected
ingredient ids into indexes into the exact `ingredients` array being sent in that request. `number[]` against
`string[]` stops a direct copy, but nothing stops an index computed against a stale array. It should echo each
ingredient's `id` so ingredients keep their identity, though step references no longer depend on it. And it must
not invent ids, because nothing server-side validates that one belongs to the recipe (`BUG-15`). "Draft saved" in the design depends on `R-19`. The photo field depends on
`R-12`.

### R-22
**Cook log** · `01-architect` (ADR required) → full stack · ~1–2 days

Record each time a recipe is cooked. This feeds "Cooked 11 times", Home's "Last cooked" rail and
"never cooked yet" view, and the Profile stats (total cooks, most cooked). It is probably a child collection
of `Recipe` or a separate aggregate. If it is a separate aggregate, it forces the unit-of-work decision
ADR-006 deferred, so plan that here, not after. Decide whether logging a cook invalidates the recipe's cache
entries.

### R-23
**Cooking mode** · `07-ux-ui` → `03-senior-react` · ~2 days

Design screen 3f. It deliberately breaks the paper palette: warm near-black with an amber accent, for reading
at arm's length. One step at a time with a large type size, only that step's ingredients, and the ones
already used struck through. It has a step timer, and uses the Screen Wake Lock API behind the Settings
preference. Finishing logs a cook (`R-22`). The accent pair must be contrast-checked as its own palette,
since it is not a theme.

### R-24
**Export and import** · `01-architect` + `05-security-reviewer` (both required) → full stack · ~1–2 days

Export the whole catalogue as JSON, and import it back. Import is a bulk write from a user-supplied file, so
the database-level length limits (`SEC-08`; `SEC-09` closed with `R-17`) must exist first. Otherwise one
crafted file stores values no API request could. It must also go **through the aggregate** rather than bulk SQL:
step-to-ingredient references are a `uuid[]` with no foreign key, so only `Recipe.ValidateProperties` stops a
step pointing at an ingredient that does not exist (ADR-022/023). It also needs a size cap, schema versioning of the export format, and a
duplicate policy (skip, replace, or copy). "Print the whole catalogue" in the design is not part of this item:
it is `R-25`.

### R-25
**Print the whole catalogue** · `07-ux-ui` → `03-senior-react` · ~½ day

The Settings row in the editorial design (3e). Needs a route that renders every recipe in full, reusing
`R-18`'s print stylesheet, and today it would lean on the unpaginated `GET /api/recipes` (`SEC-07`) — build it
after `R-11`, or accept that cost explicitly. Split out of `R-18` on 2026-09-26 (spec 012).

### R-11
**Pagination and server-side search** · `01-architect` → `02-senior-csharp` + `03-senior-react` · ~1 day

`GET /api/recipes` returns the entire table, unpaginated, mapped in full, cached under a single `IMemoryCache`
key (`SEC-07`), and the SPA filters it in the browser. This is fine at 20 recipes and untenable at 2,000.

Design the pagination contract and the cache-key strategy **together** — paginating invalidates the current
single-key `recipes_all` approach. Ingredient search is now a join against the `"RecipeIngredients"` child table
(ADR-022) rather than an array scan, so it is an ordinary indexable SQL query — which is precisely why ADR-022
rejected `jsonb`. Note that `R-10` **worsened `SEC-07` in degree**: each recipe's payload grew, and this
endpoint is still unpaginated.

The editorial design asks for "Load the rest" on the Recipes screen, and three saved views on Home ("under 30
minutes", "feeds a table", "never cooked yet"). Design the query contract so those are filters on the same
endpoint, not three new endpoints. The last one needs `R-22`.

### R-12
**Recipe images** · `01-architect` + `05-security-reviewer` (both required) → full stack · ~1 day

`RecipeDto` has no image field, and `recipe-manager-frontend/src/types/recipe.ts` no longer declares one
(`BUG-04` resolved by dropping it — spec 007), so every card falls back to the CSS hatch placeholder introduced
by `R-16` Task 10 (`BUILD-05`, resolved) instead of a real photo.

**Do not start without the security requirements** in
[agents/05-security-reviewer.md](agents/05-security-reviewer.md#recipe-image-upload--none-exists-yet-requirements-if-one-is-added):
content validation by magic bytes, server-side re-encode, size and rate limits, format allow-list, generated
filenames, storage outside the web root, EXIF stripping.

Decide first whether images are uploaded or referenced by URL — a URL field is a fraction of the work and may
be enough. The editorial design leans that way: its photo slot reads "drop or paste a URL", is optional on every
screen, and no screen needs a photo to look finished.

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
| Length limits enforced at the database, not only in FluentValidation — ingredients and steps done; `Title`/`Description` remain | `SEC-08` (`SEC-09` closed 2026-09-26) |
| `GET /api/recipes` paginated | `SEC-07`, `R-11` |
| Health/readiness endpoint | `SEC-11` |
| CI green on every PR — **workflow shipped**; still to make the checks *required* to merge | `INFRA-07` (was `INFRA-01`, `R-04`) |
| Versioning scheme and a tested rollback procedure — migrations apply themselves at startup | `INFRA-02`, `INFRA-03` |
| Real production `VITE_API_URL` and a documented frontend host | `INFRA-04`, `SEC-12` |

`R-14` **Authentication and ownership** · `01-architect` + `05-security-reviewer` · ~3–5 days — the largest
single item on this list. Requires choosing the identity source (ASP.NET Core Identity vs. an external IdP),
adding a `User` aggregate, adding `OwnerId` to `Recipe` with a migration for existing rows, filtering every
query, and wiring auth through the SPA. Do not start it as a side effect of another feature. The editorial
design reserves an "Account" block in Profile & settings (design screen 3e), shown disabled until this exists.

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
