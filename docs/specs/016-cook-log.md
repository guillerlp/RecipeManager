# Spec: Cook log

| | |
| --- | --- |
| **ID** | `016` |
| **Status** | in progress — PR 1 (backend, contract, Detail) open; PR 2 (Home, Profile) to follow |
| **Author** | `00-leader` + `01-architect` |
| **Created** | `2026-10-11` |
| **Branch** | `feat/cook-log` (PR 1), `feat/cook-log-stats` (PR 2) |

---

## 1. Context

Nothing records that a recipe was cooked. The editorial design leans on that record in four places: "Cooked 11
times" on Detail, Home's masthead (`24 recipes · 78 cooks`) and its "Last cooked" rail, and Profile's "Cooked"
and "Most cooked" stats. Spec 009 left all of them out rather than stub them, and `R-23` (cooking mode) needs
somewhere to log a cook when it finishes. `R-22` adds the record, and must decide whether it is part of `Recipe`
or an aggregate of its own — the latter being the first time ADR-006 (no unit of work) is put to the test.

## 2. Goal

Each time a recipe is cooked can be logged, backdated, and undone, and every screen the design names shows the
counts and dates derived from those logs.

## 3. In scope

**PR 1 — backend, contract, Detail screen** (`feat/cook-log`)

- [x] `Cook` entity: its own aggregate root, `RecipeId` + `CookedAt` (§6).
- [x] `CookErrors.CookedAtInFuture()` (422) and `CookErrors.CookNotFound(id)` (404).
- [x] `ICookRepository` (Domain) and `CookRepository` (Infrastructure). No cache decorator (§7, §9).
- [x] `"Cooks"` table with a cascading, indexed FK to `"Recipes"`; migration `AddCooks`.
- [x] `LogCookCommand` / `DeleteCookCommand` and their handlers; `TimeProvider.System` registered as a singleton.
- [x] `CooksController` at `api/recipes/{recipeId:guid}/cooks`: `POST` and `DELETE /{cookId:guid}`.
- [x] `RecipeDto` gains `CookCount` and `LastCookedAt`, filled by both read handlers from one stats query.
- [x] Regenerated `contracts/openapi.json` and `src/types/generated/api.ts`; `Cook` and `LogCookRequest` aliases
      in `src/types/recipe.ts`; `logCook` and `deleteCook` in `services/recipeService.ts`.
- [x] `useLogCook` and `useDeleteCook` in `hooks/useRecipeMutations.ts`.
- [x] Detail: a "Cooked" stat, an "I cooked this" button with Undo, and a "Cooked on another day?" disclosure.
- [x] `utils/relativeDate.ts`, used by the Cooked stat (and by PR 2).
- [x] ADR-028, `domain-model.md`, `architecture.md`, `decisions-log.md`, `learning-mode.md` concept row,
      `known-issues.md` (`BUG-22`, `UX-20`), and the test counts in `CLAUDE.md`.

**PR 2 — Home and Profile** (`feat/cook-log-stats`)

- [ ] `utils/cookStats.ts`: `{ totalCooks, mostCooked, recentlyCooked }` from the published and draft lists.
- [ ] Home: `· N cooks` in the masthead (hidden at 0) and a "Last cooked" aside of up to three recipes (hidden
      when nothing has been cooked).
- [ ] Profile: a stats row with "Cooked" and "Most cooked".
- [ ] `R-22` removed from the roadmap; spec 009's "until `R-22`" notes resolved; this spec set to shipped.

## 4. Out of scope

- **A list of a recipe's cooks** (`GET …/cooks`) and **editing a cook's date.** Undo covers the mistaken tap;
  delete-and-relog covers a wrong date. Without a list, a cook cannot be removed from the UI once the page is
  left — filed as `UX-20`.
- **Notes, ratings, or servings per cook.** A cook is a date (§14). Any of them is a nullable column later.
- **The "never cooked yet" saved view** and the other "Jump to" links on Home. They are filters on the list
  endpoint and belong to `R-11`, which can sort and filter on the `cookCount` / `lastCookedAt` this spec adds.
