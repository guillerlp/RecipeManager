# Spec: CI pipeline (R-04)

| | |
| --- | --- |
| **ID** | `002` |
| **Status** | shipped |
| **Author** | `00-leader` |
| **Created** | 2026-08-08 |
| **Branch** | `chore/r-04-ci-pipeline` |

---

## 1. Context

*(Written before implementation; the present tense describes the state this item started from.)*

Nothing enforces any checklist in this repo. There is no `.github/` directory, no workflow, and every step in
[../workflows/release-workflow.md](../workflows/release-workflow.md) is manual and therefore skippable
(`INFRA-01`). `R-03` is the proof rather than the theory: `npm run lint` could not *start* for eleven months and
nothing reported it, because a check nobody runs and a check that passes look identical from outside.

Two gates now exist locally and are enforced by nothing — `TreatWarningsAsErrors` (ADR-010) and the frontend
type-check/lint scripts (ADR-012). Both were built so that this item would have something to call.

`INFRA-06` compounds it: on a Windows machine with Smart App Control enabled, all 14 integration tests fail to
load a freshly built `RecipeManager.Api.dll`, so they are unreliable locally exactly when a backend change most
needs verifying. A Linux runner has no such policy.

## 2. Goal

Every pull request automatically runs the full build, test, and vulnerability checklist, and cannot be merged
green while any of it fails.

## 3. In scope

- [x] `.github/workflows/ci.yml` — runs on every PR targeting `main` and on pushes to `main`.
- [x] Backend job: `dotnet restore --locked-mode`, `dotnet build`, `dotnet test`, vulnerable-package check.
- [x] Frontend job: `npm ci`, `npm run typecheck`, `npm run lint`, `npm run build`, `npm audit`.
- [x] NuGet lock files: `RestorePackagesWithLockFile` in `Directory.Build.props`, six committed
      `packages.lock.json`, `--locked-mode` in CI.
- [x] `.github/dependabot.yml` — `npm`, `nuget`, and `github-actions` ecosystems, grouped, weekly.
- [x] Node version pinned in `.nvmrc` + `engines`, read by the workflow (`BUILD-07`).
- [x] Negative tests proving each gate actually rejects — see section 12.
- [x] Docs: `known-issues.md`, `roadmap.md`, `release-workflow.md`, `README.md`, `CLAUDE.md`, ADR-013,
      `decisions-log.md`.

**Changed during implementation**, because building it surfaced things the plan did not know:

- [x] **CI builds `Debug`, not `Release`** — the plan assumed Release without checking. ADR-005 makes the
      `IntegrationTest` environment throw in RELEASE builds, so Release fails all 14 integration tests by
      design (measured: 84 → 70). Recorded in ADR-013 and the decisions log; the workflow carries a comment so
      nobody "fixes" it back.
- [x] **The vulnerable-package step parses output instead of trusting the exit code**, after confirming
      `dotnet list package --vulnerable` exits 0 regardless of findings.
- [x] **New `INFRA-07`** — the workflow runs but is not yet *required* to merge, which is a repository setting.
      Section 15 predicted this; it is now a tracked entry rather than only a follow-up line.

## 4. Out of scope

- **Deployment of any kind.** No publish, no container push, no environment. `INFRA-04` (no frontend host) and
  `INFRA-02`/`INFRA-03` (versioning, rollback) stay open; CI that deploys nothing is still the whole value here.
- **Testcontainers** (`R-06`). This item is its prerequisite, not its delivery. Integration tests continue to run
  on EF InMemory, so `TEST-06` is untouched.
- **Frontend tests** (`R-07`, `TEST-01`). There is no `test` script to call. The workflow must not pretend
  otherwise — no placeholder step that passes vacuously.
- **Coverage reporting and thresholds** (`TEST-05`, `BUILD-06`). No number has ever been agreed, and a threshold
  invented here would be arbitrary.
