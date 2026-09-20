# Decisions log

A running record of decisions taken on this project **and what was learned from them** — written to be
re-readable months later, when the reasoning has been forgotten but the code is still here.

## How this differs from the other documents

| Document | Answers |
| --- | --- |
| [architecture.md](architecture.md) ADRs | *What* was decided structurally, in the terse formal record |
| [learning-mode.md](learning-mode.md) | *How* agents must explain decisions |
| **This file** | *Why* a decision was made, what was rejected, and **what it taught** — including decisions too small for an ADR |

An ADR is a record for the codebase. This is a record for the person building it.

## What goes in

- Any decision you would struggle to justify in six months.
- Anything where you learned something you did not know before, including from a bug.
- Decisions that were **reversed** — those teach more than the ones that held.

## What stays out

- Routine applications of an existing convention.
- Anything already fully captured by an ADR with nothing to add.

Newest first. Read bottom-up to follow the project chronologically. Entries are append-only: when a decision is
superseded, add a new entry and link back rather than editing history.

---

## Concept index

Jump to every entry touching a topic.

| Concept | Entries |
| --- | --- |
| CQRS / dispatching | [2026-07-26 Scrutor](#2026-07-26--auto-register-handlers-instead-of-listing-them), [2025-08-28 CQRS without MediatR](#2025-08-28--hand-rolled-cqrs-instead-of-mediatr) |
| Layering / dependency direction | [2026-09-16 Error kinds implemented](#2026-09-16--rank-errors-by-what-they-mean-not-by-where-they-sit), [2026-07-26 Error kinds](#2026-07-26--http-status-codes-do-not-belong-in-the-domain) |
| Error handling | [2026-09-16 Error kinds implemented](#2026-09-16--rank-errors-by-what-they-mean-not-by-where-they-sit), [2026-09-13 FluentResults 4.0](#2026-09-13--take-a-library-major-when-it-is-cheap-not-when-it-is-needed), [2026-07-26 Error kinds](#2026-07-26--http-status-codes-do-not-belong-in-the-domain), [2025-09-18 FluentResults](#2025-09-18--expected-failures-are-values-not-exceptions) |
| Caching | [2026-09-18 Cold cache](#2026-09-18--a-cold-cache-cannot-go-stale-so-prime-it-before-testing-invalidation), [2025-08-30 Decorator](#2025-08-30--caching-as-a-decorator-not-as-handler-code) |
| Domain modelling | [2026-09-19 Design as dependency graph](#2026-09-19--a-ui-design-is-a-dependency-graph-in-disguise), [2026-07-26 Structured ingredients](#2026-07-26--free-text-ingredients-are-a-shortcut-with-an-expiry-date) |
| Testing | [2026-09-18 Cold cache](#2026-09-18--a-cold-cache-cannot-go-stale-so-prime-it-before-testing-invalidation), [2026-09-18 Extract to test](#2026-09-18--extract-logic-out-of-a-component-to-test-it-rather-than-test-it-through-rendering), [2026-09-17 Testcontainers shipped](#2026-09-17--a-test-that-cannot-run-is-not-a-test-that-passes), [2026-07-26 Testcontainers](#2026-07-26--ef-inmemory-is-not-a-database), [2025-10-08 Integration tests](#2025-10-08--integration-tests-need-an-escape-hatch-and-escape-hatches-need-guards) |
| Project direction | [2026-09-19 Design as dependency graph](#2026-09-19--a-ui-design-is-a-dependency-graph-in-disguise), [2026-07-26 Project stance](#2026-07-26--practice-project-with-deployment-intent) |
| Tooling / infrastructure | [2026-09-19 Measure a latch](#2026-09-19--measure-a-latch-before-you-arm-it), [2026-09-19 Received-file gate](#2026-09-19--a-regeneration-workflow-that-only-works-where-the-tests-run-is-not-a-workflow), [2026-09-19 Generator's own TypeScript](#2026-09-19--the-openapi-generator-gets-its-own-typescript), [2026-09-16 Parity then correctness](#2026-09-16--parity-was-the-bar-for-the-swap-not-for-what-came-after), [2026-09-16 Oxlint + TS 7](#2026-09-16--replace-the-tool-when-its-upstream-says-no), [2026-09-13 Vite 8](#2026-09-13--compare-what-a-toolchain-upgrade-produces-not-what-it-prints), [2026-08-08 CI builds Debug](#2026-08-08--ci-must-build-debug-because-a-security-guard-from-2025-says-so), [2026-08-08 Remediate before you gate](#2026-08-08--remediate-before-you-gate-and-check-what-is-installed-rather-than-what-is-allowed), [2026-08-08 Frontend gate](#2026-08-08--a-check-that-cannot-start-and-a-check-that-passes-look-identical), [2026-08-04 Warnings as errors](#2026-08-04--a-warning-nobody-has-to-fix-is-a-warning-that-multiplies), [2026-07-25 .NET 10 + PostgreSQL](#2026-07-25--net-10-and-postgresql) |
| Enforcement vs. convention | [2026-09-19 Measure a latch](#2026-09-19--measure-a-latch-before-you-arm-it), [2026-09-19 Received-file gate](#2026-09-19--a-regeneration-workflow-that-only-works-where-the-tests-run-is-not-a-workflow), [2026-09-16 Parity then correctness](#2026-09-16--parity-was-the-bar-for-the-swap-not-for-what-came-after), [2026-09-16 Oxlint + TS 7](#2026-09-16--replace-the-tool-when-its-upstream-says-no), [2026-08-08 CI builds Debug](#2026-08-08--ci-must-build-debug-because-a-security-guard-from-2025-says-so), [2026-08-08 Remediate before you gate](#2026-08-08--remediate-before-you-gate-and-check-what-is-installed-rather-than-what-is-allowed), [2026-08-08 Frontend gate](#2026-08-08--a-check-that-cannot-start-and-a-check-that-passes-look-identical), [2026-08-04 Warnings as errors](#2026-08-04--a-warning-nobody-has-to-fix-is-a-warning-that-multiplies), [2026-07-26 Scrutor](#2026-07-26--auto-register-handlers-instead-of-listing-them), [2025-10-08 Integration tests](#2025-10-08--integration-tests-need-an-escape-hatch-and-escape-hatches-need-guards) |
| Dependency management | [2026-09-19 Generator's own TypeScript](#2026-09-19--the-openapi-generator-gets-its-own-typescript), [2026-09-16 Oxlint + TS 7](#2026-09-16--replace-the-tool-when-its-upstream-says-no), [2026-09-13 Vite 8](#2026-09-13--compare-what-a-toolchain-upgrade-produces-not-what-it-prints), [2026-09-13 Remove MUI](#2026-09-13--remove-a-dependency-whose-footprint-is-smaller-than-its-upgrade), [2026-09-13 FluentResults 4.0](#2026-09-13--take-a-library-major-when-it-is-cheap-not-when-it-is-needed), [2026-08-08 Remediate before you gate](#2026-08-08--remediate-before-you-gate-and-check-what-is-installed-rather-than-what-is-allowed), [2026-08-04 Warnings as errors](#2026-08-04--a-warning-nobody-has-to-fix-is-a-warning-that-multiplies) |
| Frontend / React | [2026-09-19 Design as dependency graph](#2026-09-19--a-ui-design-is-a-dependency-graph-in-disguise), [2026-09-19 Generator's own TypeScript](#2026-09-19--the-openapi-generator-gets-its-own-typescript), [2026-09-18 Extract to test](#2026-09-18--extract-logic-out-of-a-component-to-test-it-rather-than-test-it-through-rendering), [2026-09-16 Parity then correctness](#2026-09-16--parity-was-the-bar-for-the-swap-not-for-what-came-after), [2026-09-16 Oxlint + TS 7](#2026-09-16--replace-the-tool-when-its-upstream-says-no), [2026-09-13 Vite 8](#2026-09-13--compare-what-a-toolchain-upgrade-produces-not-what-it-prints), [2026-09-13 Remove MUI](#2026-09-13--remove-a-dependency-whose-footprint-is-smaller-than-its-upgrade), [2026-08-08 Frontend gate](#2026-08-08--a-check-that-cannot-start-and-a-check-that-passes-look-identical) |
| Accessibility | [2026-09-20 Design file as proposal](#2026-09-20--a-design-file-is-a-proposal-about-colour-not-a-verdict), [2026-08-08 Frontend gate](#2026-08-08--a-check-that-cannot-start-and-a-check-that-passes-look-identical) |
| API contract | [2026-09-19 Received-file gate](#2026-09-19--a-regeneration-workflow-that-only-works-where-the-tests-run-is-not-a-workflow), [2026-09-19 Generator's own TypeScript](#2026-09-19--the-openapi-generator-gets-its-own-typescript) |

---

## Entries

### 2026-09-20 — A design file is a proposal about colour, not a verdict

**Context.** `R-16`'s canonical design (spec [009](specs/009-editorial-design-system-and-shell.md) §8.2) gave
exact hex values for every token. Three of them, measured against the surface they are actually used on, fail
WCAG before a line of app code ever uses them — ratios below are read directly from `light.css`/`dark.css` and
match spec 009 §8.2:

| Token | Design | Shipped | Why |
| --- | --- | --- | --- |
| `--accent` (light) | `#2563eb` | `#1d4ed8` | design value measures 4.47:1 on `--paper-2`, under AA body's 4.5:1, exactly where the design places small accent links; shipped is 6.32:1 on `--paper` / 5.80:1 on `--paper-2` |
| `--danger` (dark) | `#b42318` — the design's one danger colour, defined only for its light frame and reused as-is in dark | `#f87171` | that value measures 2.76:1 on dark `--paper` — a fail, and `UX-01`'s exact mistake (a status colour shared across themes and checked only against light); shipped is 6.57:1 on `--paper` / 6.05:1 on `--paper-2` |
| `--field-border` | *(none — the design bounds its search field with `--rule`)* | `#8a8275` (light) / `#726e7d` (dark) | `--rule` measures 1.29:1 (light) / 1.36:1 (dark), failing WCAG 1.4.11's 3:1 for the boundary that identifies a control; shipped is 3.58:1 / 3.28:1 (light), 3.67:1 / 3.38:1 (dark) |

**Decision.** Ship the measured values instead of the design's, in all three cases. `--field-border` did not
exist in the design at all — `--rule` was reused for input borders in the design's own search field, and a new
token was introduced rather than repeating that failure everywhere else a control needs a boundary (`UX-06`).
Recorded in ADR-021 and spec 009 §8.2.

**Rejected.** *Ship the design's values as drawn.* It is the literal reading of "canonical design reference"
([agents/07-ux-ui.md](agents/07-ux-ui.md#canonical-design-reference)), and it is what a design-fidelity review
would flag as a deviation — but it ships text and control boundaries that fail contrast against exactly the
surfaces the design puts them on, which is a worse outcome than a hex value one step off from the mockup.
*Ask the design owner before deviating.* The design owner is `07-ux-ui`, which is also the agent that measured
the failure; deferring the numbers to a human sign-off when the tool that produced the mockup cannot check its
own contrast would have delayed three fixes with no real ambiguity in the answer.

**Cost.** The shipped palette no longer matches the design file pixel-for-pixel, so a side-by-side comparison
shows a difference a reviewer has to know is deliberate. Three ADR-021 comments carry the ratio and the reason
so that "why doesn't this match the design?" has an answer in the code, not only in this log.

**Takeaway.** *A design file is a proposal about colour, not a verdict — measure before adopting.* A mockup
tool has no way to check contrast against the exact surface a token will sit on in both themes, so treating its
hex values as final skips the one check that actually matters for text and controls. The fix is cheap once the
ratio is known: measure every colour against the surface it is actually used on, before it ships, not after a
review catches it.

---

### 2026-09-19 — A UI design is a dependency graph in disguise

**Context.** Phase 3 was about to start with `R-10` (structured ingredients) when a complete redesign of the SPA
arrived: the editorial design, six screens in light and dark. The obvious question was whether to land the new
frontend first and return to the domain afterwards, on the theory that the UI and the data model are
independent.

**Decision.** Neither "frontend first" nor "domain first". Each screen of the design was mapped to the data it
needs, and the roadmap was re-sequenced from that. Only the design system and the shell (`R-16`) turned out to
be independent. Detail, Add/Edit, and Cooking mode are all built on quantities, units, and per-step
ingredients, so they come after `R-10` and the new `R-17`. The design also brought in four domain concepts the
roadmap had never mentioned (drafts, tags, a cook log, export/import), which became `R-19`–`R-24`. Two were
decided on the spot: ingredients stay owned by their recipe with no catalogue, and "only the title is
required" becomes draft recipes rather than a weaker aggregate.

**Rejected.** *(a)* Building the screens first on today's `string[]` contract. It looks faster and gives visible
progress, but every screen that shows an ingredient would be built twice, and the domain model warns against
exactly this ("do not build features that entrench free-text ingredients"). *(b)* Treating the design's copy as
a specification. Its Add/Edit screen claims the validator requires only a title, which is false (`UX-05`).
Building to it would have produced a form whose submissions the API rejects. *(c)* Loading fonts and icons the
way the prototype does, from Google Fonts. A prototype's implementation choices are not decisions. Self-hosting
fits an app meant to run on its owner's server, and keeps the future CSP simple.

**Cost.** Visible frontend progress is limited to the shell until `R-10` and `R-17` ship, and those are
backend-heavy. The roadmap grew by nine items, each needing its own spec, and most of them need an ADR as well. The design now lives outside the repo, so it can drift from what is documented here, and
`UX-05` is the first example.

**Takeaway.** *Read a mockup as a list of the data it assumes.* Every number, badge, and button on a screen is a
claim about what the backend can answer. Listing those claims per screen turns "which goes first, UI or
domain?" from a matter of taste into a topological sort. It also flushes out the features a design adds
without saying so, and the places where its copy has quietly drifted from the rules.

---

### 2026-09-19 — Measure a latch before you arm it

**Context.** `EnforceCodeStyleInBuild` had been on since ADR-010 and enforced nothing, because there was no
`.editorconfig`. `R-15` warned that adding one would turn every chosen rule into a build error in every file at
once, and framed the item as risky enough that "drop the property" was a legitimate outcome.

**Decision.** Adopt, enforcing `IDE0055` formatting, `IDE0005` unused usings, and `IDE0161` file-scoped
namespaces at `warning`. `var` is documented but not enforced. The reformat went in its own commit, listed in
`.git-blame-ignore-revs`. ADR-020.

**Rejected.** *(a)* Dropping the property: honest, but the 21 block-scoped files that had drifted under a
written convention are the evidence that review alone does not hold a style rule. *(b)* Everything at
`suggestion`: it looks like adoption and enforces exactly what nothing did before. *(c)* A CI-only
`dotnet format --verify-no-changes`: the check would disagree with the local build and report minutes later.

**What measuring first showed.** Before choosing anything, read-only `dotnet format … --verify-no-changes` runs
put a number on each candidate rule: 22 formatting fixes in 7 files, 21 namespace conversions, and 137 hits
from .NET's default modern-C# suggestions, which are not this repo's style at all. The "every file at once"
risk turned out to be real only for rules nobody wanted. Three things surfaced that the roadmap entry did not
mention:
- `IDE0005` is **not reported by a build** unless `GenerateDocumentationFile` is on. Turning that on raises
  `CS1591` on every public member, and `TreatWarningsAsErrors` makes that fatal, so the rule costs a property plus
  a justified `NoWarn`.
- EF migrations are generated block-scoped. Without `generated_code = true` on `Migrations/`, the next
  `dotnet ef migrations add` would have broken the build, long after this PR and far from its cause.
- The first red build reported **6** errors, not ~30, because MSBuild stops at the first failing project
  (Domain) and never compiles its dependants. A build's error count is a lower bound; `dotnet format` reports
  across the whole solution.

**Cost.** Style errors now stop a build mid-refactor. Every build writes XML documentation files. `git blame`
needs a one-time `git config` per clone to skip the reformat.

**Takeaway.** *Before arming a gate that fails everything at once, run it in report-only mode and count.* The
count turns a vague fear ("it will break every file") into a specific choice between rules, and the side paths
you don't know about (generated code, hidden prerequisites, a build that stops early) show up as numbers that
don't add up, instead of as surprises in the next unrelated PR.

---

### 2026-09-19 — A regeneration workflow that only works where the tests run is not a workflow

**Context.** `INFRA-06` (Settled) resolved Smart App Control by making CI the authority: the integration tests
are trustworthy there and merely inconvenient locally. `OpenApiContractTests` (`R-09`, ADR-019) broke that
resolution the moment it existed, because it is not just *inconvenient* to run under Smart App Control — it
cannot run there at all, `FileLoadException` on every attempt, same as the rest of `RecipeManager.IntegrationTests`.
The gate's whole purpose is to be re-run by whoever changes a DTO, and on this project's own development
machine, that person cannot run it.

**Decision.** `OpenApiDocument_ShouldMatchCommittedSnapshot` writes the document it actually got to
`contracts/openapi.received.json` on any mismatch, before failing — the "received file" pattern the Verify
library uses for golden-master tests — and deletes that file on a pass. CI uploads it as the `openapi-received`
artifact when the job fails. That gives two acceptance routes: run the snapshot update locally where the test
runs at all, or — from a machine that can never run it — push, download the artifact CI produced, and promote
it by hand. Documented in ADR-019 and the README's "Changing the API contract".

**Rejected.** Leaving the ritual as "run this test locally" and treating Smart App Control as background noise,
the same shrug `INFRA-06` already used for the rest of the suite. It works for tests that merely need to *pass*
somewhere; it fails for a test whose job is to be *re-run on demand* by the person making the change, since that
person now has no route to a green local run at all — not even a slow one.

**Cost.** A test that writes a file is an unusual idiom on first read and needs the comment that explains it.
The artifact is upload-on-failure only, so a developer who does not know to look for it will see a red PR with no
obvious next step until they read the failure message, which now names both routes explicitly.

**Takeaway.** *A regeneration workflow that only works where the tests can run is not a workflow — it is a
description of a workflow that excludes whoever is standing at the machine that can't run them.* Related:
**a snapshot tool should write what it got, not merely report that it differs.** The mismatch message alone
would have told the author something was wrong; the received file is what let them fix it without a second
environment.

---

### 2026-09-19 — The OpenAPI generator gets its own TypeScript

**Context.** `R-09` needed `openapi-typescript`, which declares `peer typescript ^5.x`. The frontend runs
TypeScript 7 (ADR-016), whose `node_modules/typescript/lib` holds only a `tsc.js` launcher.

**Decision.** The generator lives in `RecipeManager/contracts/` with a private, pinned TypeScript 5.9.3 (ADR-019).

**Rejected.** An `npm overrides` entry giving the generator a nested TS 5 fails with `ERESOLVE`: npm satisfies a
peer from the parent's tree, so the override only rewrote the range it could not meet. `--legacy-peer-deps`
would work by disabling peer checks for the whole project, including the Vite/plugin-react pair that has already
broken once.

**Cost.** Two TypeScript versions, a second lockfile, and a Dependabot `ignore` rule that must not be removed
while the generator needs TS 5.

**Takeaway.** *A tool can depend on a compiler as a library, not as a compiler. That coupling is invisible until
the compiler changes runtime.* Codegen tools use `ts.factory` to print code, and a compiler rewritten in Go took
that away without breaking a single type-check. Separately: overrides change what a package asks for, never
where a peer is resolved from.

---

### 2026-09-18 — A cold cache cannot go stale, so prime it before testing invalidation

**Context.** `TEST-02`: nothing verified that `CachedRecipeRepository` invalidates its entries on writes. The
roadmap described the fix as "create → list contains it; update → detail shows new values; delete → detail
returns 404" (`R-08`, spec 006).

**Decision.** Every test reads through the API *before* the write (the priming read), writes, then reads again.
A fourth test proves a cache hit happens, by changing the row directly in the database between two `GET`s and
asserting the stale value. Each test's sensitivity was proven by a mutation check: each invalidation call and
the `Decorate` registration were removed one at a time, and at least one test had to fail.

**Rejected.** The roadmap's scenarios taken literally. They look correct and cannot fail: without a priming read
the cache entry does not exist, so the re-read goes to the database whether or not invalidation works. With
caching removed altogether, all three would still pass. Also rejected: unit-testing the decorator with a mocked
`ICacheService`. It is fast and precise, but it pins key names and call order rather than what a client sees.

**Cost.** The cache-hit test asserts stale data on purpose and writes behind the API's back, which reads like
a bug without its comment. Designing the tests also exposed `BUG-14`: the update handler mutates the cached
instance in place, so "update → detail shows new values" passes even when `recipe_{id}` is never invalidated.
That one assertion is honest about being insensitive rather than pretending to cover it.

**Takeaway.** *Before trusting a test, ask what change to the code would make it fail. If the answer is
"none", it is not a test.* For caches specifically, a test has to put the system into the state where the bug
would show (a warm entry) before triggering the code that should fix it. Removing the line under test and
watching the test go red (mutation testing, done by hand) is the cheapest way to know.

---

### 2026-09-18 — Extract logic out of a component to test it, rather than test it through rendering

**Context.** `formatDuration` and `getISODuration` lived inside `RecipeCard`
(`recipe-manager-frontend/src/components/ui/Recipe/RecipeCard/RecipeCard.tsx`), unreachable except by rendering
a card. They were the top item on the first frontend test list (`R-07`).

**Decision.** Move both into `recipe-manager-frontend/src/utils/duration.ts` and unit-test them directly,
writing the tests first against the new path and reading the expected values off the old implementation
(ADR-018).

**Rejected.** Testing through rendering — zero production change and closer to what users see, which is what a
competent engineer choosing it would be optimising for. But eight-plus boundary cases would each render a
component, and a failure could not say whether formatting or rendering broke.

**Cost.** A test-motivated refactor of production code inside a "tests only" item, and the repo's first
`utils/` folder — which will attract things that belong elsewhere unless watched.

**Takeaway.** *When logic is hard to test, the difficulty is usually information about where the logic lives.*
Pure calculation embedded in a component is a design smell the test merely exposed; a characterisation test
written before the move is what makes the refactor safe.

---

### 2026-09-17 — A test that cannot run is not a test that passes

**Context.** Implementing `R-06` (spec [004](specs/004-testcontainers-integration-tests.md), ADR-017): swapping
EF InMemory for real PostgreSQL via Testcontainers. The 2026-07-26 entry below had already settled *that* it
should happen; this is what doing it taught.

**Decision.** One container per test assembly with a database per test class, schema by `Database.Migrate()`,
and — the part that needed deciding on the day — **skip rather than fail** when Docker is missing, since the
author's machine has none and `INFRA-06` had already made CI the authority for these tests.

**Rejected.** Failing hard without Docker, which is the honest option and was rejected anyway: a suite that is
red for an environment reason gets ignored, and an ignored red suite is worse than a loud skip. Also rejected:
`EnsureCreated()`, which would have tested a schema that is not the deployed one.

**Cost.** Three things, all bought knowingly. A skipped test is silent — locally `dotnet test` now reports 85
passed and 14 skipped, and nothing stops someone reading only the first number. xUnit 2.x has **no dynamic
skip** (`Assert.Skip` is v3 only), so the skip cost a second dependency, `Xunit.SkippableFact`, and an
attribute change on every integration test. And the first implementation skipped nothing at all: Testcontainers
probes Docker in `PostgreSqlBuilder.Build()`, not in `StartAsync()`, so building the container in a field
initialiser threw in the fixture constructor and failed all 14 tests — the exact failure mode the design was
meant to avoid, shipped and then found by running it.

**Takeaway.** *Verify the failure path by running it, not by reasoning about it.* The plan named the skip
mechanism as its one unknown and named a fallback — and both the assumption and the fallback turned out to rest
on an API that does not exist in this xUnit version. Two minutes of running the thing replaced two plausible
paragraphs. The corollary for reading results: a green suite is only evidence about the tests that actually
executed, so the skip count is part of the result, not a footnote to it.

---

### 2026-09-16 — Rank errors by what they mean, not by where they sit

**Context.** Implementing ADR-009 (`R-05`, spec [003](specs/003-domain-error-kinds.md)). `RecipeErrors` put HTTP
statuses in domain metadata, and `ResultExtensions` answered with the first error's status. No handler returned
mixed kinds yet, so the bug was latent and no integration test could reach it.

**Decision.** A `sealed DomainError : Error` carries an `ErrorKind`; `ResultExtensions.Classify` maps each kind to
a status and a severity in one `switch` whose default arm throws. The most severe error wins; an error with no
kind outranks everything and keeps its old 400. `Conflict` waits for its first producer.

**Rejected.** *Kind as a metadata key* — the smallest diff, but a misspelt key silently becomes a 400.
*One subclass per kind* — FluentResults' usual idiom, but no enumerable set of kinds to prove the mapping
complete. *Keep `errors.First()` and order errors carefully* — no code at all, but correctness then rests on
every handler's discipline. *Map a kind-less error to 500* — probably right, but it changes a live status, so it
went to `SEC-06`.

**Cost.** A new kind needs three coordinated edits. `RecipeManager.UnitTests` now compiles ASP.NET Core. The
change was staged over three commits (add kinds beside codes, switch the reader, delete codes) so that each one
left the suite green — slower than one commit, but each step was reviewable on its own.

**Takeaway.** *When a latent bug cannot be reached through the system, test the unit that owns the decision, and
make the decision exhaustive so the next case cannot be forgotten.* A default arm that throws turns "someone
forgot to map it" from a wrong status in production into a red test.

---

### 2026-09-16 — Parity was the bar for the swap, not for what came after

**Context.** ADR-016 kept Oxlint's `correctness` category off so the linter swap checked exactly what ESLint had
checked, and recorded the resulting gap, root tooling files with no rules at all, as `BUILD-10`.

**Decision.** Turn `correctness` on for every file, and type-check `vite.config.ts` through a separate
`tsconfig.node.json` (ADR-016, amended). Also remove the unused `ts-node` (`BUILD-08`).

**Rejected.** *A rule list in a root-files override*: overrides cannot set categories, so it would be a second
hand-maintained list. *Adding `vite.config.ts` and `types: ["node"]` to `tsconfig.json`*: one file fewer, but Node's
globals would type-check in browser code again, undoing #32. *A solution-style `tsconfig.json` with references*
(the Vite template layout): the same result with more restructuring; `tsc -b` accepts several project files
directly.

**Cost.** `src/` now gets 88 rules nobody chose one by one. They found nothing today, but a future finding may
be a style disagreement dressed as correctness. `typecheck` is now `tsc -b` (incremental), not `tsc --noEmit`, so
it keeps `*.tsbuildinfo` state, already gitignored.

**Takeaway.** *Change one thing at a time, then go back.* Holding a migration to strict parity is what made its
verification meaningful. The parity constraint then has to be deliberately released, or the recorded gap
becomes permanent by default.

---

### 2026-09-16 — Replace the tool when its upstream says no

**Context.** Dependabot #19 bumped `typescript` 5.9 → 7.0.2 and failed at `npm ci`: `typescript-eslint` peers on
`typescript <6.1.0`. That was not a lag waiting to close. TypeScript 7 is a Go rewrite whose npm package ships a
binary and no JavaScript API, and typescript-eslint's type-aware rules are built on that API. The upstream issues
asking for TS 7 support were closed as not planned. Two TS 7 errors were genuinely ours (`baseUrl`, `process.env`)
and were fixed separately in #32, so this change could be purely the tool swap.

**Decision.** Replace ESLint + typescript-eslint with Oxlint in type-aware mode (`oxlint-tsgolint` embeds its own
typescript-go), move to TypeScript 7.0.2, pin all three exactly, and group them in Dependabot (ADR-016). The rule
set was translated by `@oxlint/migrate` and then checked mechanically: the committed `.oxlintrc.json` has the same
71 rules with the same options as the migration output. The PR supersedes #19, and #35 and #36 (ESLint 10),
which the removal makes moot.

**Rejected.** *Waiting for typescript-eslint* — the maintainers have said it is not planned, so "later" has no
date. *Biome* — 46 of 72 rules, and none of the `no-unsafe-*` family, which is the reason to lint with types at
all. *Oxlint without types* — the same loss. *Floating version ranges* — `oxlint-tsgolint@7.0.2001` is named after
the TypeScript it embeds; `^` ranges would let `tsc` and the linter drift onto different compilers without a
signal.

**What the translation found.** Listing the rules per file pattern showed the ESLint block for root tooling files
enabled **nothing**. It only switched typed rules off, and there was no global rule block, so `vite.config.ts` was
parsed against zero rules while ADR-012 described it as linted. Recorded as `BUILD-10`, and deliberately carried
over unchanged, so this PR swaps the tool without also changing what is checked.

**Cost.** `prefer-optional-chain` is nursery-only in Oxlint and no longer enforced. Oxlint's type-aware mode has
been stable only since July 2026, and its rule behaviour is close to, not identical to, typescript-eslint's; the
negative test (six rules fired on a probe file) samples that, it does not prove it. Rule names in config are
spelled differently (`typescript/…`, `react/…`). In exchange: 183 → 83 installed packages, and lint in ≈0.7 s instead of ≈3 s.

**Takeaway.** *Distinguish "not yet" from "not ever".* A peer-dependency conflict usually means waiting a few
weeks; when the blocking project has declined the work, waiting is a decision to stay put indefinitely, and it
should be taken on purpose or not at all. And a config migration is an audit for free: enumerating what the old
config *actually* enabled per file is how an unnoticed gap surfaced.

---

### 2026-09-13 — Compare what a toolchain upgrade produces, not what it prints

**Context.** Dependabot #16 bumped `@vitejs/plugin-react` to 6, a major that peers on `vite` 8, while leaving
`vite` on 7, so it failed at `npm ci`. Vite 8 swaps the whole engine under an unchanged config: Rolldown bundles,
Oxc transforms and does React Fast Refresh, Lightning CSS minifies. After the upgrade, typecheck, lint, build and
audit were all green with only a one-token config change.

**Decision.** Upgrade to Vite 8.3 + plugin-react 6.1 (ADR-015), tighten `engines` to Vite's own floor, and add a
`vite` Dependabot group so the family can never be split across PRs again. Closed #16.

**Why a green build was not the evidence.** None of the gates can see the two things that changed: the CSS the
minifier emits, and Fast Refresh, which only exists in the dev server. The frontend has no tests (`TEST-01`), and
the manual smoke test used for ADR-014 ran on the dev server, which does not minify CSS at all. It would have
passed whatever Lightning CSS did. So the check had to be built around the change:
- *Computed styles.* A script recorded ~40 computed properties for every element on both routes in both themes
  on a Vite 7 production build, stored them in `localStorage` (same origin, so they survive a rebuild), and diffed
  the Vite 8 build against them. It was first shown to report **0** diffs against itself and **1** diff for an
  injected 1 px padding change, so a clean result meant something.
- *The stylesheet itself*, because computed styles only cover one viewport. Both CSS files were parsed with the
  browser's CSSOM and compared as longhand property maps per selector and media condition. The raw texts differ
  in dozens of places; semantically, every difference was an equivalence: reordered declarations,
  `(max-width: 768px)` → `(width <= 768px)`, `#ffffff` → `#fff`, `background: transparent` → `0 0`, two adjacent
  `opacity: 1` rules merged into one selector list.
- *Fast Refresh.* With "lemon" typed into search, `RecipePage`'s placeholder was edited on disk. The page
  hot-updated, the page did not reload, and the typed state survived. That is the specific behaviour the Babel → Oxc
  swap could have broken.

**Rejected.** *Deferring* — safe today, but the PR re-raises weekly and the gap grows under `R-07`. *A text diff of
the CSS* — every rule it flagged, even after normalising class-name hashes, turned out to mean the same thing. *Trusting the green build* — it cannot see
either of the changed components. *An `update-types: [major]` Dependabot group* — whether a minor update then
falls through to the minor/patch group is not documented, so the group matches every update type instead.

**Cost.** Babel plugins can no longer go through `react({ babel })`. The default build target is newer. The
`vite` group opens its own PR for Vite minors instead of folding them into the weekly minor/patch PR. The
verification took longer than the upgrade; it is not repeatable without re-writing the scripts (`TEST-01`).

**Takeaway.** *Aim the evidence at what actually changed.* A passing gate only proves what that gate looks at, so
before trusting it, ask which component the upgrade replaced and whether any check observes that component. And
test the test: a diff tool that has never been seen to report a difference proves nothing when it reports none.

---

### 2026-09-13 — Remove a dependency whose footprint is smaller than its upgrade

**Context.** Dependabot #17 bumped `@mui/icons-material` 7 → 9 and could not install: icons 9 peers on
`@mui/material` 9, which arrived in no PR. The obvious question was "upgrade MUI or defer". Counting call sites
first changed the question: two `<Box>` elements and four icons were the entire usage, and the conventions
already forbade `sx`, `styled` and a `ThemeProvider`.

**Decision.** Remove MUI and Emotion (ADR-014). Closed #17 without upgrading.

**How it was evaluated.** All three paths were built on `84226dc` and measured, not estimated. MUI 9.4.0: zero
code changes, JS 406.90 kB / 135.24 gzip. No MUI: JS 327.49 / 107.26, 53 fewer packages. The icons keep MUI's path
data byte for byte (checked by script against `node_modules`). Icon sizes were then read with
`getComputedStyle` on desktop and at 375 px, before and after, rather than judged from screenshots.

**What the measurement found that inspection would not.** `SearchBar.module.css` sets the search icon to 18 px
under 420 px, but on the MUI build it rendered at 24 px: the same rule's `left` applied, its `width` did not,
because Emotion appends its styles to `<head>` after the CSS Modules and wins the tie at equal specificity. The
`!important`s in `Logo.module.css` and `Footer.module.css` were the same fight, won by force. The replacement puts
the icon defaults in `:where(.icon)`, which has zero specificity, so any consumer class wins whatever order Vite
emits the files in.

**Rejected.** *Upgrading* — cheap once and genuinely safe, but it re-buys the whole library at every MUI and
Emotion major for four glyphs, and MUI 9 raises the browser floor to Safari 17. *A small icon library* — a
dependency to render four SVGs, with different glyphs. *Deferring* — stays on a line with no release since
May 2026.

**Cost.** New icons are copied in by hand. The search icon is visibly smaller on phones (the size its stylesheet
always asked for). If the recipe form ever wants an accessible date picker or dialog, a component library comes
back as a decision with this entry to argue against, rather than already being installed.

**Takeaway.** *Before asking "upgrade or defer", count the call sites.* A dependency is worth its upgrade cost
only in proportion to what it does for you, and a bundle-size number is a weaker argument than a runtime style
engine silently overriding your stylesheet. An `!important` added to beat a library is evidence that the library
and the codebase are working against each other.

---

### 2026-09-13 — Take a library major when it is cheap, not when it is needed

**Context.** Dependabot opened #21, FluentResults 3.16.0 → 4.0.0. FluentResults is the expected-failure channel
for the whole app (ADR-002), so a major bump is an architecture change, not a routine update. #21's CI never got
past `dotnet restore --locked-mode` (NU1004 — Dependabot updated the `Direct` lock entries but not the
`CentralTransitive` ones), so its red check said nothing about compatibility.

**Decision.** Upgrade, on a fresh branch rather than Dependabot's. Superseding #21.

**How it was evaluated.** From the library source at tags `v3.16` and `v4.0`, not from the release-note titles.
The prediction — exactly one compile break, `ResultExtensions.CreateProblemDetails(List<IError>)` receiving
the now-`IReadOnlyList<IError>` `Errors` — was then confirmed by building before fixing anything: 3 errors, all
that one signature. Fixed by widening the parameter to `IReadOnlyList<IError>` rather than adding `.ToList()`
at the three callers, because the method only reads the list. "Deconstruct operators (BREAKING)" cost nothing —
no `Result` is deconstructed anywhere.

**Why upgrade when no 4.0 feature is needed.** The new helpers (`FailIfNotEmpty`, `Merge` on enumerables,
`OrFailIf`) are not used. The case was:
- *A semantic trap closes.* In 3.16, `IsFailed` is computed as "has any error", so `Result.Fail(emptyList)`
  returned a **successful** result — and for `Result<T>`, a success with a `default` value. 4.0 throws
  instead. Every current call site is guarded, so this is protection for future code, recorded in ADR-002.
- *`Errors` was a fresh copy on every read* in 3.16, so `result.Errors.Add(x)` compiled and did nothing. It is
  now a compile error.
- *The break only grows.* Every new method taking `List<IError>` would add to it.

**Rejected.** Deferring with `@dependabot ignore this major version`. 3.16 has no known vulnerability, so this
was legitimate — but the ignore silences **every** 4.x update, including any future security patch, and pins
the project to a line upstream no longer releases (the last 3.x release was June 2024). It trades a one-line fix
today for a suppression someone must remember to lift.

**Cost.** An empty collection reaching `Result.Fail` is now a 500 through `ErrorHandlerMiddleware` rather than a
silent success — the right direction, but a new exception path. FluentResults 4.0 ships no `net10.0` build, so
the app consumes the `net9.0` asset. Upstream releases roughly yearly, so a 4.x defect may sit unfixed as long as
a 3.x one would have.

**Takeaway.** *A release-note title is not an impact assessment.* The "BREAKING" item was free and the
innocent-sounding "introduce ReadOnlyList type" was the actual break. Read the diff of the types you consume,
predict the break, then let the compiler confirm it before changing code — and weigh "defer" by what the
deferral silences, not only by what the upgrade costs.

---

### 2026-08-08 — CI must build Debug, because a security guard from 2025 says so

**Context.** `R-04` added the CI pipeline. Choosing `--configuration Release` for the build and test steps was
reflex: it is what CI conventionally does, and it is what most guides show.

**Decision.** CI builds and tests in **Debug**. ADR-013.

**Why the reflex was wrong here.** ADR-005 makes `Program.Main` throw
`"IntegrationTest environment is not allowed in RELEASE builds"` — a deliberate guard added in 2025 (commit
`898c9ce`) so that setting `ASPNETCORE_ENVIRONMENT=IntegrationTest` on a real deployment cannot start the app
with no database configured. `WebApplicationFactory` uses exactly that environment name. So a Release CI build
fails **all 14** integration tests by design. Measured, not reasoned about: 84 passing became 70, with
`InvalidOperationException` from `Program.cs:20` on every one.

**Rejected.** *(a)* Relaxing the ADR-005 guard so Release works — this inverts a security control to satisfy a
convention, and the guard is compile-time precisely so it cannot be argued with. *(b)* Building Release and
running tests in Debug — two builds, double the time, to gain almost nothing. *(c)* Excluding the integration
tests from CI, which would discard the single biggest thing CI buys this repo (`INFRA-06`).

**Cost.** A Release-only compilation difference would not be caught. Genuinely accepted rather than waved past:
`TreatWarningsAsErrors` is unconditional rather than Release-only (ADR-010), so the warning gate is identical in
Debug, and the deployable artefact is built by `Dockerfile`, not by this workflow.

**The other thing turning the gate on found.** Three of them, and none was in the plan:

- **`dotnet list package --vulnerable` exits 0 even when it finds vulnerabilities.** A step written the obvious
  way would have been a green tick that could never go red — the `BUILD-03` shape again, in code I was writing
  *to prevent* it. The step parses the output instead. Verified both directions against synthetic output, and
  against a real `Newtonsoft.Json@12.0.1`, where `NU1903` at restore turned out to fire first anyway.
- **A malformed XML comment silently disabled `Directory.Build.props` entirely.** The comment I added contained
  `--`, which is illegal inside an XML comment, so the file failed to parse and *every* property vanished. The
  error MSBuild reported was `NETSDK1013: The TargetFramework value '' was not recognized` on all six projects —
  pointing at a property I had not touched, in files I had not opened. I formed and tested two confident
  theories about NuGet's restore internals before running `dotnet msbuild -getProperty`, which named the real
  cause in one line.
- **All 18 npm advisories were fixable inside the existing semver ranges** — the finding recorded in the
  entry below.

**Takeaway.** *Check whether your project's own recorded decisions constrain the tool you are reaching for,
before reaching for the conventional configuration of it.* ADR-005 was written about deployment safety and
turned out to dictate a CI flag three items later; nothing linked the two, and only running it revealed the
connection. Corollary, learned the expensive way: **when an error names something you did not change, suspect
the file you did change of not parsing at all.** A syntax error in a config file does not report itself as a
syntax error — it reports as every value being absent, arbitrarily far from the cause.

---

### 2026-08-08 — Remediate before you gate, and check what is installed rather than what is allowed

**Context.** `R-04` (CI) specifies a `npm audit` step. `SEC-03` recorded 68 open Dependabot alerts — 70 by the
time it was measured, two having been published in the interim. A blocking audit step would therefore have
failed on the very PR that introduced it.

**Decision.** Fix `SEC-03` first, in its own PR, then land `R-04`'s audit step **blocking** against a clean
baseline. Chosen over the two alternatives below.

**Rejected.** *(a)* Land the audit step report-only and promote it later. Smaller and unblocks CI immediately —
but it ships a step that cannot fail, which is precisely the `BUILD-03` shape this project has already been
burned by, and "promote it later" is enforced only by memory. *(b)* Land it blocking with the alerts still
open, so the pipeline tells the truth from day one. Honest, but every PR until remediation merges over a red
check, and a check people routinely override has negative value — it costs attention and buys nothing.

**Cost.** `R-04` slips behind a dependency upgrade, and the upgrade carried real risk with no automated net:
`TEST-01` means zero frontend tests exist, so an `axios` and `react-router` bump was verified by hand in a
browser. That is not a repeatable guarantee, and it is the strongest argument yet for `R-07`.

**What the fix actually found.** `npm audit fix` cleared all 13 advisories and **`package.json` did not
change** — only `package-lock.json`. Every fix was already inside the declared semver ranges. Two things follow:

- `SEC-03`'s warning that `react-router` and `vite` were "majors-adjacent" was **wrong**. Every bump was minor:
  `axios` 1.10→1.19, `react-router` 7.7→7.18, `vite` 7.0→7.3. The entry had been treating an unverified fear as
  a finding, and it deterred the fix for months. Corrected in the [Settled](known-issues.md#settled) row.
- Nothing had gone wrong in `package.json`. The ranges permitted every patched version the whole time. The lock
  file had simply never been re-resolved, and no tool asks it to.

**Takeaway.** *A dependency range says what is permitted; only the lock file says what you actually run — and
nothing reconciles the two on your behalf.* Being "on `^1.10.0`" while shipping 1.10.0 for a year is the normal
case, not an anomaly. Related but separable: **remediate before you gate.** A new gate should be introduced
against a baseline it passes, or the first thing it teaches everyone is how to ignore it.

---

### 2026-08-08 — A check that cannot start and a check that passes look identical

**Context.** `R-03`. `npm run lint` had been unable to *start* since 2025-08-08: the config was renamed to
`eslint.config.ts` and ESLint 9 loads a TypeScript flat config through `jiti`, which was never added
(`BUILD-03`). `npm run build` was `vite build`, and esbuild strips types without checking them (`BUILD-04`). So
the frontend had two scripts that looked like gates and were not.

**Decision.** Add `jiti`; `"build": "tsc -b && vite build"`; add `"typecheck"`. Scope the type-aware ESLint
rule sets to `src/**` — the only files `tsconfig.json` includes — and lint the root tooling configs without type
information. ADR-012.

**Rejected.** *(a)* Renaming the config to `.js`: no new dependency, but it discards type checking of the config
and the typed `tseslint.config()` helper already in use — reversing a 2025 decision to avoid a dev-only loader
that never ships to a browser. *(b)* Merging the gate PR green and fixing the lint findings later: smaller diff,
but a gate merged red is not a gate. *(c)* `ts-node`, which was already installed and is presumably a previous
attempt at this fix — factually the wrong loader, and it sat there for a year looking like the problem was
handled.

**Cost.** Every frontend PR now has to satisfy `recommendedTypeChecked` + `stylisticTypeChecked`, and a build
that used to succeed with a type error now fails. Same shape of friction as ADR-010, and the same answer: the
friction is the mechanism.

**What turning it on actually found.** Twelve errors, and the interesting ones were not style:

- `ThemeContext` was created with a **default value**, so `useContext` never returned `undefined` and
  `useTheme`'s `if (!ctx) throw` was unreachable code. The guard-hook pattern this repo documents as one of its
  examples had never worked. `Footer` also called `useContext(ThemeContext)` directly, bypassing the hook —
  which is why nobody hit the missing-provider case that would have exposed it.
- `no-console` was **never configured**. `BUG-08` was recorded as "ESLint would have caught this had it been
  runnable"; it would not have. The unrunnable linter was hiding a second gap, not causing this one.
- `RecipeList` passed an `onClick` whose whole body was a `console.log`, so every card rendered as
  `<button aria-label="View X recipe">` that did nothing when activated. Deleting the log left a no-op handler
  and made the accessibility lie visible. Cards are now `<article>`; `BUG-10` tracks the missing detail route.
- The unused `Recipe` import that appeared after that deletion was caught by **`tsc`, not ESLint** —
  `no-unused-vars` is configured with `varsIgnorePattern: '^[A-Z_]'`, which exempts every PascalCase binding.
  Two gates, genuinely different coverage.

**Verified by negative test**, per the 2026-08-04 entry: a deliberate `const x: number = "s"` fails
`npm run build` before Vite runs, and a deliberate `console.log` is reported by `npm run lint`.

**Takeaway.** *An unrun check and a passing check are indistinguishable from outside — and the moment you can
finally see through the window, expect the room to be dirtier than the ticket said.* The estimate for this was
30 minutes for three one-line edits. What it actually surfaced was a documented pattern that had never
functioned, a rule everyone assumed existed, and an accessibility defect hiding inside a debug statement. Budget
for that: the value of turning on a disabled check is mostly in what it reports, not in the switch.

---

### 2026-08-04 — A warning nobody has to fix is a warning that multiplies

**Context.** Seven build warnings had sat in `RecipeManager.UnitTests` since the .NET 10 upgrade
(`BUILD-01`, `BUILD-02`). Every one was a ~2-minute fix. They survived several PRs anyway, because a warning is
a line of scrollback between "Build succeeded" and the prompt, and the correct number of those to read is zero.
Meanwhile `TargetFramework`, `Nullable`, and `ImplicitUsings` were copy-pasted into all six `.csproj` files.

**Decision.** Fix the seven, then add `RecipeManager/Directory.Build.props` owning those three properties plus
`TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` — **unconditionally, not Release-only**. ADR-010, `R-02`.

**Rejected.** *(a)* Release-only enforcement, the common advice: it keeps local iteration frictionless and
matches what CI would run. Wrong **here** specifically, because there is no CI (`INFRA-01`) — the local Debug
build is the only gate that exists, so conditioning on Release would have enforced nothing while looking like a
control. *(b)* Fixing the seven warnings and stopping there — the count is then free to grow straight back.
*(c)* An explicit `WarningsAsErrors` rule list: surgical, but somebody has to maintain it and it cannot catch a
new class of warning.

**Cost.** Real and daily: an unused local while mid-refactor now fails the build. That friction *is* the
mechanism — the whole point is that the fix cannot be deferred. `EnforceCodeStyleInBuild` also currently
reports nothing (IDE rules default to suggestion severity with no `.editorconfig`); it is a latch that arms
itself the day one is added, which is worth knowing so nobody assumes style is being checked today.

**The step that mattered.** After adding the gate, a deliberate broken build — an unused local, `CS0219` —
confirmed it actually fails. `BUILD-03` is this project's own proof of why: `npm run lint` had been unable to
*start* for eleven months and nobody noticed, because an unrun check and a passing check look identical from
outside.

**Follow-on, same PR.** Having removed the duplicated *properties*, the obvious next question was what else was
duplicated — and it was every package **version**: ten of them declared in two projects each. That one is worse,
because bumping EF Core in `Infrastructure` and not `Api` produces no warning at all; NuGet resolves
nearest-wins and the mismatch appears at runtime. Fixed with central package management (ADR-011), and verified
the same way: putting a `Version` back into a `.csproj` now gives `error NU1008`.

**Takeaway.** *Turn "should" into "cannot".* The gap between a documented standard and an enforced one is
filled entirely by human attention, which is the least reliable component available — and the failure is silent
by construction, since nothing reports the warnings you stopped reading. Then verify the enforcement by
breaking it on purpose: a gate you have never seen reject anything is indistinguishable from no gate.

---

### 2026-07-26 — Free-text ingredients are a shortcut with an expiry date

**Context.** `Recipe.Ingredients` is `IReadOnlyList<string>`, stored as PostgreSQL `text[]`. It works, and the
app functions — which is exactly what makes a shortcut dangerous, because nothing forces a reckoning.

**Decision.** Move to structured ingredients (`Quantity`, `Unit`, `Name`). Recorded as `R-10`. Explicitly
labelled a temporary shortcut in [domain-model.md](domain-model.md) so future work does not entrench it.

**Rejected.** Keeping free text permanently. It is genuinely simpler, and for a recipe *viewer* it would be
enough — but it makes serving scaling, shopping lists, unit conversion, and ingredient search impossible, and
those are the features a recipe manager eventually grows.

**Cost.** The migration is the hard part: `"200g flour"` cannot be parsed into structured data reliably, so
existing rows need a strategy decided in advance. Delaying makes this strictly worse as data accumulates.

**Takeaway.** *The cost of a data-model shortcut is not paid when you take it — it is paid at migration time,
and it grows with every row.* Recognising which shortcuts have an expiry date, and writing that down while the
reasoning is fresh, is the difference between a deliberate trade-off and accidental debt. Writing "this is
temporary" in the docs is what keeps it from silently becoming permanent.

---

### 2026-07-26 — EF InMemory is not a database

**Context.** The 14 integration tests run against `Microsoft.EntityFrameworkCore.InMemory`. They pass. They also
cannot detect anything provider-specific: `text[]` behaviour, PostgreSQL identifier folding, real constraint
violations, or concurrency.

**Decision.** Move to Testcontainers with real PostgreSQL (`R-06`), deferred until CI exists since it needs
Docker in both places.

**Rejected.** Staying on EF InMemory. It is faster and needs no Docker — but the EF Core team itself recommends
against it for integration testing, precisely because passing tests imply guarantees the provider does not
give.

**Cost.** Slower tests and a Docker dependency for every developer and every CI run.

**Takeaway.** *A test suite's value is capped by how closely it resembles production.* A green suite that
cannot fail for the reasons production fails is worse than a smaller honest one, because it converts unknown
risk into false confidence. Worth asking of any test setup: what class of bug is this **structurally incapable**
of catching?

---

### 2026-07-26 — HTTP status codes do not belong in the domain

**Context.** `RecipeErrors.TitleRequired()` attaches `.WithCode(422)` — an HTTP status — to a domain error. So
`RecipeManager.Domain`, the project that references nothing precisely so it depends on nothing, encodes
knowledge of HTTP.

**Decision.** Domain errors will carry a semantic kind (`Validation`, `NotFound`, `Conflict`); the API layer
owns the kind → status mapping. ADR-009, `R-05`.

**Rejected.** Leaving it. It works today and keeps controllers thin, which is why it was done — a reasonable
call under delivery pressure, not a careless one.

**Cost.** Touching every `RecipeErrors` factory plus `ResultExtensions`, with the integration tests as the
regression net.

**What made it worth fixing.** The layering violation was abstract, but it produced a concrete bug:
`ResultExtensions.CreateProblemDetails` derives the response status from `errors.First()`, so a `Result`
carrying both a 404 and a 422 returns whichever happens to be first. That is not a coincidence.

**Takeaway.** *Layering violations announce themselves as bugs before they announce themselves as design
problems.* "The domain shouldn't know about HTTP" sounds like purity until it produces a wrong status code.
When a rule feels academic, the useful question is not "is this principled?" but "what does breaking it
actually cost me?" — and if you cannot answer, the rule may genuinely not apply.

---

### 2026-07-26 — Auto-register handlers instead of listing them

**Context.** Every CQRS handler had to be added by hand to `ServiceInitializer.RegisterCqrsHandlers()`. A
forgotten line compiled cleanly and threw `InvalidOperationException` at runtime, on the first request that
dispatched that command.

**Decision.** Discover handlers by assembly scanning with Scrutor. ADR-008. **Implemented 2026-08-03** —
`RegisterCqrsHandlers` deleted, the scan added to `RegisterCqrsDispatchers`, and
`CqrsHandlerRegistrationTests` added as the resolution net.

**Rejected.** *(a)* Keeping manual registration and relying on the review checklist — but a rule enforced only
by attention fails eventually, and this one fails in production. *(b)* Adopting MediatR, which solves this and
adds pipeline behaviours — but it is commercially licensed from v12, and the hand-rolled dispatcher was a
deliberate choice worth preserving.

**Cost.** Registration becomes implicit and therefore invisible. Mitigated by a container-resolution test that
asserts every handler interface resolves — otherwise the failure just moves somewhere less obvious.

**The detail that decided it.** Scrutor was **already a dependency**, used only for `Decorate`. The fix cost
nothing new.

**Takeaway.** *Prefer failures the compiler or a test can catch over failures that require discipline.* And
before adding a library to solve a problem, check what the existing dependencies already do — Scrutor's
scanning is its main feature, and this project had been using only its smallest one.

---

### 2026-07-26 — Practice project with deployment intent

**Context.** Whether the missing authentication, CI, and versioning are critical defects or acceptable
simplifications depends entirely on what this project is for — and that had never been written down.

**Decision.** Practice project with deployment intent: the production-grade bar applies, and security gaps are
**sequenced behind a [deploy gate](roadmap.md#deploy-gate) rather than waived**.

**Rejected.** *(a)* "Local tool only" — would have justified permanently dropping auth and rate limiting, but
forecloses deployment and removes the reason to practise those patterns at all. *(b)* "Production product now"
— would make `SEC-01`/`SEC-02` block every feature, which is wrong for something still being learned on.

**Cost.** More items stay open than a purely local tool would carry, and the deploy gate must be honoured
rather than quietly eroded when deployment starts to look appealing.

**Takeaway.** *"What is this project for?" is a technical question, not a philosophical one.* It determines
severity ratings, what counts as done, and which corners are legitimate. It is worth answering explicitly and
early — every later prioritisation call inherits from it.

---

### 2026-07-25 — .NET 10 and PostgreSQL

**Context.** The project ran on .NET 8 with SQL Server (`Integrated Security=True`, Windows-only auth).

**Decision.** Upgrade all six projects to `net10.0`, replace `Microsoft.EntityFrameworkCore.SqlServer` with
Npgsql, pin the SDK in `global.json`. ADR-007. Commit `d2d490d`.

**Cost, and the parts that were not obvious.** The connection string had to become a **password-less
template** — SQL Server integrated auth needed no password, PostgreSQL does, so the secret moved to
user-secrets. `nvarchar(max)` became `text`, `uniqueidentifier` became `uuid`, and `Ingredients`/`Instructions`
went from a JSON string to a native `text[]` — a *better* representation, since PostgreSQL can query arrays.
PostgreSQL also folds unquoted identifiers to lowercase while EF creates `"Recipes"`, so raw SQL needs quoting.

The package upgrades that came along with it introduced **7 build warnings** in the test project
(`BUILD-01`, `BUILD-02`) — NSubstitute 6 annotated `Arg.Is<T>` as nullable, and xUnit's analyzer started
flagging `[InlineData(null)]` on non-nullable parameters.

**Takeaway.** *A database swap is never only a connection-string change.* Types, identifier casing, secret
handling, and the capabilities available to you all shift. And a coordinated dependency upgrade will surface
new analyser warnings in code that did not change — budget for that instead of treating it as noise.

---

### 2025-10-08 — Integration tests need an escape hatch, and escape hatches need guards

**Context.** Integration tests need to substitute the real database, but `Program.cs` registers the DbContext
and applies migrations at startup, so `WebApplicationFactory` alone could not intervene.

**Decision.** `Program.Main` skips DbContext registration and migrations when
`EnvironmentName == "IntegrationTest"` — and **throws in RELEASE builds** if that environment name is used.
ADR-005. Commits `b15bb11`, then `898c9ce`.

**Why the second commit exists.** The first version added the escape hatch. The follow-up added the `#if DEBUG`
guard, because an environment variable that disables database configuration is a production hazard: set
`ASPNETCORE_ENVIRONMENT=IntegrationTest` on a real deployment and the app starts in an undefined state.

**Takeaway.** *Any hook added for testing is also an attack surface and an operational footgun.* The right
reflex is to add the hatch and the guard together — ask immediately "what happens if someone sets this in
production?" Notice that the fix here was a compile-time guard, not documentation or a naming convention: the
hatch cannot exist in a release binary at all.

---

### 2025-09-18 — Expected failures are values, not exceptions

**Context.** Validation failures and not-found conditions needed to reach the client as proper HTTP responses.

**Decision.** Adopt FluentResults. Domain and application failures return `Result`/`Result<T>`; only genuinely
unexpected failures throw and are caught by `ErrorHandlerMiddleware`. ADR-002. Commits `1a1f6de`, `2f3fb86`.

**Rejected.** Custom exception types per failure (`RecipeNotFoundException`) mapped in middleware. Common in
.NET, and it keeps handlers terse — but it uses exceptions for control flow, which is expensive, hides the
failure path from the method signature, and makes "this can fail" invisible at the call site.

**Cost.** Every caller must check `IsFailed`; nothing forces them to. The compiler will not catch an ignored
`Result` the way an uncaught exception announces itself.

**Takeaway.** *A return type that includes failure makes the failure path visible; an exception makes it
invisible.* `Task<Result<RecipeDto>>` tells you this can fail before you read the body. The trade is that
exceptions are impossible to ignore silently and `Result` is not — which is the actual reason this pattern
needs discipline, and the honest counter-argument to it.

---

### 2025-08-30 — Caching as a decorator, not as handler code

**Context.** Recipe reads were repetitive and hit the database every time. The obvious implementation is a
cache lookup at the top of each query handler.

**Decision.** `CachedRecipeRepository` implements `IRecipeRepository` and wraps the real one, wired with
Scrutor's `services.Decorate(...)`. Handlers are unaware caching exists. ADR-003. Commit `1f7e8c0`.

**Rejected.** Cache lookups inside the handlers — fewer files and a more obvious control flow, but it puts an
infrastructure concern in the application layer, repeats itself in every handler, and makes handler unit tests
require a cache mock.

**Cost.** Caching becomes invisible at the call site: a developer reading `GetAllRecipesHandler` sees no hint
that results may be stale. And every new `IRecipeRepository` method must be implemented **twice**, with an
invalidation decision each time.

**Takeaway.** *The decorator pattern's value is that the decorated code does not change and does not know.*
That is also its cost — behaviour becomes invisible where it is used. It is the right trade when the added
behaviour is genuinely orthogonal (caching, logging, retries) and the wrong one when it is part of what the
operation means. Worth noticing that the DI container is what makes this practical: `Decorate` swaps the
implementation without touching a single call site.

---

### 2025-08-28 — Hand-rolled CQRS instead of MediatR

**Context.** Commands and queries needed dispatching to handlers. MediatR is the default answer in .NET.

**Decision.** Custom `ICommand<T>`/`IQuery<T>` markers with `CommandDispatcher`/`QueryDispatcher` resolving
handlers from `IServiceProvider`. ADR-001. Commit `05656ed`.

**Rejected.** MediatR — mature, and its pipeline behaviours give validation, logging, and transactions for
free. Rejected to avoid a third-party dependency for roughly 40 lines of code, and because writing the
dispatcher makes the mechanism legible rather than magic. (MediatR moved to a commercial licence at v12, so
this aged well.)

**Cost.** No pipeline, so cross-cutting concerns need handler decorators; and handlers must be registered
manually — the problem [ADR-008](#2026-07-26--auto-register-handlers-instead-of-listing-them) later fixed.

**Takeaway.** *"Build it yourself" and "use the library" is a trade between control and unpaid maintenance —
and for a learning project the calculus differs from a commercial one.* Writing the dispatcher is ~40 lines and
teaches how mediator dispatch actually works; you then understand MediatR properly if you adopt it later.
Reaching for the library first would have made this the one part of the architecture that stayed opaque.

---

### Template for new entries

```md
### YYYY-MM-DD — <what was decided, as a statement>

**Context.** What forced the decision. Reference real files.

**Decision.** What was chosen. Link the ADR or roadmap ID if one exists.

**Rejected.** The alternatives, stated fairly — what would someone competent choosing them be optimising for?

**Cost.** What this makes harder. If nothing, you have not finished thinking.

**Takeaway.** *The generalisable lesson*, in one or two sentences — the part that transfers to other projects.
This is the line you will actually re-read.
```

Then add it to the [concept index](#concept-index).
