# RecipeManager

Clean-architecture recipe API (ASP.NET Core 8 + EF Core + SQL Server) with a React 19 / Vite frontend.

```
RecipeManager/
  RecipeManager.Domain/            entities, guard clauses
  RecipeManager.Application/       use cases, validators (FluentValidation/FluentResults)
  RecipeManager.Infrastructure/    EF Core DbContext, migrations
  RecipeManager.Api/               controllers, DI/startup, Swagger
  RecipeManager.UnitTests/         xUnit + NSubstitute
  RecipeManager.IntegrationTests/  xUnit + WebApplicationFactory (EF InMemory)
  recipe-manager-frontend/         React 19 + Vite + MUI
```

## Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| .NET SDK | **10.0** | All projects target `net10.0`. Pinned in `RecipeManager/global.json` with `rollForward: latestFeature`. |
| PostgreSQL | 16 or newer | Accessed via Npgsql. Default host/port `localhost:5432`. |
| Node.js | 20 LTS or newer | Only needed for the frontend. |

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