- **Branch protection rules.** These are GitHub repository settings, not files in the repo. The workflow makes
  the checks *exist*; only the repo owner can make them *required*. Called out in section 15.
- **`npm audit fix` / `SEC-03`.** Deliberately done first, in its own PR, so this item's audit step can be
  blocking. Already shipped.

## 5. Open questions

All three were put to the user and answered before implementation.

| # | Question | Recommended default | Answer |
| --- | --- | --- | --- |
| 1 | How does the `npm audit` gate land, given 70 open alerts? | Report-only now, blocking once `SEC-03` is fixed | **Fix `SEC-03` first, then land blocking.** Done — the audit baseline is 0, so the gate is blocking from day one and any failure is a real regression. |
| 2 | NuGet lock files now, or defer? | Yes — lock files + `--locked-mode` | **Yes.** The roadmap parked the decision here precisely so it would be made with CI as the consumer. |
| 3 | Pin the Node version, closing `BUILD-07`? | Yes — `.nvmrc` + `engines` | **Yes.** CI must name a version regardless; two places stating it with nothing keeping them equal is the drift ADR-011 removed for package versions. |

## 6. Domain impact

- **New/changed entities:** none
- **New/changed properties on `Recipe`:** none
- **New/changed invariants:** none
- **New/changed shape validation:** none
- **Migration required:** no
- **Known limitations touched:** none

No file under `RecipeManager.Domain/`, `RecipeManager.Application/`, `RecipeManager.Infrastructure/`,
`RecipeManager.Api/`, or `recipe-manager-frontend/src/` is modified by this spec.

## 7. API impact

None. No endpoint, DTO, route, cache key, or status code changes. `08-api-contract` is not involved.

## 8. Frontend impact

- **New routes:** none
- **New/changed components:** none
- **New/changed hooks:** none
- **States to design:** none — no UI change
- **Design tokens needed:** none

The only frontend files touched are `package.json` (an `engines` field) and a new `.nvmrc`. No `src/` file
changes, so `03-senior-react`'s component and state rules are not engaged.

## 9. Architecture impact

- **ADR required:** yes → ADR-013 in [../architecture.md](../architecture.md). Not because CI is structural in
  the layering sense, but because `RestorePackagesWithLockFile` in `Directory.Build.props` changes the restore
  behaviour of **every project**, which is the same blast radius as ADR-010 and ADR-011 and was decided the same
  way.
- **New dependency:** none in either ecosystem. No package is added to `Directory.Packages.props` or to
  `package.json`. The workflow consumes only first-party GitHub Actions.
- **Layer/dependency changes:** none. `Domain ← Application ← Infrastructure ← Api` is untouched.
- **New DI registrations:** none.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| One job running everything sequentially | Simplest file; one log to read; no risk of a matrix mistake | The backend and frontend checks are genuinely independent, so serialising them roughly doubles wall-clock for no benefit. Worse for feedback: a frontend lint error would be hidden behind a five-minute backend test run. Two jobs mean both failures are visible in the same run. |
| Trust `dotnet list package --vulnerable`'s exit code | One line, reads naturally, looks exactly like a gate | **It exits 0 even when it finds vulnerabilities** — verified locally. A step that cannot fail is the precise `BUILD-03` failure mode this whole item exists to eliminate, and it would be *worse* than omitting the step, because the green tick would be evidence of nothing. The output must be parsed. |
| Rely solely on `NuGetAudit`/`NU1903` failing the build (ADR-010) | Already works; no extra step | It is genuinely the stronger gate and it stays. But it only fires on **restore**, so with `--locked-mode` and an unchanged lock file the advisory database is not necessarily re-consulted on every run. The explicit step is the belt to that braces, and it names the failure clearly in the log. |
| Pin actions by tag (`actions/checkout@v4`) | Readable; auto-receives fixes within the major | A tag is mutable. Whoever controls the repository can repoint `v4` at new code, which is how the 2025 `tj-actions/changed-files` compromise leaked secrets from thousands of repos. Pinning to a commit SHA makes the action content-addressed and immutable. |
| SHA-pin without Dependabot | Immutable, no automation to configure | The pins then rot silently, and a security fix in an action never arrives. Adding the `github-actions` Dependabot ecosystem is what makes SHA pinning maintainable rather than a decision you regret in six months. |
| Skip lock files (option B on question 2) | Smallest diff; nothing new to maintain | CI could legitimately resolve a different transitive closure than a developer did, with **no diagnostic at all** — the same silent-failure shape ADR-011 removed for direct versions. |

