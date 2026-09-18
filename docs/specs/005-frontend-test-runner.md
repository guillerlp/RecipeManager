# Spec: Frontend test runner and first tests (R-07)

| | |
| --- | --- |
| **ID** | `005` |
| **Status** | in progress — implemented, awaiting a green CI run |
| **Author** | `00-leader` |
| **Created** | 2026-09-18 |
| **Branch** | `feat/frontend-test-runner` |

---

## 1. Context

*(Written before implementation; the present tense describes the state this item started from.)*

The SPA has zero tests and no runner (`TEST-01`, rated High). Every frontend change is verified by clicking
through the app, and CI's frontend job proves only that the code type-checks, lints, and bundles — never that it
behaves. Dependabot's npm updates are merged on that same evidence (`.github/dependabot.yml` says so in a
comment).

The logic most worth protecting is small but real: duration formatting with boundary behaviour, client-side
search filtering, four render states of the recipe list, theme persistence, and nav active-state matching. None
of it has ever been asserted.

`R-03` (ADR-012) and `R-04` (ADR-013) shipped the working toolchain and the CI job this item attaches to.

## 2. Goal

`npm test` runs a Vitest + React Testing Library suite under jsdom, locally and in CI, covering the five
priority areas listed in `06-qa-tester.md`.

## 3. In scope

- [x] Dev dependencies, exact versions: `vitest@5.0.1`, `jsdom@30.1.0`, `@testing-library/react@16.3.3`, and its
      required peer `@testing-library/dom@10.4.2`. `package-lock.json` regenerated.
- [x] A `test` block in `vite.config.ts` (`environment: 'jsdom'`, `setupFiles: ['./src/test/setup.ts']`), with
      `defineConfig` imported from `vitest/config` so the key type-checks.
- [x] `src/test/setup.ts` registering RTL's `cleanup()` in `afterEach`.
- [x] Scripts: `"test": "vitest run"` (single run, exits — what CI calls) and `"test:watch": "vitest"`.
- [x] `formatDuration` and `getISODuration` extracted from `RecipeCard` into `src/utils/duration.ts`, sharing one
      private `toSafeMinutes` guard. `RecipeCard` imports them. Behaviour-preserving.
- [x] Colocated tests: `src/utils/duration.test.ts`, `RecipeList.test.tsx`, `ThemeProvider.test.tsx`,
      `NavLink.test.tsx` — see section 12.
- [x] `.github/workflows/ci.yml`: a `Test` step running `npm test`, after `Lint` and before `Build`.
- [x] Docs: ADR-018, frontend Tests section in `conventions.md`, and the updates listed in section 16.

## 4. Out of scope

- **Coverage tooling** (`@vitest/coverage-v8`). No threshold has been agreed (`TEST-05`); a number nobody acts
  on is not worth a dependency yet.
- **`@testing-library/jest-dom`.** `getBy*` queries already throw when nothing matches, and attributes are
  asserted with `getAttribute`. Add it if assertion messages become the bottleneck.
- **`@testing-library/user-event`.** None of the five areas needs simulated typing; the search query reaches
  `RecipeList` as a prop, and theme toggling is exercised through the context function.
- **MSW / network-level mocking.** Rejected in section 9.
- **Fixing defects the tests expose.** Recorded in `known-issues.md` instead (global rule 9). Tests do not pin a
  known-wrong behaviour; they assert the intended behaviour only where the code already has it.
- **Fixing the TS/C# contract drift** (`BUG-01`–`BUG-05`). Test fixtures use the current TS `Recipe` type,
  `id: number` included. That is `R-09`'s territory.
- **Tests for `SearchBar`, `Header`, pages, `useRecipesLoading`.** Not on the priority list; follow-ups.

## 5. Open questions

All answered by the user on 2026-09-18:

| # | Question | Answer |
| --- | --- | --- |
| 1 | Test duration helpers through rendering, or extract them? | Extract to `src/utils/duration.ts` |
| 2 | Which seam replaces `RecipeList`'s data source? | Mock `@/services`; real TanStack Query |
| 3 | Where does the Vitest config live? | `test` block in `vite.config.ts` |
| 4 | Where do test files live? | Colocated `Foo.test.tsx` |
| 5 | Scope of the PR? | All five areas; no coverage tooling |

## 6. Domain impact

- **New/changed entities:** none
- **New/changed properties on `Recipe`:** none
- **New/changed invariants:** none
- **New/changed shape validation:** none
- **Migration required:** no
- **Known limitations touched:** none

## 7. API impact

None. No endpoint, DTO, or cache change.

## 8. Frontend impact