- **Logging a cook from cooking mode.** `R-23` calls the `POST` this spec builds.
- **A server-side stats endpoint.** Home and Profile derive their numbers in the browser from lists the SPA
  already loads, which only works while `GET /api/recipes` is unpaginated (`SEC-07`). `R-11` replaces it.
- **Profile's "Median time" tile.** It is in the same row of the design but is not cook data; noted as a PR
  follow-up rather than built here.
- **Caching cook stats.** See §7.
- **`07-ux-ui` screen spec.** The design already specifies the Detail stat, the Home rail, and the Profile stats;
  the only new controls (the button, Undo, the date disclosure) follow the existing Detail patterns, and the
  reasoning for each is in §8.

## 6. Domain impact

- **New entity:** `Cook` (`RecipeManager.Domain/Entities/Cook.cs`), sealed, derives from `Entity`. A **second
  aggregate root**, not an owned entity of `Recipe` (§9).

  | Property | C# type | PostgreSQL column | Notes |
  | --- | --- | --- | --- |
  | `Id` | `Guid` | `uuid`, PK | Minted by the domain; mapped `ValueGeneratedNever()`, as `Ingredient` |
  | `RecipeId` | `Guid` | `uuid`, `NOT NULL`, FK → `"Recipes"("Id")` **ON DELETE CASCADE**, indexed | A reference by identity — `Cook` holds no `Recipe` navigation |
  | `CookedAt` | `DateTimeOffset` | `timestamp with time zone`, `NOT NULL` | Normalised to UTC by `Create` |

  `public static Result<Cook> Create(Guid recipeId, DateTimeOffset? cookedAt, DateTimeOffset now)` is the only
  way to build one. A null `cookedAt` means `now`. The entity reads no clock: the handler passes
  `TimeProvider.GetUtcNow()`, so the rule is deterministic under test. A private parameterless constructor exists
  for EF only, as on `Recipe`.
- **New/changed properties on `Recipe`:** none. `Recipe` does not know cooks exist.
- **New invariants:**
  - `CookedAt <= now` → `CookErrors.CookedAtInFuture()`, kind `Validation` (422), field `cookedAt`. No skew
    tolerance: the SPA never sends a date for "just now", only for a past day (§8), so a client clock running
    ahead cannot trip it through the UI.
  - No lower bound. A cook in 1970 is odd, not harmful.
  - Cook exists, *for this recipe* (repository-level) → `CookErrors.CookNotFound(id)`, kind `NotFound` (404),
    field `id`.
  - Recipe exists (repository-level) → the existing `RecipeErrors.RecipeNotFound(id)`.
- **New/changed shape validation:** none. `LogCookDto` has no validator, deliberately: its only field is a
  nullable date, which has no length or bound to check, and an unparseable date is already a 400 at model
  binding. "Not in the future" is a business rule and lives in the domain (global rule 5). This is a stated
  exception to "a validator for every type the controller binds".
- **Migration required:** yes — `AddCooks` creates `"Cooks"`, its index, and its FK. Additive; no existing row
  changes. Its `Down` drops the table **and every logged cook with it** — call this out in the PR description,
  because migrations run at startup (`INFRA-03`).
- **Known limitations touched:** #9 ("no transaction boundary beyond one repository call") still holds, now with
  a second aggregate in the model; its wording is updated to say why it still holds (§9).

## 7. API impact

- **New endpoints** — a new `CooksController`, routed under the recipe:

  | Verb | Route | Request body | Success | Failures |
  | --- | --- | --- | --- | --- |
  | POST | `/api/recipes/{recipeId:guid}/cooks` | `LogCookDto { cookedAt?: string (date-time) }` — `{}` means now | 201 + `Location: /api/recipes/{recipeId}/cooks/{cookId}` + `CookDto { id, cookedAt }` | 400 malformed date, 404 recipe not found, 415 no body (`{}` is "now"; an empty request has no `Content-Type`), 422 `cookedAt` in the future |
  | DELETE | `/api/recipes/{recipeId:guid}/cooks/{cookId:guid}` | — | 204 | 404 cook not found, or not this recipe's |

  There is no `GET` for a single cook, so `Location` identifies a resource that answers only `DELETE`. RFC 9110
  asks `Location` to *identify* the created resource, not to make it retrievable; it is worth a comment in the
  controller, because it reads like a bug.