- **Pattern applied:** *continuous integration as an enforced gate* — and, more specifically, the same move as
  ADR-010 and ADR-012: **turn "should" into "cannot"**. Existing example in this repo:
  `RecipeManager/Directory.Build.props`'s `TreatWarningsAsErrors`.
- **What this makes harder:** every PR now waits on CI, and a red pipeline blocks work that may be unrelated —
  a flaky or slow run is friction on every change, not an occasional annoyance. Lock files add a step to every
  dependency bump that is easy to forget and fails with an unhelpful message. And CI creates a real temptation
  to stop running checks locally, which makes the feedback loop *slower* for the individual even as it becomes
  reliable for the repo.

## 10. Security impact

- **New user-controlled input:** none
- **User content rendered in the SPA:** unchanged
- **File upload:** no
- **Auth/ownership implications:** none. Every endpoint remains anonymous and world-writable — `SEC-01` and
  `SEC-02` are untouched and remain the two Critical items on the deploy gate.
- **Config/secrets touched:** none. The workflow declares `permissions: contents: read` and uses **no** secrets;
  it never needs the database password, and nothing in it can read one.
- **Standing gaps affected:** **improves** the posture that `SEC-03` sat in. Dependency regressions become
  merge-blocking in both ecosystems rather than notifications nobody acts on. Closes `INFRA-01` and `INFRA-06`,
  and closes the `CI green on every PR` row of the deploy gate.

Specific hardening applied to the workflow itself, since a CI workflow is an execution surface:

- `permissions: contents: read` at the top level — the default token is otherwise broadly scoped, and a
  compromised action inherits whatever the token can do.
- Third-party actions pinned by **commit SHA**, not tag. See the alternatives table.
- Trigger is `pull_request`, not `pull_request_target` — the latter runs with a **writable** token in the base
  repo's context while checking out untrusted PR code, which is the standard way CI is turned into a secret
  exfiltration path.
- No secrets referenced, so a fork PR gains nothing by running it.

## 11. Acceptance criteria

- [ ] Given a PR to `main`, when it opens, then the backend and frontend jobs both run. **Only verifiable once
      the workflow is on GitHub** — confirmed on the PR for this spec, not locally.
- [x] Given a deliberate compiler warning, when CI runs, then the backend job **fails** (ADR-010 via CI).
- [x] Given a deliberately failing test, when CI runs, then the backend job **fails**.
- [x] Given a deliberate TypeScript error, when CI runs, then the frontend job **fails** before Vite bundles.
- [x] Given a deliberate `console.log`, when CI runs, then the frontend job **fails** at lint.
- [x] Given a `packages.lock.json` that does not match the `.csproj` files, when CI runs, then restore **fails**
      under `--locked-mode` rather than silently resolving something new.
- [x] Given a vulnerable NuGet package, when CI runs, then the backend job **fails** — and it must fail because
      the output was parsed, not because the exit code was trusted.
- [x] All 14 integration tests pass on the Linux runner, demonstrating `INFRA-06` is an environment constraint
      and not a defect in the tests.
- [x] The workflow references no secret and requests no write permission.

## 12. Test plan