- **New routes:** none
- **New/changed components:** `RecipeCard` — imports the two duration helpers instead of declaring them; rendered
  output unchanged.
- **New module:** `src/utils/duration.ts` — the first `utils/` folder. No barrel `index.ts`: one module, one
  importer; add one when a second utility arrives.
- **New/changed hooks:** none
- **States to design:** none — no UI change.
- **Design tokens needed:** none

## 9. Architecture impact

- **ADR required:** yes → ADR-018 in [../architecture.md](../architecture.md) — a new tooling dependency set
  and a new CI gate.
- **New dependency:** `vitest`, `jsdom`, `@testing-library/react`, `@testing-library/dom` — dev only; nothing
  enters the production bundle.
- **Layer/dependency changes:** none.
- **New DI registrations:** none.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| **Jest** instead of Vitest | The largest ecosystem and the most online answers | Needs its own transform (Babel/`ts-jest`) and a second copy of the eight path aliases in `moduleNameMapper`. Vitest runs through Vite's own pipeline, so aliases, the React plugin, and CSS Modules just work. |
| **happy-dom** instead of jsdom | Faster start-up and test runs | Less spec-complete; the roadmap and `06-qa-tester.md` already decided on jsdom. At five test files the speed difference is not measurable. |
| **Separate `vitest.config.ts`** | Clean split between build and test config | A second root tooling file that must also be added to `tsconfig.node.json`, and a `mergeConfig` indirection. One file cannot drift from itself. |
| **Mock `useRecipes` directly** | Simplest tests — each state is one line | Tests the component against a hand-written `{ data, isLoading, error }` shape. A change in what TanStack Query returns, or in the hook's options, would pass unnoticed. |
| **MSW** at the network level | Realism; reusable for future mutation tests | A further dependency and the most setup, to test four states of one read-only list. Reconsider when mutation hooks exist. |
| **Test durations through rendering `RecipeCard`** | Zero production change | Eight-plus boundary cases each render a component, and a failure cannot say whether formatting or rendering broke. |
| **Vitest `globals: true`** | Jest-style bare `describe`/`it` | Needs ambient `vitest/globals` types in `tsconfig.json` for all of `src/`, and hides where test APIs come from. Explicit imports are ordinary TypeScript that the type-aware Oxlint rules already understand. |
| **`src/__tests__/` tree** | Separation of test and production files | Mirrors drift as files move, and an untested component is invisible from its own folder. Colocation matches the existing `Foo.tsx` + `Foo.module.css` + `index.ts` folder convention. |

- **Pattern applied:** extract-function refactor guarded by characterisation tests (tests written against the
  current behaviour *before* moving the code); test double at the service boundary (a stub, via `vi.mock`).
  Existing example of the stub-at-a-port idea: the backend handler tests substitute `IRecipeRepository` with
  NSubstitute — `RecipeManager.UnitTests/Application/Handlers/`.
- **What this makes harder:**
  - Colocated tests are inside `tsconfig.json`'s `include`, so `npm run build` type-checks them: a type error in a
    test fails the production build. Deliberate — a test that does not compile is not a test — but it is coupling.
  - `npm ci` now installs jsdom and its tree; the CI frontend job gets slower by the install plus the run.
  - The error-state test depends on `useRecipes` hard-coding `retry: 2`. A per-query option overrides any
    `QueryClient` default, so the test must advance fake timers through the backoff. If the retry policy changes,
    that test changes with it.

## 10. Security impact

- **New user-controlled input:** none
- **User content rendered in the SPA:** no change
- **File upload:** no
- **Auth/ownership implications:** none
- **Config/secrets touched:** none
- **Standing gaps affected:** none. Four new dev dependencies enlarge the supply-chain surface; covered by the
  existing `npm audit --audit-level=high` gate and Dependabot's npm ecosystem. Nothing ships to the browser.

## 11. Acceptance criteria

- [ ] Given a clean checkout, when `npm ci && npm test` runs, then Vitest runs every `*.test.ts(x)` under jsdom
      and exits 0.
- [ ] Given a deliberately failing assertion, when `npm test` runs, then it exits non-zero (negative test — the
      gate can fail).
- [ ] Given a deliberately failing test, when CI runs on the PR, then the frontend job fails at `Test` and `Build`
      does not run.
- [ ] `npm run typecheck`, `npm run lint`, and `npm run build` still pass with the test files present — and lint
      covers them with the type-aware `src/**` rules.
- [ ] `RecipeCard` renders the same `<time>` text and `dateTime` values before and after the extraction.
- [ ] Every case listed in section 12 exists and passes.

## 12. Test plan

