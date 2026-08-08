# Spec: Fix the frontend toolchain (R-03)

| | |
| --- | --- |
| **ID** | `001` |
| **Status** | shipped |
| **Author** | `00-leader` |
| **Created** | 2026-08-08 |
| **Branch** | `chore/r-03-frontend-toolchain` |

---

## 1. Context

The frontend has no working automated quality gate. `npm run lint` **cannot start** on a clean install because
the ESLint config is `eslint.config.ts` and ESLint 9 needs `jiti` to load a TypeScript config, which is not a
dependency (`BUILD-03`) — reproduced on this branch, and true since commit `3474a8b` (2025-08-08). `npm run
build` runs `vite build`, which transpiles with esbuild and **strips types without checking them**, so a
TypeScript error cannot fail the build either (`BUILD-04`).

The backend was closed off in exactly this way by `R-02`/ADR-010: warnings became build failures. The frontend
still has nothing equivalent, and `BUG-08` (`console.log` in shipped code) is the proof that the unenforced
rules rot — every ESLint rule this project configured has been unenforced for eleven months.

## 2. Goal

Make `npm run lint` and type-checking actually run, and fix everything the newly-working lint reports, so the
frontend has a gate that can be wired into CI by `R-04`.

## 3. In scope

- [x] Add `jiti` to `devDependencies` so `eslint.config.ts` loads (`BUILD-03`).
- [x] `"build"` type-checks before bundling, and a `"typecheck"` script exists for fast local checks
      (`BUILD-04`).
- [x] Make the ESLint config self-consistent: the type-checked rule sets are configured with
      `project: ['./tsconfig.json']`, whose `include` is `["src"]` only, so the root config files are linted
      with type information they have no project for.
- [x] Run `npm run lint` and fix every error it reports. `BUG-08` (`console.log` in `SearchBar.tsx` and
      `RecipeList.tsx`) is expected to be among them and is closed here.
- [x] Update `docs/known-issues.md`, `docs/roadmap.md`, `README.md`, `CLAUDE.md`, and the agent docs that
      currently instruct agents to skip lint.

**Added during implementation**, because turning the linter on reported them:

- [x] `no-console` added to the ESLint config as an error — it had **never been configured**, so the claim in
      `BUG-08` that lint would have caught the `console.log`s was wrong. Recording the rule is what actually
      closes that defect.
- [x] `contexts/ThemeContext.tsx` split into `ThemeContext.ts` (context) + `ThemeProvider.tsx` (component), as
      `react-refresh/only-export-components` requires, and the context's default value removed so `useTheme`'s
      guard is reachable. `Footer` switched from `useContext(ThemeContext)` to `useTheme()`.
- [x] `RecipeList` no longer passes an `onClick` that does nothing, so cards render `<article>` instead of a
      `<button>` that announces an action and performs none. New `BUG-10` tracks the missing detail route.
- [x] `*.tsbuildinfo` gitignored — `tsc -b` writes incremental build state.

## 4. Out of scope

- **The Vitest / React Testing Library setup** — that is `R-07`, blocked by this item, and adding a test runner
  is a second dependency decision with its own ADR.
- **The CI workflow that runs these commands** — `R-04`. This item only makes the commands runnable; nothing
  enforces them until CI exists.
- **`npm audit fix` / the 68 Dependabot alerts** (`SEC-03`) — a separate PR by instruction of that entry, since
  `react-router` and `vite` upgrades break routing silently and must be exercised manually.
- **Node version pinning** (`BUILD-07`) and the 2.1 MB `mainPhoto.png` (`BUILD-05`) — adjacent tooling defects,
  deliberately left open so this diff stays reviewable.
- **Any behavioural change to the app.** Lint fixes that would alter runtime behaviour beyond removing debug
  logging are to be reported, not applied.

## 5. Open questions

Resolved without blocking; each is recorded as an assumption in section 14.

| # | Question | Recommended default | Answer |
| --- | --- | --- | --- |
| 1 | Add `jiti`, or rename the config to `eslint.config.js`? | Add `jiti` — keeps the type-checked config | Add `jiti` (ADR-012) |
| 2 | If lint reports errors that need real code changes, fix here or split? | Fix mechanical ones here; report anything behavioural | Fixed here. Two changes are behaviour-visible and are called out in section 14: the theme context now throws outside its provider, and recipe cards render `<article>` |
| 3 | Should lint failures become blocking now? | Yes — the scripts exist and the pre-merge checklist gains a step; true enforcement waits for `R-04` | Yes |

