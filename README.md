# RecipeManager

Clean-architecture recipe API (ASP.NET Core 10 + EF Core + PostgreSQL) with a React 19 / Vite frontend.

```
RecipeManager/
  RecipeManager.Domain/            entities, guard clauses
  RecipeManager.Application/       use cases, validators (FluentValidation/FluentResults)
  RecipeManager.Infrastructure/    EF Core DbContext, migrations
  RecipeManager.Api/               controllers, DI/startup, Swagger
  RecipeManager.UnitTests/         xUnit + NSubstitute
  RecipeManager.IntegrationTests/  xUnit + WebApplicationFactory (PostgreSQL via Testcontainers)
  recipe-manager-frontend/         React 19 + Vite + CSS Modules
```

## Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| .NET SDK | **10.0** | All projects target `net10.0`. Pinned in `RecipeManager/global.json` with `rollForward: latestFeature`. |
| PostgreSQL | 16 or newer | Accessed via Npgsql. Default host/port `localhost:5432`. |
| Docker | any current version | Only needed for the **integration tests**, which start a real PostgreSQL container (ADR-017). Without it they are reported as skipped and everything else still runs. |
| Node.js | 20.19+ or 22.12+; **24 recommended** | Only needed for the frontend. `engines` declares `^20.19.0 \|\| >=22.12.0`, the floor Vite 8 itself requires; `recipe-manager-frontend/.nvmrc` pins **24**, which is what CI installs and what the project is tested on. `nvm use` in that folder picks it up. |

Install on Windows:

```bash
winget install Microsoft.DotNet.SDK.10
```

```bash
winget install PostgreSQL.PostgreSQL.18 --interactive
```

```bash
winget install OpenJS.NodeJS.LTS
```

Open a new terminal afterwards so `PATH` picks up the new tools.

## Backend

Run everything below from the `RecipeManager/` folder (the one holding `RecipeManager.sln`).

```bash
dotnet restore RecipeManager.sln
```

```bash
dotnet build RecipeManager.sln
```

Code style is part of the build (ADR-020). The root `.editorconfig` makes formatting (`IDE0055`), unused usings
(`IDE0005`), and block-scoped namespaces (`IDE0161`) build **errors**. When one fires, let the tool fix it:

```bash
dotnet format RecipeManager.sln
```

