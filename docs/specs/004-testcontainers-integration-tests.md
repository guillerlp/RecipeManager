# Spec: Testcontainers for integration tests (R-06)

| | |
| --- | --- |
| **ID** | `004` |
| **Status** | draft |
| **Author** | `00-leader` |
| **Created** | 2026-09-17 |
| **Branch** | `chore/testcontainers-integration-tests` |

---

## 1. Context

*(Written before implementation; the present tense describes the state this item started from.)*

`IntegrationTestBase` swaps the real database for `Microsoft.EntityFrameworkCore.InMemory`, which is not a
relational provider: it has no SQL, no constraints, no collation, and no `text[]`. The 14 integration tests
therefore prove that routing, model binding, validation, DI, and the handler pipeline work — and prove nothing
about the database the application actually runs on. `TEST-06` records this, and it is unfixable while the
provider stays.

The gap is not theoretical. `BUG-11` (the `IReadOnlyList<string>` backing-field mapping) is explicitly marked
"verification must use real PostgreSQL", and every PR touching persistence currently has to carry a manual
statement of what was checked by hand.

`R-06` was deferred until CI existed, because a Docker-backed suite needs Docker in both places. `R-04`
(ADR-013) closed that: the `ubuntu-latest` runner provides Docker.

## 2. Goal

The integration tests run against a real PostgreSQL started per test run, with the schema created by the
project's own EF migrations, and the 14 existing tests keep passing unchanged.

## 3. In scope

- [ ] `Testcontainers.PostgreSql` added to `Directory.Packages.props` (test-only group) and to
      `RecipeManager.IntegrationTests.csproj`.
- [ ] `Microsoft.EntityFrameworkCore.InMemory` removed from both.
- [ ] `RecipeManager.IntegrationTests/packages.lock.json` regenerated.
- [ ] A shared `PostgresContainerFixture` (one container per test assembly) exposed through an xUnit collection.
- [ ] `IntegrationTestBase` creates its own database on that container, registers `UseNpgsql`, and applies
      migrations.
- [ ] Integration tests are skipped, with a message naming Docker, when Docker is unavailable.
- [ ] Docs updated (section 16), `R-06` deleted from the roadmap, `TEST-06` deleted from known issues,
      ADR-017 recorded, decisions-log entry appended.

## 4. Out of scope

- **New test cases.** The roadmap's wording is "a swap of the base class, not a rewrite". The 14 tests keep
  their names, bodies, and assertions. What changes is what they run against.
- **`TEST-02` — cache invalidation through the HTTP client.** That is `R-08`, and it becomes materially easier
  once this lands, which is exactly why it stays a separate item with its own review.
- **`TEST-03` — order-sensitive instruction assertions**, and the `BUG-11` regression test. Both are now
  *possible*; neither is this item.
- **Production code.** No file under `RecipeManager.Api`, `.Application`, `.Domain`, or `.Infrastructure`
  changes. ADR-005 stands unmodified (section 8).
- **Concurrency, performance, and volume testing.** Real PostgreSQL makes them reachable; nothing here
  attempts them.
- **Making the CI checks required to merge.** `INFRA-07` is a branch-protection setting, not a file.

## 5. Domain impact

None. No entity, invariant, validator, migration, or column changes. This item changes only how the existing
schema is created during tests.

## 6. API impact

None. No endpoint, DTO, status code, route, or cache key changes. The 14 integration tests are the regression
net proving it.

## 7. Frontend impact

None.

## 8. Architecture impact

- **ADR required:** yes — **ADR-017**, "Integration tests run against real PostgreSQL in a container". A new
  test-only dependency with a Docker prerequisite is a structural commitment, not an implementation detail.
- **New dependency:** `Testcontainers.PostgreSql@4.15.0` (test-only). It **replaces**
  `Microsoft.EntityFrameworkCore.InMemory@10.0.12`, which is removed rather than left installed — a fake
  provider that is still referenced is a fake provider the next test will reach for.
- **Layer/dependency changes:** none. Only `RecipeManager.IntegrationTests` changes.
- **New DI registrations:** none in production. The test `ConfigureTestServices` call swaps
  `UseInMemoryDatabase` for `UseNpgsql`.

### Shape

Two pieces, in the test project only:

1. **`PostgresContainerFixture` + `PostgresCollection`** — one `PostgreSqlContainer` per test **assembly**,
   owned by an `ICollectionFixture<>`, image pinned to `postgres:18-alpine` (matching the version
   `README.md` installs). `IAsyncLifetime.InitializeAsync` starts it; a start failure is captured as a skip
   reason rather than rethrown.
2. **`IntegrationTestBase`** — keeps its public surface (`Client`, `Factory`, `Scope`, `DbContext`,
   `SeedDatabase<T>`) so no test body changes. Internally it now creates `TestDb_{Guid}` on the shared
   container, points `AppDbContext` at it with `UseNpgsql`, and calls `Database.Migrate()`.

