# Spec: Cache-invalidation tests through the HTTP client (R-08)

| | |
| --- | --- |
| **ID** | `006` |
| **Status** | approved |
| **Author** | `00-leader`, owned by `06-qa-tester` |
| **Created** | 2026-09-18 |
| **Branch** | `test/cache-invalidation-tests` |

---

## 1. Context

*(Written before implementation. The present tense describes the state this item started from.)*

`CachedRecipeRepository` decorates `IRecipeRepository` (ADR-003) and invalidates `recipes_all` and
`recipe_{id}` by hand on every write. Nothing verifies that invalidation (`TEST-02`, rated High). The unit tests
mock `IRecipeRepository`, so they never reach the decorator. The integration tests assert **database** state
after a write and never read back through the API. A write method that forgets to invalidate therefore passes
all 99 tests, while users see stale data for up to ten minutes (`CacheDuration.DefaultExpiration`).

`R-06` (ADR-017) made this cheap: integration tests now run the real pipeline against real PostgreSQL. Each test
also gets its own `WebApplicationFactory`, so each test gets its own `IMemoryCache`.

## 2. Goal

Every cache invalidation in `CachedRecipeRepository` is covered by an integration test that goes red when that
invalidation is removed. A separate test proves caching is actually on.

## 3. In scope

- [ ] `RecipeManager.IntegrationTests/RecipeCacheTests.cs`: a new class holding the four tests in section 12.
- [ ] Manual mutation check: remove each invalidation line in turn, confirm the matching test fails, restore
      the line. The results are reported in the PR description.
- [ ] Docs listed in section 16.

## 4. Out of scope

- **Fixing cached-instance aliasing** (section 6, recorded as `BUG-14`). It changes production code, so global
  rule 9 applies.
- **`TEST-03`** (instruction order) and **`TEST-04`** (`Location` header). They are in the same test project
  but are separate items.
- **Expiry behaviour** (`DefaultExpiration`, sliding windows). Testing it needs a controllable clock, and
  `MemoryCacheService` has no seam for one.
- **Cache-failure paths** (`SetCache`/`RemoveCache` swallowing exceptions). `IMemoryCache` does not fail in
  practice, and faking a failure means replacing `ICacheService`, which belongs at unit level.
- **Unit tests for `CachedRecipeRepository`.** They could pin which keys are removed. The integration level was
  chosen deliberately, because it tests what users observe (see section 9).

## 5. Open questions

None open. The user answered all of these on 2026-09-18:

| # | Question | Answer |
| --- | --- | --- |
| 1 | Add a positive cache-hit test on top of the roadmap's three scenarios? | **Yes.** Without it, every invalidation test passes when caching is disabled. |
| 2 | Aliasing makes "update → detail" insensitive to per-id invalidation. How do we test update? | **Assert list and detail**, comment that the detail assertion alone is not sensitive, and record the aliasing as a defect. |
| 3 | Where do the tests live? | **A new `RecipeCacheTests` class.** |

## 6. Domain impact

None. No entity, invariant, or migration changes.

**A defect found while designing this item, recorded rather than fixed:** `IMemoryCache` stores object
references. `UpdateRecipeHandler` calls `GetByIdAsync`. On a cache hit, that returns *the same* `Recipe`
instance the cache holds, and `Recipe.Update(...)` mutates it in place **before** `UpdateAsync` persists it. Two
consequences:

1. If `SaveChangesAsync` throws after the mutation, `recipe_{id}` serves values that were never persisted,
   until the entry expires. This is the defect, recorded as `BUG-14`.
2. A test of "update → `GET /api/recipes/{id}` shows new values" passes whether or not `UpdateAsync`
   removes `recipe_{id}`, because the cached object already holds the new values. The test design in section 12
   accounts for this.

## 7. API impact

- **Endpoints:** none changed.
- **`RecipeDto` / `UpdateRecipeDto` changes:** none.
- **Cache impact:** none changed. This item only *pins* the current invalidation map:

  | Write | Removes | Sets |
  | --- | --- | --- |
  | `AddAsync` | `recipes_all` | `recipe_{id}` |
  | `UpdateAsync` | `recipes_all`, `recipe_{id}` | — |
  | `DeleteAsync` | `recipes_all`, `recipe_{id}` | — |

## 8. Frontend impact

None.

## 9. Architecture impact

No architectural impact. `01-architect`: no ADR, since the item applies the existing ADR-017 test
infrastructure and changes no decision.

- **New dependency:** none.
- **Layer/dependency changes:** none.
- **New DI registrations:** none.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Unit-test `CachedRecipeRepository` with a substituted `ICacheService`, asserting `Received(1).RemoveAsync("recipes_all")` | Speed, no Docker, exact key-level precision | It pins the implementation (key names, call order), not the behaviour. A refactor to tag-based eviction breaks it while staying correct, and a wrong key constant passes it while being broken. The unit level also skips the real `Decorate` wiring and the real `MemoryCacheService`. |
| Assert against `IMemoryCache` directly from the test (`Factory.Services.GetRequiredService<IMemoryCache>()`) | Precision without mocks | Couples the tests to key strings in `CacheKeys` and reaches past the API. Users observe HTTP responses, not cache entries. |
| Write → re-read with **no priming read** (the roadmap text taken literally) | Shortest tests | A cold cache cannot be stale. Without a preceding `GET`, `recipes_all` is empty, the re-read hits the database, and the test passes with invalidation deleted. A test that cannot fail is worse than none, because it looks like coverage. |
| **Chosen:** prime with a `GET`, write through the API, re-read through the API, plus one test that proves a cache hit | Behavioural, black-box, sensitive to the real defect | See the cost below. |