- **Handlers.**
  - `LogCookHandler`: `IRecipeRepository.GetByIdAsync` (the **cached** read — it is an existence check and the
    recipe is never mutated, so `BUG-14`'s write-path concern does not apply; say so in a comment) → 404 if
    missing; `Cook.Create(recipeId, cookedAt, timeProvider.GetUtcNow())`; `ICookRepository.AddAsync`; return
    `CookDto`.
  - `DeleteCookHandler`: `ICookRepository.GetByIdAsync(recipeId, cookId)` → 404 if missing (which also covers a
    missing recipe, so there is no second lookup); `DeleteAsync`.
  - `GetAllRecipesHandler`, `GetRecipeByIdHandler`: one `ICookRepository.GetStatsAsync(recipeIds)` call after
    loading, merged by `MapToRecipeDto(CookStats?)`. `CreateRecipeHandler` maps with no stats.
- **`ICookRepository`:** `AddAsync`, `GetByIdAsync(Guid recipeId, Guid cookId)`, `DeleteAsync`,
  `GetStatsAsync(IReadOnlyCollection<Guid> recipeIds) → IReadOnlyDictionary<Guid, CookStats>`, where `CookStats`
  is `record (int Count, DateTimeOffset LastCookedAt)` in the Domain beside the interface. One
  `GROUP BY "RecipeId"` query, served by the FK index. Every write calls `SaveChangesAsync` itself (ADR-006).
- **`RecipeDto` changes:** two fields **appended**: `int CookCount` (0 when never cooked) and
  `DateTimeOffset? LastCookedAt` (null when never cooked). New schemas `CookDto` and `LogCookDto`.
- **Breaking for the client?** Additive only. `08-api-contract` ships the regenerated types in PR 1.
- **Cache impact: none — and that is the decision, not an omission.** The recipe cache stores `Recipe`
  entities; cook stats are not on the entity, they are joined in when the DTO is built, after the cache. So
  logging or deleting a cook invalidates nothing, and the stats are always read live. The cost is one extra,
  uncached, indexed query per recipe read. `CookRepository` gets a `ponytail:` comment: add a
  `CachedCookRepository` (Scrutor `Decorate`, as `CachedRecipeRepository`) only if that query shows up in
  profiling.
- **DI:** `services.AddSingleton(TimeProvider.System)` and `ICookRepository` → `CookRepository` (scoped) in
  `RecipeManager.Api/Startup/ServiceInitializer.cs`. Handlers need nothing (ADR-008).

## 8. Frontend impact

**PR 1 — Detail**

- **New routes:** none.
- **`RecipeDetailPage`:**
  - A **Cooked** entry in the existing stats `<dl>`: `11 times` plus `last <time dateTime="…">Thursday</time>`
    when `cookCount > 0`; `never yet` when 0.
  - An **"I cooked this"** button below the stats. On success it becomes `Logged · Undo`, announced through a
    `role="status"` region; Undo deletes that cook's id and restores the button. **Undo is not timed** — it
    stays until the page is left. A disappearing Undo fails WCAG 2.2.1 (Timing Adjustable), and the API puts no
    deadline on a delete, so a timer would be a restriction invented in the UI.
  - A **"Cooked on another day?"** native `<details>` with `<input type="date" max={today}>` and a Log button.
    The chosen day is sent as **local noon** converted to UTC: midnight would fall on the previous UTC day for
    anyone east of Greenwich, and the cook would appear a day early. Native control, no date-picker dependency
    (ADR-014).
  - Error: an inline "Couldn't log this cook. Try again." for any failure. The server's own message is not
    rendered (`SEC-05`).
  - Print stylesheet hides the button and the disclosure; the Cooked stat prints.
- **Hooks:** `useLogCook()` and `useDeleteCook()` in `hooks/useRecipeMutations.ts`, following that file's rule:
  on success, invalidate the `['recipes']` prefix. The count lives on every `RecipeDto`, so list and detail are
  both stale after a log. No optimistic update: the refetch is one small GET, and an optimistic count needs a
  rollback path.
- **`utils/relativeDate.ts`:** formats a past instant relative to now — "today", "yesterday", a weekday, "N weeks
  ago", or an absolute date — on `Intl.RelativeTimeFormat` and `Intl.DateTimeFormat`. The cut-offs are a product
  choice, written by the owner during implementation.
- **States:** loading and error are the page's existing ones; the new controls have idle, pending (button
  disabled), logged (Undo), and failed.
