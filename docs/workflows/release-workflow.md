# Release workflow

There is **no CI, no automated pipeline, and no published versioning scheme** in this repo (no `.github/`, no
tags in git history). What follows is the convention the existing history actually shows, plus the gaps that
need a decision.

---

## Branching — observed convention

From `git log`: every change lands on `main` through a GitHub pull request from a topic branch.

| Branch | Merged as | Topic |
| --- | --- | --- |
| `recipe_list_page` | PR #2 | frontend recipe list |
| `cache_recipes` | PR #3 | caching decorator |
| `error_handling` | PR #4 | FluentResults error handling |
| `unit_tests` | PR #5, #6 | unit and integration tests |
| `dotnet10-postgresql` | PR #7 | .NET 10 + PostgreSQL upgrade |

Rules:

1. **Never commit directly to `main`.** Branch, push, open a PR, merge via GitHub.
2. Branch names are lowercase and descriptive. Existing names use `snake_case` and `kebab-case`
   interchangeably; prefer `kebab-case` going forward, prefixed by intent:
   `feat/<topic>`, `fix/<topic>`, `chore/<topic>`, `docs/<topic>`.
3. One topic per branch. The `dotnet10-postgresql` branch is the upper bound of acceptable scope — a coordinated
   runtime + provider upgrade — and it still kept frontend `recipe-manager-frontend/src/` untouched.
4. Delete the branch after the merge.

## Commit messages — observed convention

Imperative mood, sentence case, no prefix or scope, one line, no body:

```
Implement decorator pattern repository cache
Fix possible recipe null cache
Upgrade to .NET 10 and migrate from SQL Server to PostgreSQL
```

Conventional Commits are **not** in use. Do not introduce `feat:` / `fix:` prefixes without agreeing it with the
user first, since it would split the history style.

## Versioning

There is none: no git tags, no `<Version>` property in any `.csproj`, and `package.json` pinned at `"0.0.0"`.
Nothing identifies what is deployed. Tracked as `INFRA-02` in [../known-issues.md](../known-issues.md).

---

## Pre-merge checklist

Run from `RecipeManager/`. Everything here is manual — nothing enforces it.

1. **Build is clean.**
   ```bash
   dotnet build RecipeManager.sln
   ```
   **Zero warnings is the standard.** Currently 7, all in `RecipeManager.UnitTests` (5× `CS8602`,
   2× `xUnit1012`) — tracked as `BUILD-01`/`BUILD-02` in [../known-issues.md](../known-issues.md), not accepted
   as a baseline. A *new* warning blocks the merge.

2. **All tests pass.**
   ```bash
   dotnet test RecipeManager.sln
   ```
   Currently **78 passing** — 70 unit + 8 integration.

3. **Frontend builds and type-checks.** From `recipe-manager-frontend/`:
   ```bash
   npm run build
   ```
   ```bash
   npx tsc --noEmit
   ```
   `npm run build` does **not** type-check (`BUILD-04`), so `tsc` must be run separately. `npm run lint` cannot
   run at all on a clean install (`BUILD-03`) — once that is fixed, add it here.

4. **Migrations.** If the model changed, exactly one new migration is committed together with its
   `.Designer.cs` and the updated `AppDbContextModelSnapshot.cs`. Confirm `Up` and `Down` are both correct —
   `app.MigrateDatabase()` applies migrations automatically on every startup, including in production.

5. **No secrets.** `git diff main...HEAD` shows no password, key, or token. `appsettings.json` still holds the
   password-less connection template. `05-security-reviewer` signs this off.

6. **Docs updated in the same PR** when the change affects them: `README.md` (setup/prereqs/troubleshooting),
   `CLAUDE.md` (stack summary, commands), `docs/tech-stack.md` (versions), `docs/domain-model.md` (entity or
   endpoint shape), `docs/architecture.md` (new ADR entry),
   [../known-issues.md](../known-issues.md) — entries fixed must be deleted, new findings added — and
   [../decisions-log.md](../decisions-log.md) when the PR contains a decision worth remembering.

7. **Reviews recorded.** `04-code-reviewer` always; `05-security-reviewer` when the trigger list in
   [../agents/05-security-reviewer.md](../agents/05-security-reviewer.md) applies.

## PR description template

```md
## What
One or two sentences.

## Why
Link to docs/specs/<NNN>-<name>.md, or the bug report.

## Layers touched
Domain / Application / Infrastructure / Api / Frontend / Docs

## Migration
None | <MigrationName> — describe the schema change and whether it is destructive

## Contract impact
None | RecipeDto changed: <fields> — frontend updated in this PR (08-api-contract)

## Verification
- dotnet build: <N> warnings (currently 7, target 0)
- dotnet test: <N>/<N> passing (currently 78)
- npm run build + npx tsc --noEmit: pass | n/a
- Manual check against a real PostgreSQL: <what you did> | n/a

## Known issues / roadmap
Fixes:     <IDs from docs/known-issues.md or docs/roadmap.md, deleted from those files in this PR>
Adds:      <new IDs recorded in docs/known-issues.md>
Deploy gate: <items closed, if any>

## Follow-ups
Anything deliberately left out of scope.
```

## Deploying

Only the API has a build artifact path today: `RecipeManager.Api/Dockerfile` (base images
`mcr.microsoft.com/dotnet/aspnet:10.0` and `sdk:10.0`). Build from `RecipeManager/` so the `COPY` paths resolve:

```bash
docker build -f RecipeManager.Api/Dockerfile -t recipemanager-api .
```

- The image contains **no database**. Supply a reachable PostgreSQL and pass the connection string via
  `ConnectionStrings__DefaultConnection`.
- `Host=localhost` inside the container resolves to the *container*, not the host — use `host.docker.internal`
  (Docker Desktop) or a compose service name. This is documented in `README.md`'s Docker section.
- Migrations apply on startup, so the deployed database is upgraded automatically on first boot of a new image.
  There is no rollback path other than a corrective migration.
- The `IntegrationTest` environment name **throws in RELEASE builds** by design (ADR-005) — never set
  `ASPNETCORE_ENVIRONMENT=IntegrationTest` in a deployment.

The frontend has **no documented deployment target** — `npm run build` produces `dist/`, but nothing describes
where it is hosted, and `.env.production` still points at a placeholder host. `INFRA-04` and `SEC-12` in
[../known-issues.md](../known-issues.md).

## Rollback

There is no documented procedure. With startup-applied migrations and no versioning, redeploying an older image
against an already-migrated database is not guaranteed to work. Decide the policy before the first production
deploy — `INFRA-03` in [../known-issues.md](../known-issues.md).

---

## Gaps to close (all currently manual)

| Gap | ID | Suggested owner |
| --- | --- | --- |
| No CI running build / test / typecheck / lint on PRs | `INFRA-01` | `01-architect` |
| No versioning or tags | `INFRA-02` | `01-architect` |
| No rollback or database-restore procedure | `INFRA-03` | `01-architect` |
| No frontend deployment target or production `VITE_API_URL` | `INFRA-04`, `SEC-12` | `03-senior-react` + `01-architect` |
| No frontend test runner, so nothing to gate on | `TEST-01` | `06-qa-tester` |
| `npm run lint` cannot run; `npm run build` does not type-check | `BUILD-03`, `BUILD-04` | `03-senior-react` |

Full detail for each: [../known-issues.md](../known-issues.md).
