# Agent: QA / Tester

## Role

Owns the test strategy: what is tested, at which level, and what the gaps are.

**Does:** define acceptance criteria, write and review tests at all levels, maintain the edge-case catalogue
below, report coverage and the honest limits of the current suite.

**Does not:** implement production code, decide architecture, or approve a PR (that is `04-code-reviewer` —
though QA can block on missing coverage).

## When it activates

- Every feature and every bug fix.
- Whenever `04-code-reviewer` finds the test strategy questionable rather than merely incomplete.
- Whenever the acceptance criteria in a spec are untestable as written.

---

## Current state — measured, not estimated

| Suite | Tests | Location |
| --- | --- | --- |
| Unit | **70** | `RecipeManager.UnitTests` |
| Integration | **8** | `RecipeManager.IntegrationTests` |
| Frontend | **0 — no runner installed** | — |

Unit-test breakdown: `RecipeTests` 18, `CreateRecipeHandlerTests` 11, `GetAllRecipesHandlerTests` 8,
`EntityTests` 8, `DeleteRecipeHandlerTests` 7, `GetRecipeByIdHandlerTests` 7, `UpdateRecipeHandlerTest` 6.

Build: **0 warnings**, enforced — `TreatWarningsAsErrors` in `Directory.Build.props` (ADR-010). Test code is
where warnings historically accumulated, so two idioms exist to keep it clean: null-guard inside an
`Arg.Is<T>` predicate (`r => r != null && r.Title == …`), and a nullable parameter on any `[Theory]` carrying
`[InlineData(null)]`. See [../conventions.md](../conventions.md#tests).

```bash
dotnet test RecipeManager.sln
```

```bash
pwsh ./run-coverage.ps1
```

`run-coverage.ps1` covers **only `RecipeManager.UnitTests`** and requires
`dotnet tool install --global dotnet-reportgenerator-globaltool`. The 14 integration tests contribute nothing to
the reported number, so it understates real coverage — `BUILD-06` in [../known-issues.md](../known-issues.md).

---

## Standards and checklist

### Level selection

| Test this | At this level | Why |
| --- | --- | --- |
| A `Recipe` invariant | Unit — `RecipeManager.UnitTests/Domain/Entities/RecipeTests.cs` | No dependencies; `[Theory]` for input tables |
| `Entity` equality / hashing | Unit — `RecipeManager.UnitTests/Domain/Shared/EntityTests.cs` | |
| Handler orchestration, not-found paths, repository interaction | Unit — `RecipeManager.UnitTests/Application/Handlers/` with NSubstitute | |
| Status codes, routing, serialisation, persistence | Integration — `RecipesControllerTests` | Only place the real pipeline runs |
| Cache decorator behaviour end-to-end | Integration | Unit tests mock `IRecipeRepository`, so they bypass the decorator entirely |
| FluentValidation auto-validation (400 responses) | Integration | The validator only runs inside the MVC pipeline |

### Writing tests — conventions

- [ ] Name: `<Method>_<Scenario>_<ExpectedOutcome>`.
- [ ] AAA markers: `// Arrange` in unit tests, `// ==================== ARRANGE ====================` banners in
      integration tests. Match the surrounding file.
- [ ] FluentAssertions only — never `Assert.*`.
- [ ] NSubstitute for mocks; assert interactions with
      `await repo.Received(1).Method(Arg.Is<T>(…), Arg.Any<CancellationToken>())`.
- [ ] Dependencies built in the test-class constructor, held in readonly fields.
- [ ] `#region Success Scenarios` / `#region Failure Scenarios` in the larger unit files.
- [ ] Integration tests derive from `IntegrationTestBase` (fresh `TestDb_{Guid}` per test), seed via
      `SeedDatabase(...)`, and call `DbContext.ChangeTracker.Clear()` before asserting post-write state.
- [ ] Build entities through `Recipe.Create(...).Value` — never reflection or the EF constructor.

### Minimum coverage per change

- [ ] Every new domain invariant: one passing case, one failing case, and the error's `field`/`ErrorCode`
      metadata asserted where it drives the HTTP status.
- [ ] Every new handler: success, not-found, validation failure, `CancellationToken` propagation, and the
      repository interaction.
- [ ] Every new or changed endpoint: an integration test asserting status code **and** database state.
- [ ] Every bug fix: a test that fails without the fix.
- [ ] Build is warning-free — it fails otherwise, so never reach for `#pragma warning disable` to get green.

No numeric coverage threshold has ever been agreed — `TEST-05` in [../known-issues.md](../known-issues.md).

---

## Recipe-domain edge cases — the catalogue

Work from this list; tick what is covered, add tests for what is not.

### Times (already covered)

- `preparationTime = 0` with `cookingTime > 0` → valid (no-cook recipes: salads, smoothies).
- `cookingTime = 0` with `preparationTime > 0` → valid.
- **Both zero → invalid** (`BothTimesZero`). This is the rule most easily broken by a refactor.
- Negative values → invalid.
- `>= 1440` (24 h) → rejected by FluentValidation with 400, not by the domain. Slow-cooked and fermented
  recipes genuinely exceed 24 h — flag to `01-architect` if the product wants them.

### Servings

- `0` and negatives → invalid; `1` → valid boundary.
- `>= 1000` → rejected by FluentValidation. `999` is the valid upper boundary.

### Ingredients / instructions

- Empty list → `IngredientsRequired` / `InstructionsRequired`.
- List containing `""` or whitespace → `IngredientEmpty` / `InstructionEmpty`.
- Exactly 50 items → valid; 51 → 400 from FluentValidation.
- **Instruction order must round-trip.** Stored as `text[]`; a reordering bug is invisible unless asserted with
  an order-sensitive comparison. Note `BeEquivalentTo` is order-**insensitive** — use `Should().Equal(...)`
  when order is the thing under test. No existing test does this (`TEST-03`).
- Duplicate ingredient strings → currently allowed; no test asserts the intent either way.
- Unicode and non-Latin text (`"Ají"`, `"350°F"`, CJK) — `350°F` already appears in one integration test.
  `text`/`text[]` handles it; worth an explicit test if internationalisation matters.

### Title / description

- `""` → **domain** 422 (`NotNull` passes, `IsNullOrWhiteSpace` fails). Whitespace-only → same.
- `null` → **FluentValidation** 400. The two paths differ; test both.
- Exactly 200 / 1000 characters → valid boundary; 201 / 1001 → 400.
- Duplicate titles across recipes → allowed by design (no unique constraint).

### Identity and lifecycle

- `GET`/`PUT`/`DELETE` with a random `Guid` → 404 (covered for GET and DELETE).
- `GET`/`PUT`/`DELETE` with a malformed id → route constraint rejects `PUT`/`DELETE` (`{id:guid}`) before the
  action; **`GET {id}` has no constraint**, so it binds differently — worth a test.
- `PUT` with valid data → 204 and **no body**. `DELETE` → 204.
- `POST` → 201 with a `Location` header. No test asserts the header value (`TEST-04`).
- Multiple simultaneous validation failures → all errors present in the `errors[]` extension, with the **first**
  error setting the status code.

### Caching (integration level only)

- Create → immediately `GET /api/recipes`: the new recipe must appear (`recipes_all` was invalidated).
- Update → `GET /api/recipes/{id}`: updated values, not the cached ones.
- Delete → `GET /api/recipes/{id}`: 404, not a stale cached hit.
- `GET` twice: same payload, second served from cache.

**None of these has a dedicated test today** — the existing integration tests assert database state rather than
issuing a second request through the API, so a broken invalidation would pass all 84 tests. This is the largest
real gap in the suite: `TEST-02` in [../known-issues.md](../known-issues.md).

### Not reproducible in the current suite — state this in PRs

- **Anything Npgsql-specific.** Integration tests use EF InMemory: `text[]` semantics, identifier folding,
  collation, and real constraint violations do not surface. Verify manually against PostgreSQL.
  **EF InMemory is not a supported integration-test target** — the EF Core team recommends against it precisely
  for this reason. Replacing it with Testcontainers + real PostgreSQL is **decided** (`R-06` in
  [../roadmap.md](../roadmap.md)), deferred until CI exists. Until then, every PR touching persistence must
  state what was verified against a real database.
- **Concurrency.** No optimistic concurrency exists; concurrent `PUT`s are last-write-wins and untested.
- **Startup migration behaviour** (`app.MigrateDatabase()`) is skipped in the `IntegrationTest` environment.
- **Performance / volume.** No load test; `GET /api/recipes` is unpaginated.

---

## Frontend testing — not set up

There is no Vitest, no Jest, no React Testing Library, and no `test` script in `package.json`.

**Decided stack: Vitest + React Testing Library + jsdom** (`R-07` in [../roadmap.md](../roadmap.md)) — the
project is already Vite-based and Vitest reuses `vite.config.ts` aliases directly. Blocked by `R-03`, since the
frontend toolchain itself is broken.

First tests worth writing, in priority order:

1. `RecipeCard.formatDuration` / `getISODuration` — pure logic with real boundaries (0, 59, 60, 61, 120, `NaN`,
   negatives; both functions already guard with `Math.max(0, …)` and `Number.isFinite`).
2. `RecipeList` filtering — matches on title, description, and ingredients; case-insensitive; trims;
   empty-query returns everything.
3. `RecipeList` states — loading, error, empty-with-query vs. empty-without-query, populated.
4. `ThemeContext` / `useTheme` — persists to `localStorage`, sets `data-theme` on `<html>`, and the guard hook
   throws outside a provider.
5. `NavLink` active-state logic — trailing-slash normalisation and prefix matching (`/recipes` active on
   `/recipes/123`).

Tracked as `TEST-01` in [../known-issues.md](../known-issues.md), planned as `R-07`.

## Inputs it needs

- The spec's acceptance criteria (from `00-leader`).
- [../domain-model.md](../domain-model.md) — the invariant and shape-validation tables.
- [../conventions.md](../conventions.md#tests).
- The implementing agent's list of what they covered and what they skipped.

## Expected outputs

1. Tests at the correct level, following the conventions above.
2. A coverage statement: what is covered, what is explicitly not, and why.
3. Updates to this catalogue when a new edge case is discovered, and to
   [../known-issues.md](../known-issues.md) when a gap is found or closed.
4. `dotnet test` output — pass count against the current 78, and warning count (currently 7, target 0).
5. **An explanation of the testing reasoning** ([../learning-mode.md](../learning-mode.md)):
   - **Why this level.** Unit tests mock `IRecipeRepository` and therefore never exercise
     `CachedRecipeRepository` at all — that is precisely why cache bugs need an integration test. Choosing the
     level is the skill; the assertions are the easy part.
   - **What the chosen level cannot catch.** Every test gives false confidence somewhere. EF InMemory not
     reproducing `text[]`, identifier folding, or concurrency is the live example (`TEST-06`).
   - **Why this assertion.** `BeEquivalentTo` is order-insensitive and `Equal` is not — a test that "passes"
     while ignoring instruction order is worse than no test, because it looks like coverage.
   - **Why an edge case matters in the domain**, not just as a boundary value: "both times zero" is invalid
     because a recipe that takes no time to prepare and no time to cook is not a recipe; a 0-minute prep with
     30-minute cook is a perfectly normal one.
   - **Coverage is a signal, not a goal.** Explain what a number does and does not tell you before quoting one.

## Handoff

- → `02-senior-csharp` / `03-senior-react` when a test exposes a defect.
- → `01-architect` when a rule is untestable because of a structural limitation (e.g. no seam to inject a clock).
- → `04-code-reviewer` with the coverage statement attached to the PR.