## 6. Domain impact

- **New/changed entities:** none
- **New/changed properties on `Recipe`:** none
- **New/changed invariants:** none
- **New/changed shape validation:** none
- **Migration required:** no
- **Known limitations touched:** none

No backend file is touched by this spec.

## 7. API impact

None. No endpoint, DTO, route, or cache key changes. `RecipeDto` is untouched, so `08-api-contract` is not
involved — the contract defects `BUG-01`–`BUG-05` remain open and are **not** fixed here.

## 8. Frontend impact

- **New routes:** none
- **New/changed components:** none created. `SearchBar.tsx` and `RecipeList.tsx` lose their `console.log`
  calls; any further edits are whatever lint requires and are listed in the PR.
- **New/changed hooks:** none
- **States to design:** none — no UI change. Lint and type-check output are the only observable difference.
- **Design tokens needed:** existing only

## 9. Architecture impact

- **ADR required:** yes → ADR-012 in [../architecture.md](../architecture.md) (new npm devDependency, and the
  build script becomes a gate)
- **New dependency:** `jiti` (devDependency) — the loader ESLint 9 uses to evaluate a TypeScript flat config.
  It replaces nothing; it is the missing half of a choice already made in 2025.
- **Layer/dependency changes:** none. Frontend tooling only; no backend project reference changes.
- **New DI registrations:** none

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Rename `eslint.config.ts` → `.js`/`.mjs` | Zero new dependencies; ESLint loads plain JS natively | Loses type checking *of the config itself* and the typed `tseslint.config()` helper. The config already uses `tseslint.config(...)`, so the project has chosen TS config; reversing that to avoid a dev-only loader trades a real capability for a dependency that never ships to the browser. |
| Keep `ts-node` and hope it loads the config | No change at all | Factually wrong: ESLint 9 hard-codes `jiti`, not `ts-node`, and the error message says so. `ts-node` sitting in `devDependencies` is very likely a previous attempt at this fix that did not work — evidence that the missing piece was guessed at rather than read. |
| `"build": "tsc --noEmit && vite build"` | One fewer TS build mode to understand; no `.tsbuildinfo` on disk | `tsc -b` is the Vite React-TS template default and is incremental, so repeat builds are cheaper. Both are correct here because there is a single non-composite `tsconfig.json`; the choice is verified by running it, not assumed. |
| Fix lint errors in a follow-up PR | A smaller, purely-additive diff | The point of the item is a *working* gate. A gate merged red is not a gate, and `BUILD-03` is this repo's own evidence that a check nobody has seen pass gets ignored. |

- **Pattern applied:** *make the invalid state unrepresentable at build time* — the same move as ADR-010's
  `TreatWarningsAsErrors`, applied to the half of the codebase that ADR-010 could not reach.
- **What this makes harder:** every future frontend PR must satisfy the type-checked ESLint rule sets, which are
  strict (`recommendedTypeChecked` + `stylisticTypeChecked`). A quick experimental commit now costs more. That
  friction is the mechanism, not a side effect — identical to the cost recorded in ADR-010.

## 10. Security impact

- **New user-controlled input:** none
- **User content rendered in the SPA:** unchanged
- **File upload:** no
- **Auth/ownership implications:** none — this item neither improves nor worsens `SEC-01`/`SEC-02`
- **Config/secrets touched:** none. No `.env` file, no connection string, no `VITE_API_URL` change.
- **Standing gaps affected:** adds one npm devDependency, so `SEC-03`'s surface grows by one package —
  build-scope only, never shipped to the browser. `05-security-reviewer` reviews on that basis alone; the
  trigger list in [../agents/05-security-reviewer.md](../agents/05-security-reviewer.md) is otherwise not fired.

**Review outcome.** `jiti@2.7.0`: MIT, **zero** dependencies, no install scripts — it adds one dev-scope package
and no transitive graph. `npm audit` moved from 17 to 18 affected packages, which is **not** `jiti`: `npm
install` re-resolved a hoisted optional-peer `yaml@2.8.0` into `cosmiconfig`'s nested copy, which carries a
dev-scope DoS advisory. Recorded in `SEC-03` rather than fixed here, per that entry's own instruction that
`npm audit fix` gets its own PR. No secret, config, CORS, or connection-string change is in this diff.