- **Pattern applied:** black-box integration testing of a **decorator**, with **mutation testing** (applied
  manually) as the proof that each test is sensitive. The system under test is
  `RecipeManager.Infrastructure/Repositories/Recipes/CachedRecipeRepository.cs`.
- **What this makes harder:** the cache-hit test (test 1) deliberately asserts **stale** data. It has to write to
  the database behind the API's back, which the other integration tests never do, and it breaks the day caching
  is intentionally removed. That is correct, but it will look like a bug to a reader who skips its comment. The
  tests also need Docker, so on a machine without it they skip like the rest of the integration suite
  (ADR-017).

## 10. Security impact

- **New user-controlled input:** none.
- **User content rendered in the SPA:** no.
- **File upload:** no.
- **Auth/ownership implications:** none. Test-only change.
- **Config/secrets touched:** none.
- **Standing gaps affected:** none. `SEC-07` (the whole list cached under one key) is unchanged and now pinned.

## 11. Acceptance criteria

- [ ] Given the list was fetched, when a row changes directly in the database, then a second `GET /api/recipes`
      still returns the old value.
- [ ] Given the list was fetched, when `POST /api/recipes` succeeds, then `GET /api/recipes` contains the new
      recipe.
- [ ] Given the list and the detail were fetched, when `PUT /api/recipes/{id}` succeeds, then both
      `GET /api/recipes` and `GET /api/recipes/{id}` show the new values.
- [ ] Given the list and the detail were fetched, when `DELETE /api/recipes/{id}` succeeds, then
      `GET /api/recipes/{id}` returns 404 and `GET /api/recipes` no longer contains it.
- [ ] With any single invalidation line in `CachedRecipeRepository` removed, at least one of these tests fails.

## 12. Test plan

Every invalidation test follows **prime → write → re-read**, all through `Client`. The priming read is what
makes the test sensitive: it fills the entry that a missing invalidation would leave stale.

| # | Test | Removing this makes it fail |
| --- | --- | --- |
| 1 | `GetAllRecipes_WhenCalledTwice_ShouldServeSecondResponseFromCache`: seed, `GET` list, change the title with `DbContext.Recipes.ExecuteUpdateAsync(...)`, `GET` list → old title | the `Decorate<IRecipeRepository, CachedRecipeRepository>()` registration, or the cache read in `GetAllAsync` |
| 2 | `CreateRecipe_AfterListWasCached_ShouldIncludeNewRecipeInList` | `RemoveCache(CacheKeys.AllRecipes)` in `AddAsync` |
| 3 | `UpdateRecipe_AfterListAndDetailWereCached_ShouldReturnNewValuesFromBoth` | `recipes_all` removal in `UpdateAsync` (list assertion). The detail assertion is kept as the user-visible contract, with a comment explaining it is not sensitive to `recipe_{id}` removal because of `BUG-14`. |
| 4 | `DeleteRecipe_AfterListAndDetailWereCached_ShouldReturnNotFoundAndExcludeFromList` | either removal in `DeleteAsync`: `recipe_{id}` (detail → cached 200 instead of 404) or `recipes_all` (list still contains it) |

- **Conventions:** `IntegrationTestBase`, `[Collection(PostgresCollection.Name)]`, `[SkippableFact]`,
  ARRANGE/ACT/ASSERT banners, entities built with `Recipe.Create(...).Value`, FluentAssertions only.
- **Isolation:** xUnit builds a new class instance per test, so each test has its own factory, cache, and
  database. There is no cross-test leakage.
- **Mutation check (manual, reported in the PR):** comment out each of the five invalidation calls in turn,
  plus the `Decorate` line, run `dotnet test --filter RecipeCacheTests`, and record which test failed.
- **Not covered, and why:**
  - Per-id invalidation on **update**, because of `BUG-14`. It becomes testable once aliasing is fixed.
  - Expiry and cache-failure paths: see section 4.
- **Expected counts:** 103 backend tests (85 unit + 18 integration), 0 warnings. On a machine without Docker,
  18 skipped.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | "no architectural impact" (section 9) |
| 3 | `06-qa-tester` | `RecipeCacheTests`, mutation check, coverage statement |
| 4 | `04-code-reviewer` | review |

No production code, contract, UI, or security surface changes, so `02`, `03`, `05`, `07`, and `08` are not
involved.

## 14. Assumptions made

- The five invalidation calls and the `Decorate` registration are the complete set of cache behaviour worth
  pinning. `MemoryCacheService`'s internal `_cacheKeys` dictionary is written but never read, so it has no
  observable behaviour.
- `ExecuteUpdateAsync` can set `Title` despite its private setter. It builds SQL from an expression and never
  calls the setter.

## 15. Follow-ups

- `BUG-14`: cached-instance aliasing. Fix by not mutating cached instances (for example, loading the entity for
  update from the undecorated repository, or caching DTOs instead of entities). That fix would make per-id
  update invalidation testable, and test 3's detail assertion should then be tightened.

## 16. Known issues and roadmap items touched

- **Fixes:** `TEST-02`. Delete its entry in `known-issues.md` and the `R-08` entry in `roadmap.md`.
- **Adds:** `BUG-14` (cached-instance aliasing) to `known-issues.md`.
- **Updates:** test counts (99 → 103, integration 14 → 18) in `CLAUDE.md`, `known-issues.md` (baseline),
  `docs/agents/06-qa-tester.md` (table, breakdown, caching catalogue), and `docs/workflows/feature-workflow.md`.
  Also a `decisions-log.md` entry, "a cold cache cannot go stale".
- **Depends on:** `R-06` / ADR-017 (shipped).
- **On the [deploy gate](../roadmap.md#deploy-gate)?** No.