Run this once per clone so `git blame` skips the one-off reformat commit and shows who really wrote each line
(GitHub's blame view does this automatically):

```bash
git config blame.ignoreRevsFile .git-blame-ignore-revs
```

### Database

`Program.cs` applies EF migrations at startup (`app.MigrateDatabase()`), so the API creates and updates
the schema on first run — but it will **fail to start** if PostgreSQL is not reachable.

Create the role and database once (run from an elevated-enough shell; you'll be prompted for the
`postgres` superuser password set during install):

```bash
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -c "CREATE ROLE recipemanager LOGIN PASSWORD 'your-password';"
```

```bash
& "C:\Program Files\PostgreSQL\18\bin\createdb.exe" -U postgres -O recipemanager DbRecipeManager
```

`RecipeManager.Api/appsettings.json` holds a **password-less** template:

```
Host=localhost;Port=5432;Database=DbRecipeManager;Username=recipemanager;Timeout=90
```

Supply the password locally via user-secrets so it never reaches git (the API project already has a
`UserSecretsId`):

```bash
dotnet user-secrets --project RecipeManager.Api set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=DbRecipeManager;Username=recipemanager;Password=your-password"
```

In deployment, override the same key with the `ConnectionStrings__DefaultConnection` environment variable.

To apply migrations manually instead of at startup:

```bash
dotnet tool install --global dotnet-ef
```

```bash
dotnet ef database update --project RecipeManager.Infrastructure --startup-project RecipeManager.Api
```

### Run the API

```bash
dotnet run --project RecipeManager.Api --launch-profile https
```

- HTTPS: `https://localhost:7231` (Swagger UI at `/swagger`)
- HTTP: `http://localhost:5249`

Trust the local HTTPS certificate once, otherwise the browser and the Vite proxy will reject it:

```bash
dotnet dev-certs https --trust
```

### Tests

```bash
dotnet test RecipeManager.sln
```

190 tests: 141 unit and 49 integration. Of the 49, **45** start a real PostgreSQL container (ADR-017) and the
other 4 (`OpenApiContractTests`, ADR-019) need no database at all. **With Docker running** you get 190 passed;
**without it** you get 145 passed and 45 skipped, each naming Docker as the reason. The skip is deliberate — see
the troubleshooting entry below — but it means a green run is only as complete as its skip count says. On a
Windows machine with Smart App Control enabled, the 4 contract tests do not skip — they **fail** with
`FileLoadException`, the same way the 45 integration tests do; see the Smart App Control entry below.

These counts were taken **locally** on 2026-09-26, on a machine with no Docker: 145 passed and 45 skipped. The
"with Docker" figure is the expected total, **not yet confirmed by CI**; the last CI-confirmed run
(36051107842) predates `R-17` and counted 138. Treat CI as the authority once it has run.

Unit tests with an HTML coverage report (requires `dotnet tool install --global dotnet-reportgenerator-globaltool`):

```bash
pwsh ./run-coverage.ps1
```

## Frontend

```bash
cd recipe-manager-frontend
```

```bash
npm install
```

```bash
npm run dev
```

Serves on `http://localhost:3000` — the origin the API's CORS policy (`AllowReactApp`) allows, so don't
change the port without updating `RecipeManager.Api/Startup/ServiceInitializer.cs`.

`.env.development` points `VITE_API_URL` at `https://localhost:7231/api`. Delete or blank that variable to
fall back to the relative `/api` path, which `vite.config.ts` proxies to the same backend.

Start the API first — the frontend has no mock backend.

### Frontend checks

CI runs these on every pull request (see [Continuous integration](#continuous-integration)); run them locally
first anyway, since a failure found in seconds beats one found on a runner.

```bash
npm run lint
```

```bash
npm run build
```

`npm run build` type-checks `src/` and `vite.config.ts` (`tsc -b tsconfig.json tsconfig.node.json`) before
Vite bundles anything, so a type error fails it. For a faster
loop while working, `npm run typecheck` runs the same check without producing `dist/`. `npm test` runs the
Vitest suite once; `npm run test:watch` re-runs on save.

### Changing the API contract

The TypeScript types in `src/types/generated/api.ts` are generated from `RecipeManager/contracts/openapi.json`,
which is itself a snapshot of the API's OpenAPI document (ADR-019). After changing a DTO, a route, or a status
code, regenerate both, from `RecipeManager/`:

```bash
UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --filter OpenApiContractTests
```

then from `RecipeManager/recipe-manager-frontend/`:

```bash
npm run gen:api
```

and commit both files. CI fails if either is stale. On PowerShell, set the variable with
`$env:UPDATE_OPENAPI_SNAPSHOT='1'` and remove it afterwards.

If the snapshot test cannot run on your machine at all — Windows with Smart App Control enabled blocks it the
same way it blocks the integration tests, see [Troubleshooting](#troubleshooting) — there is a second route that
needs no local test run:

1. Push the change and let CI's **Backend** job fail on `OpenApiContractTests`.
2. Download the `openapi-received` artifact it uploads on that failure:
   ```bash
   gh run download <run-id> -n openapi-received
   ```
3. Copy the downloaded `openapi.received.json` over `RecipeManager/contracts/openapi.json`.
4. Run `npm run gen:api` from `RecipeManager/recipe-manager-frontend/` as above, and commit both files.

Either route ends the same way: `contracts/openapi.json` and `src/types/generated/api.ts` committed together.
`src/types/recipe.ts` only aliases the generated schemas — never add a field there by hand.

## Continuous integration

`.github/workflows/ci.yml` runs on every pull request to `main` and every push to `main`, in two parallel jobs
on `ubuntu-latest`:

| Job | Steps |
| --- | --- |
| **Backend** | `dotnet restore --locked-mode` → `dotnet build` (Debug) → `dotnet test` (190) → upload `openapi-received` snapshot on failure → vulnerable-package check |
| **Frontend** | `npm ci` → contract types are current (`npm run gen:api` + diff check) → `npm run typecheck` → `npm run lint` → `npm test` → `npm run build` → `npm audit --audit-level=high` → `npm audit --audit-level=high --prefix ../contracts` |

Two things are worth knowing before a run surprises you:

- **NuGet restores from committed lock files.** Every project has a `packages.lock.json`, and CI restores in
  locked mode. Change a package version and CI fails with `NU1004` until you run
  `dotnet restore RecipeManager.sln --force-evaluate` and commit the updated lock files.
- **CI builds Debug on purpose.** A Release build makes every integration test fail: the `IntegrationTest`
  environment throws in RELEASE builds by design, and `WebApplicationFactory` uses that environment name. See
  ADR-005 and ADR-013.

The checks are not yet *required* to merge — that is a branch-protection setting on `main`.

## Docker

`RecipeManager.Api/Dockerfile` builds the API alone (no database container). Build from the `RecipeManager/`
folder so the `COPY` paths resolve:

```bash
docker build -f RecipeManager.Api/Dockerfile -t recipemanager-api .
```

The image needs a reachable PostgreSQL; pass the connection string via
`ConnectionStrings__DefaultConnection`. Note that `Host=localhost` resolves to the *container*, not your
machine — use `host.docker.internal` (Docker Desktop) or a compose service name instead.

## Troubleshooting

**`You must install or update .NET to run this application` / `Framework 'Microsoft.NETCore.App', version 'X' not found`**
The solution built, but the matching runtime is missing. Install the .NET 10 SDK (above); `dotnet --list-runtimes`
should show a `10.x` entry for both `Microsoft.NETCore.App` and `Microsoft.AspNetCore.App`.

**API throws on startup with a Npgsql connection or authentication error**
PostgreSQL is not running, or the credentials are wrong. Check the service with
`Get-Service postgresql*` in PowerShell, and confirm the password is set in user-secrets — the template in
`appsettings.json` deliberately has none.

**`relation "recipes" does not exist` when querying in psql**
PostgreSQL folds unquoted identifiers to lowercase, and EF creates the tables as `"Recipes"` and
`"RecipeIngredients"`. Quote them: `SELECT * FROM "Recipes";`,
`SELECT * FROM "RecipeIngredients" ORDER BY "Position";`

**Frontend requests fail with a certificate error**
Run `dotnet dev-certs https --trust`.

**The 29 integration tests are reported as skipped**
Docker is not running or not installed. The integration tests start a PostgreSQL container (ADR-017), and
without a Docker endpoint they skip rather than fail, so the unit tests still give a usable result. The skip
message names the endpoint it tried, e.g. `npipe://./pipe/docker_engine` on Windows. Start Docker Desktop and
re-run, or rely on CI, which always has Docker.

**`dotnet run` fails with "An Application Control policy has blocked this file"**
A Windows Application Control policy (Smart App Control is a common source) refused to start
`RecipeManager.Api\bin\Debug\net10.0\RecipeManager.Api.exe` — the unsigned launcher the SDK generates for every
build. The code is fine. Run through the signed `dotnet` host instead, which skips generating that `.exe`:

```bash
dotnet run --project RecipeManager.Api --launch-profile https -p:UseAppHost=false
```

Changing the Application Control policy also works, but it is a machine-wide security setting — prefer the flag.

The same policy can also block the **integration tests**: every one fails with
`FileLoadException … An Application Control policy has blocked this file. (0x800711C7)` on
`RecipeManager.IntegrationTests\bin\Debug\net10.0\RecipeManager.Api.dll`. Here the flag does not help — the
blocked file is the DLL, not the launcher. This includes `OpenApiContractTests` (ADR-019): those 4 tests need no
Docker, but they still boot the API, so they **fail** here rather than skip. The unit tests still run; for the
integration tests and the contract tests, rely on CI, which runs all of them on Linux for every PR. If you need
to accept a contract change from a machine in this state, see "Changing the API contract" above — it has a route
that needs no local test run.

It is **not** limited to those two cases. The policy can block any freshly-built assembly, intermittently and
without a pattern worth predicting — `dotnet ef` has been blocked mid-run while scaffolding a migration, which
leaves no migration file rather than a broken one. There is no fix short of turning Smart App Control off, and
Windows makes that irreversible: once off, it cannot be switched back on without reinstalling the OS. Declining
that trade is reasonable. The consequence to plan around is that **CI is the authority** — quote its numbers,
and treat a local `FileLoadException` as an environment fact rather than a defect.