- **Domain unit tests:** none — no domain change.
- **Handler unit tests:** none — no handler change.
- **Integration tests:** none added. The existing 14 run in CI for the first time, which is the point.
- **Negative tests — the actual verification.** Per the 2026-08-04 decisions-log entry, a gate never observed
  rejecting anything is indistinguishable from no gate. Each acceptance criterion above that says "fails" is
  proven by deliberately breaking it on a scratch commit, observing red, and reverting. Locally where the
  command allows it; on a throwaway PR where only CI can show it.
- **Not covered, and why:**
  - **Nothing verifies the app still works.** CI runs no frontend test (`TEST-01`/`R-07`) and no end-to-end
    test. A PR can be fully green and the app broken in the browser.
  - **Integration tests still run on EF InMemory** (`TEST-06`). Green CI does not imply the code works against
    PostgreSQL; provider-specific behaviour still needs manual verification until `R-06`.
  - **Dependabot's alert list is not the same thing as `npm audit`.** CI gates on `npm audit`; Dependabot alerts
    are observed separately and can differ.
- **Manual verification:** open a draft PR and read the checks. Confirm both jobs appear, timings are sane, and
  the summary is legible.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-013 — lock files and the workflow's shape |
| 3 | `02-senior-csharp` | `Directory.Build.props`, `packages.lock.json` generation, backend job |
| 4 | `03-senior-react` | `.nvmrc`, `engines`, frontend job |
| 5 | `06-qa-tester` | negative tests for every gate; coverage statement |
| 6 | `04-code-reviewer` | review |
| 7 | `05-security-reviewer` | workflow permissions, action pinning, trigger choice |

Rows deleted: `08-api-contract` (no contract change), `07-ux-ui` (no visual change) — see section 4.

## 14. Assumptions made

- **`ubuntu-latest` is the right runner.** It is free for public repos, and `INFRA-06` makes Linux the only
  environment where the integration tests are trustworthy. The cost: CI never exercises Windows, which is the
  platform actually developed on, so a Windows-only breakage would not be caught. Accepted — the project targets
  Linux containers (`mcr.microsoft.com/dotnet/aspnet:10.0`).
- **Node 24 in `.nvmrc`**, matching the machine this was verified on (24.18), with `engines: >=20` matching
  `README.md`'s stated floor. The two say different things deliberately: one is what is used, the other is what
  is supported.
- **`--audit-level=high` for `npm audit`**, matching the roadmap's own wording. With a baseline of 0 a stricter
  level is available, but low and moderate dev-scope advisories appear constantly and would block unrelated PRs
  — the fastest route to a gate people route around.
- **Checks are not automatically *required*.** Making them required is a repository setting only the owner can
  change; until that is done, a red pipeline is advisory. Section 15.

## 15. Follow-ups

- **Enable branch protection on `main`** requiring both CI checks. Until this is done the pipeline reports but
  does not block, and the item is only two-thirds delivered. This is a GitHub settings change, not a code change.
- **Enable Dependabot pull requests** in repository settings if `dependabot.yml` alone does not activate them.
- `R-06` — Testcontainers, now unblocked (Docker is available on the runner).
- `R-07` — frontend tests; the workflow gains a `npm test` step when a `test` script exists.
- `TEST-05` / `BUILD-06` — agree a coverage number and fix the coverage script, then add a coverage step.
- `INFRA-02` / `INFRA-03` — versioning and rollback remain open; CI publishes nothing.

## 16. Known issues and roadmap items touched

- Fixes: `INFRA-01`, `INFRA-06`, `BUILD-07`, roadmap `R-04` — **Phase 1 is now complete**
- Adds: `INFRA-07` (CI checks exist but are not required to merge)
- Depends on: `R-03`/ADR-012 (the npm scripts this calls), `SEC-03` (so the audit gate can be blocking)
- Unblocks: `R-06` (Docker in CI), `R-07` (a place to run frontend tests)
- On the [deploy gate](../roadmap.md#deploy-gate)? **yes** — closes the `CI green on every PR` row