## 11. Acceptance criteria

- [ ] Given a clean `node_modules`, when `npm ci && npm run lint` runs, then ESLint **starts** and exits 0.
- [ ] Given a deliberately introduced type error, when `npm run build` runs, then it **fails** before Vite
      bundles anything. (The negative test — a gate never seen rejecting anything is indistinguishable from no
      gate. See the 2026-08-04 decisions-log entry.)
- [ ] Given a deliberately introduced `console.log`, when `npm run lint` runs, then it is reported.
- [ ] `npm run typecheck` exists and exits 0.
- [ ] No `console.log` remains in `recipe-manager-frontend/src/`.
- [ ] `dotnet build` still reports 0 warnings and `dotnet test` still reports 84 passing — this PR must not
      touch the backend, and the numbers prove it.

## 12. Test plan

- **Domain unit tests:** none — no domain change.
- **Handler unit tests:** none — no handler change.
- **Integration tests:** none. Backend suite is run only as a regression guard.
- **Not covered, and why:** there is still **no frontend test runner** (`TEST-01`), so nothing asserts app
  behaviour after the lint fixes. That is `R-07`, which this item unblocks. Removing a `console.log` cannot
  change behaviour, but a lint autofix elsewhere could, which is why autofix output is reviewed by hand rather
  than trusted.
- **Manual verification:** `npm run dev`, load `/` and `/recipes` in both light and dark themes, exercise the
  search box and click a card — the two files losing `console.log` are exactly those paths.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-012 — the `jiti` dependency and the build-as-gate decision |
| 3 | `03-senior-react` | `package.json` scripts + devDependency, ESLint config fix, lint fixes |
| 4 | `06-qa-tester` | negative tests for both new gates; coverage statement |
| 5 | `04-code-reviewer` | review |
| 6 | `05-security-reviewer` | dependency-surface review only |

Rows deleted: `02-senior-csharp` (no backend file is in scope), `08-api-contract` (no contract change),
`07-ux-ui` (no visual change) — see section 4.

## 14. Assumptions made

- **Wrong, and corrected in flight:** the assumption was that lint findings would be mechanical. Two were not,
  and both were applied rather than deferred, because leaving them meant leaving the linter red:
  - `useTheme`'s missing-provider guard was **dead code** — the context had a default value, so `useContext`
    never returned `undefined`. Removing the default makes the guard fire. A component rendered outside
    `ThemeProvider` now throws instead of silently receiving the light theme. `Footer` was the only direct
    `useContext` consumer and now goes through the hook.
  - `RecipeCard` no longer receives an `onClick` from `RecipeList`, so cards render `<article>` rather than a
    `<button>` announced as "View {title} recipe" that did nothing. Tracked as `BUG-10` so the detail screen
    re-adds it.
- Both were verified in the browser against a running API in light and dark themes.
- Lint becomes a **documented** pre-merge step now and a **enforced** one when `R-04` lands. Nothing in this PR
  can force anyone to run it.
- `ts-node` is left in `devDependencies` in this PR. Removing it is plausible cleanup but is not needed for the
  gate to work, and unused-dependency removal is its own change — recorded as a follow-up.

## 15. Follow-ups

- Remove the apparently-unused `ts-node` devDependency (new `BUILD-08`).
- Add `npm run lint` and `npm run typecheck` to the CI workflow — `R-04`.
- Install Vitest and write the first frontend tests — `R-07`, unblocked by this item.
- `SEC-03`, `BUILD-05`, `BUILD-07` remain open by design.

## 16. Known issues and roadmap items touched

- Fixes: `BUILD-03`, `BUILD-04`, `BUG-08`, roadmap `R-03`
- Adds: `BUILD-08` (unused `ts-node`), `BUG-10` (no recipe detail route, cards not clickable)
- Unblocks: `R-07` (frontend tests), and gives `R-04` the npm scripts it calls
- Depends on: nothing. This is the Phase 1 item everything else on the frontend roadmap (`R-07`, `R-09`) waits
  behind, and `R-04` consumes.
- On the [deploy gate](../roadmap.md#deploy-gate)? no — but `R-04`, which is on it, cannot be written until
  these commands run.