The one structural change the tests can see: creating a container and a database is asynchronous, so the base
class moves from a constructor plus `IDisposable` to xUnit's `IAsyncLifetime`
(`InitializeAsync` / `DisposeAsync`, the latter dropping the database). Each test class gains the collection
attribute; nothing else in the 14 tests is edited.

### Isolation

One container, one database per test class — not one container per class, which is what the roadmap entry
sketched. With three test classes today the difference is three container starts versus one; the shape that
matters is the one that still holds when `R-07` and `R-08` add classes. Per-class databases preserve exactly
the isolation `TestDb_{Guid}` gives today.

### Schema creation

`Database.Migrate()`, not `EnsureCreated()`. `EnsureCreated` builds the schema from the EF model, so the tests
would run against a schema that is *not* the one deployed — the same category of near-miss this item exists to
remove. The side effect is the point: every test run now applies the committed migrations to an empty database,
which today is verified only by a developer starting the API by hand.

### Docker absent

The suite skips with a message naming Docker rather than failing. `INFRA-06` already established CI as the
authority for these tests — on a Windows machine with Application Control they cannot reliably load a freshly
built `RecipeManager.Api.dll` at all. A hard failure would mean `dotnet test` is red on a developer machine for
an environment reason, which trains people to ignore red.

The cost is real and accepted knowingly: a skipped test is a silent test. The mitigation is that CI has Docker
unconditionally, so the skip path can never be the one that matters for a merge decision.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Stay on EF InMemory | Speed, zero infrastructure | Not a relational provider; `TEST-06` is unfixable and `BUG-11` unverifiable. The EF Core team recommends against it for exactly this use |
| SQLite in-memory as the relational stand-in | Real SQL, no Docker | A different dialect with no `text[]` — the specific mapping this item exists to exercise still would not run |
| A GitHub Actions `services:` PostgreSQL container | No Docker API in test code; marginally faster CI | Only configures CI. Local runs would need a hand-provisioned database, so the two environments would diverge — the thing `.nvmrc` and `--locked-mode` were adopted to prevent |
| One container per test class (the roadmap's sketch) | Maximum isolation, including server version | Pays a container start per class for isolation a per-class database already provides |
| Shared database + Respawn between tests | Speed at scale | A second dependency and a reset step to get wrong, for 14 tests that do not need it |
| Inject `ConnectionStrings__DefaultConnection` so `Program`'s normal path runs | Also covers `MigrateDatabase()` and the real startup path | Changes production code and erodes the ADR-005 guard for a test-only benefit. Rejected deliberately; the cost is that `app.MigrateDatabase()` stays untested |
| Hard failure when Docker is missing | Honesty and noise | Turns an environment constraint into a red local suite (`INFRA-06`), which teaches developers to stop reading the result |

- **Pattern applied:** shared test fixture (xUnit collection fixture) over *ephemeral infrastructure* — the
  dependency is real but disposable, created and destroyed by the test run. Closest existing example in this
  repo: `IntegrationTestBase` itself, which already owns and disposes a `WebApplicationFactory` per test class.
- **What this makes harder:**
  - The suite now needs Docker. Developers without it see skips, not results.
  - CI gets slower (one image pull plus one container start per run).
  - The PostgreSQL version becomes a string literal in test code. Dependabot does not manage it, so it can
    drift from what `README.md` installs and from production.
  - A failing migration now breaks every integration test at once. Intended, but it means a migration mistake
    presents as 14 failures rather than one.

## 9. Security impact

- **New user-controlled input:** none.
- **User content rendered in the SPA:** no.
- **File upload:** no.
- **Auth/ownership implications:** none; the app remains unauthenticated (`SEC-01`).
- **Config/secrets touched:** none. The container's credentials are generated by Testcontainers and live only
  in the test process — no connection string is committed and `appsettings.json` is untouched.
- **Standing gaps affected:** none directly. `SEC-08`/`SEC-09` (length limits absent at the database level)
  become *testable* for the first time, since a real database can now be asked what it actually enforces. Not
  fixed here.
- **New surface in CI:** the workflow now pulls a public image, pinned by tag rather than by digest — a weaker
  standard than the SHA pinning the workflow applies to third-party actions. Recorded as a follow-up rather
  than silently accepted.

## 10. Acceptance criteria

- [ ] `Microsoft.EntityFrameworkCore.InMemory` appears in no `.csproj`, no `Directory.Packages.props`, and no
      `packages.lock.json`.
- [ ] Given Docker is available, when `dotnet test RecipeManager.sln` runs, then 99 tests pass (85 unit + 14
      integration) and the integration tests execute against PostgreSQL.
- [ ] Given Docker is unavailable, when `dotnet test RecipeManager.sln` runs, then the 85 unit tests pass, the
      14 integration tests are reported **skipped** with a message naming Docker, and the command exits 0.
- [ ] Given a test class runs, then its schema was created by `Database.Migrate()` — a migration that fails to
      apply fails the suite.
- [ ] Given the suite runs twice consecutively, then both runs report identical results (no state leaks
      between runs or between test classes).
- [ ] `git diff` for this PR touches only `RecipeManager.IntegrationTests/`, `Directory.Packages.props`, that
      project's lock file, and documentation.
- [ ] `dotnet build` reports 0 warnings, and `dotnet restore --locked-mode` succeeds.
- [ ] CI is green on the PR.

## 11. Test plan

- **Domain unit tests:** none affected.
- **Handler unit tests:** none affected.
- **Integration tests:** the existing 14, unchanged in name and body. They are the acceptance test for this
  item — if the harness is wrong, they fail.
- **What newly becomes real, having been unreachable before:** the `text[]` mapping for `Ingredients` and
  `Instructions`, PostgreSQL identifier folding (`"Recipes"`), actual `NOT NULL` and key constraints, real
  transaction and `SaveChanges` behaviour, and the migrations themselves.
- **Not covered, and why:**
  - Cache invalidation through the API — `TEST-02`/`R-08`; the tests still assert database state.
  - Instruction ordering — `TEST-03`; the assertions remain `BeEquivalentTo`.
  - `app.MigrateDatabase()` and the real startup path — skipped under the `IntegrationTest` environment by
    ADR-005, which this item deliberately leaves alone.
  - Concurrency — no optimistic concurrency exists to test.
- **Manual verification:** on this machine only the **skip** path, the build, and the 85 unit tests can be
  verified — Docker is not installed. The real path is verified by CI, following the `INFRA-06` precedent that
  CI is the authority for integration tests. No claim that the 14 tests pass will be made before a green run.

## 12. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-017, the dependency and isolation constraints (section 8) |
| 3 | `06-qa-tester` | fixture, base class, skip behaviour, coverage statement |
| 4 | `02-senior-csharp` | review of the EF/Npgsql details (migration, connection handling, disposal) |
| 5 | `04-code-reviewer` | review |

`08-api-contract`, `07-ux-ui`, `03-senior-react`: no contract or UI change. `05-security-reviewer`: no new
input, output, or secret handling — section 9 is recorded for the record, not as a trigger.

## 13. Decisions taken with the user (2026-09-17)

- Docker absent ⇒ integration tests **skip** with an explicit message, rather than failing hard.
- **One shared container per assembly**, with a fresh database per test class.
- Schema created by **real EF migrations**, not `EnsureCreated()`.
- Branch from `main` at `15004c1`; `R-05` had already been squash-merged as `#44`.

## 14. Assumptions made

- `postgres:18-alpine` is the right pin: `README.md` installs PostgreSQL 18 locally, and `docs/tech-stack.md`
  records that no version is enforced anywhere. This becomes the first place a version is pinned, which is an
  improvement worth stating rather than a side effect.
- Testcontainers' random port and generated credentials are used as-is; nothing in the tests depends on a
  fixed port.
- xUnit 2.9's dynamic skip is expected to work from the base class's `InitializeAsync`. **This is the one
  technical unknown in the plan** — dynamic skip is designed around the test body, and a fixture-time skip may
  be reported as a failure instead. It is verifiable locally, since no Docker here means the skip path is the
  path that runs. Fallback if it does not hold: a one-line guard at the top of each of the 14 tests, which is
  uglier and still correct.

## 15. Follow-ups

- `R-08` (cache invalidation through the HTTP client) becomes materially cheaper once this lands.
- `TEST-03` (order-sensitive instruction assertions) and the `BUG-11` regression test become possible.
- Pinning the container image by digest rather than tag, to match the SHA-pinning standard the CI workflow
  applies to third-party actions.
- `SEC-08`/`SEC-09` become testable: a real database can now demonstrate that no length limit exists at the
  schema level.

## 16. Known issues and roadmap items touched

- Fixes: `R-06` (delete from [../roadmap.md](../roadmap.md)), `TEST-06` (delete from
  [../known-issues.md](../known-issues.md)).
- Docs to update in the same PR, each currently describing EF InMemory as the integration-test database:
  `CLAUDE.md` (stack table, repository layout, the seed-data note), `README.md` (Docker prerequisite for the
  integration tests, plus troubleshooting), `docs/tech-stack.md` (package table row, PostgreSQL version row),
  `docs/architecture.md` (ADR-017; ADR-005 gains a note that it still stands and why),
  `docs/conventions.md` (the "InMemory database per test" line), `docs/agents/06-qa-tester.md` (current-state
  table, integration-test conventions, the "not reproducible" section),
  `docs/agents/02-senior-csharp.md` (the Npgsql verification caveat),
  `docs/workflows/bugfix-workflow.md` (the "InMemory, not PostgreSQL" caveat),
  `docs/known-issues.md` (`BUG-11`'s note that its regression test awaits `R-06`),
  `docs/decisions-log.md` (append; the 2026-07-26 entry stays as history).
- Depends on: `R-04`/ADR-013 (Docker on the CI runner) — already shipped.
- On the [deploy gate](../roadmap.md#deploy-gate)? no.
