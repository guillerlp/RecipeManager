# RecipeManager — AI Agent Context

Recipe Manager is a personal recipe catalogue: a Clean-Architecture ASP.NET Core REST API over a single
`Recipe` aggregate, with a React 19 SPA for browsing and searching recipes.

> **Language rule:** all code, identifiers, comments, commit messages, and documentation are written in
> **English**. Conversation with the user may be in Spanish; artifacts are not.

## Project stance — read this before anything else

RecipeManager is a **practice project with deployment intent**. It began as a place to work through
architecture and tooling patterns properly — Clean Architecture, CQRS, a caching decorator, `Result`-based
error handling, EF migrations, .NET 10 + PostgreSQL — and may be deployed for real if it becomes something
worth deploying.

**The owner is learning while building this.** That makes explanation part of the deliverable, not a courtesy:
every agent must show its reasoning, name the alternatives it rejected, and state what the choice costs. A
correct change delivered without that reasoning is an incomplete change. See
[docs/learning-mode.md](docs/learning-mode.md) for the format, the calibration (what deserves explaining and
what does not), and a map of the patterns this repo already demonstrates.

That has two further consequences for every agent:

1. **The bar is the production-grade version of each pattern**, not the smallest thing that compiles. Where the
   current code falls short of that bar, these documents describe the *right* approach and point at the gap.
   Practising a pattern badly is worse than not practising it.