- **Design tokens:** existing only.

**PR 2 — Home and Profile**

- **Data:** both pages read `useRecipes()` **and** `useRecipes('Draft')`. Published-only totals would omit the
  cooks of drafts, and Profile's "Cooked 78" would then disagree with the sum of each recipe's "Cooked N times".
  The draft query already exists (the Drafts view); no new endpoint or hook.
- **`utils/cookStats.ts`:** a pure function returning `totalCooks`, `mostCooked` (highest `cookCount`; a tie goes
  to the later `lastCookedAt`, so the result is deterministic), and `recentlyCooked` (recipes with a
  `lastCookedAt`, newest first, first three). A `ponytail:` comment names its ceiling: it is client-side over an
  unpaginated list, and becomes a server query when `R-11` paginates.
- **Home:** `24 recipes · 78 cooks · yours alone` — the recipe count stays published-only, as now; `· N cooks` is
  hidden at 0. A "Last cooked" aside with up to three rows, each a title link and a `relativeDate`; hidden when
  nothing has been cooked, since the design has no empty state for it.
- **Profile:** a stats row above Preferences — **Cooked** (the total; `none yet` at 0) and **Most cooked** (a link
  and `N times`; `—` with no cooks).

## 9. Architecture impact

- **ADR required:** yes → **ADR-028 — The cook log is a second aggregate referenced by id; stats join outside
  the cache.** Draft for [../architecture.md](../architecture.md):

  > - **Status:** accepted
  > - **Context:** `R-22` needs a record of each cook. The roadmap asked whether that is a child of `Recipe` or a
  >   separate aggregate, and noted that the latter forces the unit-of-work decision ADR-006 deferred.
  > - **Decision:** `Cook` is its own aggregate root in a `"Cooks"` table, referencing `Recipe` by id through a
  >   cascading FK, with its own `ICookRepository`. `RecipeDto` carries `CookCount` and `LastCookedAt`, merged in
  >   by the read handlers from one uncached `GROUP BY` query. ADR-006 stands: no write changes both aggregates,
  >   and deleting a recipe removes its cooks by referential action in one statement.
  > - **Alternatives:** an owned `Cooks` collection on `Recipe` (reuses the write path and its cache
  >   invalidation, but loads every cook ever logged on every recipe read and guards no invariant); a separate
  >   aggregate with its own summary/recent/stats endpoints (cleanest contract, but three endpoints and fetches
  >   for screens that already load the recipe list, and `R-11`'s "never cooked yet" needs the join anyway).
  > - **Consequences:** logging a cook invalidates no cache entry; every recipe read costs one extra query; the
  >   rule that a recipe's cooks die with it lives in the schema, not the domain; Home and Profile aggregate in
  >   the browser until `R-11`. ADR-006's trigger is now precisely "a write that must change two aggregates
  >   atomically" — merely adding an aggregate does not pull it.

- **New dependency:** none. `TimeProvider` is in the base class library (.NET 8+).
- **Layer/dependency changes:** none. `ICookRepository` and `CookStats` in Domain, implementation in
  Infrastructure, handlers in Application — the `IRecipeRepository` arrangement.
- **New DI registrations:** `TimeProvider.System` (singleton) and `ICookRepository` (scoped), in
  `ServiceInitializer`.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Owned `Cooks` collection on `Recipe` | Reuse: `UpdateAsync` and its cache invalidation already exist, and "a recipe's cooks" reads naturally | No `Recipe` invariant needs the cooks, so they only make the aggregate bigger. Every list and detail read would load every cook ever logged — unbounded growth — and every log would rewrite the recipe path and evict its cache |
| Separate aggregate + `/cooks/summary`, `/cooks/recent`, `/cooks/stats` | A clean contract: `RecipeDto` stays pure content, and the recipe cache is untouched by definition | Three endpoints, three contract types, three fetches for screens that already load the recipe list; `R-11`'s "never cooked yet" filter needs cook data beside the recipe regardless |
| Stats as columns on `"Recipes"` (`CookCount`, `LastCookedAt`) maintained on every log | The cheapest reads — no join at all | A denormalised counter is a two-row write on every log and delete, which is exactly the multi-aggregate transaction ADR-006 cannot do; drift between the counter and the rows would be silent |

- **Pattern applied:** *small aggregates, referenced by identity* (Vaughn Vernon, "Effective Aggregate Design")
  — the counterpart of the owned-entity collection in
  `RecipeManager.Infrastructure/Context/Configurations/RecipeConfiguration.cs`. Also *read-model composition*:
  the DTO is assembled from two sources at query time rather than stored.
- **What this makes harder:** one extra query on every recipe read; cascade semantics are invisible to anyone
  reading only the domain; browser-side aggregation breaks the day the list is paginated; a mistaken cook cannot
  be removed once its page is left (`UX-20`).

## 10. Security impact

- **New user-controlled input:** `cookedAt`, a date — bound by `System.Text.Json` (malformed → 400) and checked by
  the domain (future → 422). The route ids are `guid`-constrained.
- **User content rendered in the SPA:** no new user text. Titles in the Home rail and Profile render through
  React, escaped as everywhere else.
- **File upload:** no.
- **Auth/ownership implications:** none new, and the gap carries over: anyone who can reach the API can log or
  delete any cook, as they can edit or delete any recipe (known limitation #3, `R-14`). The log is a few dozen
  bytes per row with no size cap on how many rows; a write flood is the same unauthenticated-write exposure the
  recipe endpoints already have.
- **Config/secrets touched:** none.
- **Standing gaps affected:** none worsened.

## 11. Acceptance criteria

- [ ] Given a recipe, when `POST …/cooks` with `{}`, then 201, a `Location` ending in the new cook's id, a body
      with that id and a `cookedAt` equal to the server's now, and one row in `"Cooks"`.
- [ ] Given a recipe, when `POST …/cooks` with a past `cookedAt`, then 201 and that instant is stored in UTC.
- [ ] When `POST …/cooks` with a future `cookedAt`, then 422 with `field: "cookedAt"` and no row stored.
- [ ] When `POST …/cooks` with `cookedAt: "not-a-date"`, then 400.
- [ ] Given no such recipe, when `POST …/cooks`, then 404.
- [ ] Given a cook of recipe A, when `DELETE /api/recipes/A/cooks/{id}`, then 204 and the row is gone.
- [ ] Given a cook of recipe A, when `DELETE /api/recipes/B/cooks/{id}`, then 404 and the row remains.
- [ ] Given a recipe cooked three times, when `GET /api/recipes/{id}` and `GET /api/recipes`, then `cookCount` is
      3 and `lastCookedAt` is the latest of the three.
- [ ] Given a recipe never cooked, then `cookCount` is 0 and `lastCookedAt` is null.
- [ ] Given a recipe whose list and detail responses are already cached, when a cook is logged, then the next
      `GET`s both show the new count.
- [ ] Given a recipe with cooks, when it is deleted, then its rows in `"Cooks"` are gone.
- [ ] Given a draft recipe, when a cook is logged, then 201 (drafts can be cooked).
- [ ] Detail: "I cooked this" → `Logged · Undo`, the count increments after refetch; Undo restores both.
- [ ] Detail: a backdated day is sent as local noon in UTC; the date input does not allow a future day.
- [ ] Home: masthead shows `N cooks` counting draft and published recipes, hidden at 0; the rail lists up to three
      recipes newest first and is absent with no cooks.
- [ ] Profile: total and most cooked are shown; a tie in count goes to the more recent cook.

## 12. Test plan

- **Domain unit tests** (`RecipeManager.UnitTests/Domain/Entities/CookTests.cs`): null → now; past; exactly now;
  future → `CookedAtInFuture`; a non-UTC offset normalised to UTC.
- **Handler unit tests:** `LogCookHandler` — recipe not found, future date, success, `Received(1)` on `AddAsync`,
  token propagation, `TimeProvider` substituted (`GetUtcNow` is virtual). `DeleteCookHandler` — not found,
  success. Both read handlers — stats merged when present, `0`/`null` when absent. Mapping tests for the two new
  fields.
- **Integration tests** (Testcontainers): every API criterion in §11, including the cascade and the
  cached-then-logged test. The last is the one that pins §7's "no invalidation" decision: moving the stats onto
  the cached entity later would turn only that test red. `CqrsHandlerRegistrationTests` covers the two new
  handlers automatically.
- **Contract:** `OpenApiContractTests` snapshot regenerated.
- **Frontend (Vitest):** the Detail criteria; both hooks invalidate `['recipes']`; table tests for `relativeDate`
  and `cookStats` (tie-break, drafts counted, empty input); Home and Profile with zero and some cooks.
- **Not covered, and why:** the `POST`-races-recipe-delete 500 (`BUG-22`) — a timing window a test cannot open
  deterministically without hooks the code does not have.
- **Manual verification:** against a real PostgreSQL — log, backdate, undo on Detail; confirm Home and Profile;
  delete a recipe and check `SELECT * FROM "Cooks";`.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-028 (§9) |
| 3 | `02-senior-csharp` | domain + application + infrastructure + api + backend tests (PR 1) |
| 4 | `08-api-contract` | regenerated types, aliases, service methods, contract delta (PR 1) |
| 5 | `03-senior-react` | Detail controls and hooks (PR 1); Home and Profile (PR 2) |
| 6 | `06-qa-tester` | tests + coverage statement |
| 7 | `04-code-reviewer` | review, each PR |
| 8 | `05-security-reviewer` | review — the `POST` stores user input |

`07-ux-ui` is not a separate step; see §4.

## 14. Assumptions made

Answered by the owner on 2026-10-11:

- A cook is a recipe and a date — no notes, ratings, or servings.
- The server's now is the default date; a past date may be supplied; a future one is rejected.
- A single cook can be deleted; its date cannot be edited.
- All three surfaces ship, in two PRs.
- Approach A of the three in §9.

Decided without asking — each a risk if wrong:

- **Drafts can be cooked.** Testing a recipe while writing it is a real use; the alternative would be a 422 that
  `R-23` would have to handle.
- **No skew tolerance on the future-date rule.** Safe through the SPA, which never sends a date for "now".
- **Home's recipe count stays published-only** while its cook count includes drafts — the first counts the
  catalogue, the second counts what was cooked.

## 15. Follow-ups

- `BUG-22` — `POST …/cooks` racing a delete of the same recipe: the existence check passes, the insert violates
  the FK, and the response is 500 rather than 404.
- `UX-20` — no way to remove a cook from the UI after leaving the page that logged it.
- Profile's "Median time" tile (§4) — a PR follow-up note, not an issue.

## 16. Known issues and roadmap items touched

- Fixes: `R-22`. Clears spec 009's deferred masthead cook count and "Last cooked" rail.
- Opens: `BUG-22`, `UX-20`.
- Unblocks: `R-23` (its only remaining dependency); `R-11`'s "never cooked yet" view.
- On the [deploy gate](../roadmap.md#deploy-gate)? no.
