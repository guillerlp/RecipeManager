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

103 tests: 85 unit and 18 integration. The integration tests start a real PostgreSQL container (ADR-017), so
**with Docker running** you get 103 passed; **without it** you get 85 passed and 18 skipped, each naming Docker
as the reason. The skip is deliberate — see the troubleshooting entry below — but it means a green run is only
as complete as its skip count says.

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

## Continuous integration

`.github/workflows/ci.yml` runs on every pull request to `main` and every push to `main`, in two parallel jobs
on `ubuntu-latest`:

| Job | Steps |
| --- | --- |
| **Backend** | `dotnet restore --locked-mode` → `dotnet build` (Debug) → `dotnet test` (103) → vulnerable-package check |
| **Frontend** | `npm ci` → `npm run typecheck` → `npm run lint` → `npm test` → `npm run build` → `npm audit --audit-level=high` |

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
PostgreSQL folds unquoted identifiers to lowercase, and EF creates the table as `"Recipes"`. Quote it:
`SELECT * FROM "Recipes";`

**Frontend requests fail with a certificate error**
Run `dotnet dev-certs https --trust`.

**The 18 integration tests are reported as skipped**
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
blocked file is the DLL, not the launcher. The unit tests still run; for the integration tests, rely on CI, which
runs all of them on Linux for every PR.