2. **Deployment is a real possibility, so security and operational gaps are sequenced, never waived.** The
   [deploy gate](docs/roadmap.md#deploy-gate) lists what must be closed before the app is ever exposed
   publicly. Nothing on that list gets quietly downgraded because "it's only a practice project".

**These documents are prescriptive, not merely descriptive.** They record what the project should do. Where
that differs from what the code does today, the difference is stated explicitly and linked to
[docs/known-issues.md](docs/known-issues.md) (defects) or [docs/roadmap.md](docs/roadmap.md) (planned work).
Never document a target as though it were already reality, and never lower a documented standard to match
existing code — fix the code, or record the gap.

Verified against `main` @ `edfd057` (merge of `dotnet10-postgresql`) on 2026-07-26.

---

## Stack at a glance

| Layer | Technology |
| --- | --- |
| Backend | .NET 10 (`net10.0`), ASP.NET Core Web API, hand-rolled CQRS (no MediatR — deliberate, ADR-001) |
| Persistence | EF Core 10.0.10 + Npgsql 10.0.3, **PostgreSQL**, code-first migrations |
| Result/validation | FluentResults 3.16, FluentValidation 11.11 |
| DI helpers | Scrutor 7.0 (`Decorate` for the caching repository) |
| Caching | `IMemoryCache` behind `ICacheService`, decorator over `IRecipeRepository` |
| Tests | xUnit 2.9, NSubstitute 6.0, FluentAssertions 8.10, `WebApplicationFactory` + EF InMemory |
| Frontend | React 19, TypeScript 5.9, Vite 7, MUI 7, TanStack Query 5, Axios, React Router 7, CSS Modules |

Full detail and rationale: [docs/tech-stack.md](docs/tech-stack.md).

### Repository layout

```
/                                    git root
  README.md                          human setup guide — prerequisites, DB, Docker, troubleshooting
  CLAUDE.md                          this file
  docs/                              agent context (this doc set)
  RecipeManager/                     solution root — run all dotnet commands from here
    RecipeManager.sln
    global.json                      pins SDK 10.0.302, rollForward: latestFeature
    Directory.Build.props            TargetFramework/Nullable/ImplicitUsings + TreatWarningsAsErrors, all projects
    Directory.Packages.props         every package version (central package management) — never version a .csproj
    RecipeManager.Domain/            Recipe entity, Entity base, RecipeErrors, IRecipeRepository
    RecipeManager.Application/       Commands, Queries, Handlers, Dispatchers, DTOs, Validators, Mappings
    RecipeManager.Infrastructure/    AppDbContext, RecipeRepository, CachedRecipeRepository, MemoryCacheService, Migrations
    RecipeManager.Api/               RecipesController, Startup/*, Middlewares/*, Extensions/*
    RecipeManager.UnitTests/         70 tests — xUnit + NSubstitute (Domain + Application handlers)
    RecipeManager.IntegrationTests/  14 tests — xUnit + WebApplicationFactory (EF InMemory)
    recipe-manager-frontend/         React 19 + Vite SPA
    run-coverage.ps1                 unit-test coverage + HTML report
```

### Path convention in these documents

**Any path that points at something you could open is written relative to the solution folder
`RecipeManager/`** — the folder holding `RecipeManager.sln`, and the folder all `dotnet` commands run from.
Such paths always start with the project folder:

- `RecipeManager.Api/Startup/ServiceInitializer.cs` — **not** `Api/Startup/...` or `Startup/...`
- `RecipeManager.Domain/Entities/Recipe.cs` — **not** `Domain/Entities/...` or `Entities/...`
- `RecipeManager.UnitTests/Domain/Entities/RecipeTests.cs` — test files carry their test project's name
- `recipe-manager-frontend/src/types/recipe.ts` — frontend files carry the frontend folder
- `recipe-manager-frontend/src/hooks/` — directories too, when naming a location

Prepend `RecipeManager/` to resolve any of them from the git root.

The one exception, used deliberately: a bare folder name may stand for a **category** rather than a location,
where the surrounding section has already fixed the scope — "put presentational components in `components/ui/`",
or "folders are grouped by technical role (`Commands/`, `Handlers/`, `Validators/`)". If a reader could
reasonably try to `cd` into it, it needs the full prefix.

Markdown links between documents are ordinary relative links and resolve from their own file's location, as
usual.

---

## Running locally

**[README.md](README.md) is the authoritative setup guide** — prerequisites, PostgreSQL role/database creation,
Docker, and a troubleshooting section. Do not duplicate it here; update it when setup changes. The essentials:

All `dotnet` commands run from `RecipeManager/` (the folder holding `RecipeManager.sln`).

```bash
dotnet build RecipeManager.sln
```

```bash
dotnet test RecipeManager.sln
```

Current state: build succeeds with **0 warnings** and **84 tests pass** (70 unit + 14 integration).
`RecipeManager/Directory.Build.props` sets `TreatWarningsAsErrors` for every project (ADR-010), so a warning is
a **build failure**, not a note — and `TargetFramework`, `Nullable`, and `ImplicitUsings` live there too. Never
re-declare those in a `.csproj`.

Frontend checks work as of `R-03`/ADR-012: `npm run lint` runs and reports 0 problems, `npm run build` is
`tsc -b && vite build` so a type error fails it, and `npm run typecheck` exists for the fast local loop. What
they are now also *automatic*: `.github/workflows/ci.yml` runs all three on every PR (`R-04`/ADR-013). Run them
locally from `RecipeManager/recipe-manager-frontend/` anyway — a failure found in seconds beats one found on a
runner. Note the checks are not yet **required** to merge (`INFRA-07`), so a red run can still be merged past.

Unit-test coverage with an HTML report (needs `dotnet tool install --global dotnet-reportgenerator-globaltool`):

```bash
pwsh ./run-coverage.ps1
```

Run the API — Swagger UI at `/swagger`, Development environment only:

```bash
dotnet run --project RecipeManager.Api --launch-profile https
```

HTTPS `https://localhost:7231`, HTTP `http://localhost:5249`. Trust the dev certificate once or the Vite proxy
and the browser will reject the API: `dotnet dev-certs https --trust`.

Run the frontend from `RecipeManager/recipe-manager-frontend/`:

```bash
npm install && npm run dev
```

Serves on `http://localhost:3000` — the **only** origin allowed by the API's `AllowReactApp` CORS policy
(`RecipeManager.Api/Startup/ServiceInitializer.cs`). Changing the port requires changing that policy. Start the
API first; there is no mock backend.

### Database and seed data

- PostgreSQL. `RecipeManager.Api/appsettings.json` holds a **password-less template**:
  `Host=localhost;Port=5432;Database=DbRecipeManager;Username=recipemanager;Timeout=90`.
  Supply the password via user-secrets (the API project has `UserSecretsId 3fd489a5-…`) or the
  `ConnectionStrings__DefaultConnection` environment variable. **Never add a password to `appsettings.json`.**
- `Program.cs` calls `app.MigrateDatabase()` at startup, so pending EF migrations apply automatically. The API
  **fails to start** if PostgreSQL is unreachable or the `ConnectionStrings` section is missing.
- **There is no seed mechanism.** The database starts empty; create recipes via `POST /api/recipes` or Swagger.
  Integration tests seed through `IntegrationTestBase.SeedDatabase<T>(...)` against EF InMemory only
  (`INFRA-05`; a Development-only seeder is planned as `R-13`).
- PostgreSQL folds unquoted identifiers to lowercase while EF creates `"Recipes"` — quote it in psql:
  `SELECT * FROM "Recipes";`

---

## Index

### Core context

| Document | Read it when |
| --- | --- |
| [docs/architecture.md](docs/architecture.md) | Understanding layers, dependency rules, request flow, recorded decisions |
| [docs/tech-stack.md](docs/tech-stack.md) | Choosing/adding a package, checking versions |
| [docs/conventions.md](docs/conventions.md) | Writing any code in this repo |
| [docs/domain-model.md](docs/domain-model.md) | Touching `Recipe`, validation, or persistence — **mandatory for every agent** |
| [docs/known-issues.md](docs/known-issues.md) | Every known **defect** in what exists — check before starting anything, update when you fix something |
| [docs/roadmap.md](docs/roadmap.md) | **Planned work** that does not exist yet, phased, plus the deploy gate |
| [docs/learning-mode.md](docs/learning-mode.md) | How to explain decisions — **required reading for every agent**, applies to every response |
| [docs/decisions-log.md](docs/decisions-log.md) | Why past decisions were made and what they taught — read before re-opening one, append when taking a new one |

### Workflows

| Document | Read it when |
| --- | --- |
| [docs/workflows/feature-workflow.md](docs/workflows/feature-workflow.md) | Building a new end-to-end feature |
| [docs/workflows/bugfix-workflow.md](docs/workflows/bugfix-workflow.md) | Investigating and fixing a defect |
| [docs/workflows/release-workflow.md](docs/workflows/release-workflow.md) | Branching, merging, shipping |

### Agents

| Agent | Scope |
| --- | --- |
| [docs/agents/00-leader.md](docs/agents/00-leader.md) | Decomposes requests, assigns agents, arbitrates conflicts |
| [docs/agents/01-architect.md](docs/agents/01-architect.md) | Structural decisions, ADRs, layer/dependency impact |
| [docs/agents/02-senior-csharp.md](docs/agents/02-senior-csharp.md) | Domain/Application/Infrastructure/Api code and tests |
| [docs/agents/03-senior-react.md](docs/agents/03-senior-react.md) | SPA components, state, data fetching, styling |
| [docs/agents/04-code-reviewer.md](docs/agents/04-code-reviewer.md) | PR review, block vs. suggest criteria |
| [docs/agents/05-security-reviewer.md](docs/agents/05-security-reviewer.md) | OWASP checks, secrets, input handling, auth gaps |
| [docs/agents/06-qa-tester.md](docs/agents/06-qa-tester.md) | Test strategy, coverage, recipe-domain edge cases |
| [docs/agents/07-ux-ui.md](docs/agents/07-ux-ui.md) | New screens, design tokens, theming, accessibility |
| [docs/agents/08-api-contract.md](docs/agents/08-api-contract.md) | Keeps the TS `Recipe` type and C# `RecipeDto` in sync |

### Specs

- [docs/specs/_template.md](docs/specs/_template.md) — fill this in **before** implementing any non-trivial feature.

### Agent roster rationale

- **`07-ux-ui` kept** even though there is no separate designer: the frontend has a real token system
  (`recipe-manager-frontend/src/styles/themes/variables.css` + `light.css`/`dark.css`), a light/dark switch, and deliberate a11y work
  (`role="switch"`, `aria-checked`, `aria-current`, `<time dateTime>`), so screen work needs an owner.
- **`08-api-contract` added** (not in the standard roster) because the frontend and backend contracts have
  already drifted in a way that is verifiable in the code: `recipe-manager-frontend/src/types/recipe.ts` declares `id: number` while the
  API returns a `Guid`, and the TS type is missing `servings` and `instructions`. This seam needs an owner.
  See [docs/agents/08-api-contract.md](docs/agents/08-api-contract.md).
- **No DevOps/CI agent**, still: `.github/` now holds a CI workflow and a Dependabot config (`R-04`/ADR-013),
  but that is ~120 lines of YAML that verifies and deploys nothing, and there is no compose file or deployment
  target. Ownership stays with `01-architect` for structure and
  [docs/workflows/release-workflow.md](docs/workflows/release-workflow.md) for process. Revisit if a deployment
  pipeline is ever built — that is when the surface justifies an owner.
- **No DBA agent**: a single table, a single migration, no stored procedures, no indexes beyond the PK.
  EF-related concerns belong to `02-senior-csharp`, schema-shape decisions to `01-architect`.

---

## Global rules — every agent, every task

1. **Read before writing.** Before touching code, read your own file in `docs/agents/`,
   [docs/domain-model.md](docs/domain-model.md), and [docs/learning-mode.md](docs/learning-mode.md).
2. **Never commit secrets.** No connection-string passwords, API keys, or tokens in `appsettings*.json`,
   `.env*`, or source. Use `dotnet user-secrets` or environment variables. The committed connection string is a
   deliberately password-free template — keep it that way.
3. **Tests must pass and the build must stay warning-free before a PR.** `dotnet test RecipeManager.sln` from
   `RecipeManager/`. Warnings are errors (ADR-010), so a new one breaks the build rather than merely being a
   blocking review finding. Suppressing one to get moving needs a stated reason. There is no frontend
   test runner yet — see [docs/agents/06-qa-tester.md](docs/agents/06-qa-tester.md).
4. **Respect the dependency direction.** `Domain ← Application ← Infrastructure ← Api`. Domain references no
   project. Any deviation is an architecture decision, not an implementation detail.
5. **Business rules live in the domain.** `Recipe.Create` / `Recipe.Update` own the invariants; FluentValidation
   validators only guard payload shape (null, length, bounds). Never duplicate a rule in both places.
6. **Errors are `Result`, not exceptions.** Return `FluentResults.Result` with an error from `RecipeErrors`.
   Exceptions are for genuinely exceptional failures and are caught by `ErrorHandlerMiddleware`.
7. **English only** in code, comments, tests, and docs.
8. **No invented facts in docs.** If something cannot be verified in the code, say so plainly and add an entry
   to [docs/known-issues.md](docs/known-issues.md) instead of guessing. **Do not scatter inline TODO markers
   through the docs or the code** — every open item belongs in that one file, with an ID.
9. **Do not widen scope.** Fix or build what was asked; file anything else as a follow-up note in the PR
   description.
10. **`main` is protected by convention** — work on a feature branch and open a PR (see
    [docs/workflows/release-workflow.md](docs/workflows/release-workflow.md)).
11. **Keep docs in step with code.** A PR that changes the stack, the domain shape, or a recorded decision must
    update `README.md`, `docs/tech-stack.md`, `docs/domain-model.md`, or `docs/architecture.md` in the same PR.
12. **Close the loop on issues.** Fixing something listed in [docs/known-issues.md](docs/known-issues.md) means
    deleting its entry in the same PR; finding something new means adding an entry with the next free ID.
    Completing a [roadmap](docs/roadmap.md) item means deleting it and recording the ADR.
13. **Do not degrade a documented standard to match existing code.** If the code violates a convention, either
    fix it or record the gap — never edit the convention to make the violation legal.
14. **Explain your reasoning.** Every non-trivial decision ships with why it was made, what was rejected, and
    what it costs — per [docs/learning-mode.md](docs/learning-mode.md). Name the pattern so it can be looked up.
    Never invent a principled-sounding rationale after the fact: "consistency with the surrounding code" is an
    honest and acceptable reason.
15. **Record decisions worth remembering.** Append to [docs/decisions-log.md](docs/decisions-log.md) when a
    decision would be hard to justify in six months, when a bug taught something, or when a previous decision
    is reversed. Read it before re-opening a settled question — the reasoning is probably already there.