- **`src/utils/duration.test.ts`** — `it.each` tables for both functions:
  `0 → "0 min"/"PT0M"`, `59 → "59 min"/"PT59M"`, `60 → "1h"/"PT1H"`, `61 → "1h 1min"/"PT1H1M"`,
  `120 → "2h"/"PT2H"`, `NaN`, `Infinity`, and a negative → all treated as 0. Order: the test file is written
  first, importing from `@/utils/duration`, and fails because the module does not exist; the functions are then
  moved verbatim and it goes green. The expected values are read off the current implementation, so a green run
  is the proof the move preserved behaviour.
- **`RecipeList.test.tsx`** — `vi.mock('@/services')`; a fresh `QueryClient` per test (no cache shared between
  tests).
  - Filtering: matches on title, on description, on an ingredient; case-insensitive; surrounding whitespace in
    the query is trimmed; empty query returns every recipe.
  - States: loading (service promise pending); error (service rejects — `vi.useFakeTimers()` to advance through
    the hook's `retry: 2` backoff); empty with a query ("No recipes found"); empty without one ("No recipes
    available"); populated (one card per recipe).
- **`ThemeProvider.test.tsx`** — `localStorage` cleared and `data-theme` removed before each test.
  - Initial theme read from `localStorage` (`'dark'` → dark; missing or unrecognised → light).
  - `data-theme` on `<html>` matches the theme after mount and after `toggleTheme`.
  - `toggleTheme` persists the new value to `localStorage`.
  - `useTheme` outside a provider throws `"useTheme must be used within a ThemeProvider"`.
- **`NavLink.test.tsx`** — rendered inside `MemoryRouter` with `initialEntries`; `aria-current="page"` is the
  assertion (it is what assistive technology reads, and it is set by the same `isActive` as the CSS class).
  - `/recipes` on `/recipes` → active; on `/recipes/` → active (trailing-slash normalisation); on `/recipes/123`
    → active (prefix); on `/recipes-archive` → **not** active (prefix must end at a segment boundary).
  - `/` on `/` → active; `/` on `/recipes` → not active.
- **Not covered, and why:**
  - Real HTTP, axios configuration, `VITE_API_URL` handling — the service is mocked by design.
  - Visual output and CSS — jsdom has no layout engine; CSS Modules resolve to class names only.
  - Real browser behaviour (focus, `loading="lazy"`) — no browser runner; out of scope.
- **Manual verification:** the two negative tests in section 11; `npm run dev` and a visual check that recipe
  cards show the same prep/cook times as before the extraction.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-018 |
| 3 | `03-senior-react` | toolchain config, duration extraction |
| 4 | `06-qa-tester` | the four test files + coverage statement |
| 5 | `04-code-reviewer` | review |

`02-senior-csharp`, `08-api-contract`, `07-ux-ui`, and `05-security-reviewer` are not involved: no backend,
contract, UI, or user-input change (section 4).

## 14. Assumptions made

- Tests assert on visible text and ARIA attributes, never CSS Module class names — class names are an
  implementation detail and would couple tests to styling.
- `vitest run` is the right CI command: `vitest` alone enters watch mode when it detects a TTY.
- The Oxlint `src/**` override applies unchanged to test files; if a rule proves wrong for tests (e.g.
  `unbound-method` on `vi.fn` references), the exception is scoped to `*.test.ts(x)` and recorded in the ADR,
  never disabled globally.

## 15. Follow-ups

- `BUG-13` (new): a whitespace-only search query is treated as "no filter" by `filteredRecipes`, but the heading
  still reads `Found N recipes matching "   "`. The render path uses the raw `searchQuery` for truthiness while the
  filter trims it.
- Tests for `SearchBar`, `Header` (theme switch), and the pages.
- Coverage reporting once `TEST-05` agrees a threshold.

## 16. Known issues and roadmap items touched

- Fixes: `TEST-01` (delete entry), `R-07` (delete entry).
- Adds: `BUG-13`.
- Updates: `docs/tech-stack.md`, `docs/conventions.md` (frontend Tests section), `docs/agents/06-qa-tester.md`
  (frontend row and "not set up" section), `docs/agents/03-senior-react.md` if it describes the missing runner,
  `CLAUDE.md` (stack table, test counts, "no frontend test runner" rule 3), `README.md` if it lists npm scripts,
  the `.github/dependabot.yml` comment citing `TEST-01`, `docs/decisions-log.md` (the extraction decision).
- Depends on: `R-03` (ADR-012), `R-04` (ADR-013) — both shipped.
- On the [deploy gate](../roadmap.md#deploy-gate)? no
