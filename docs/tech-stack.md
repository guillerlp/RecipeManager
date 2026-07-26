# Tech stack

Versions read from `*.csproj`, `global.json`, and `recipe-manager-frontend/package.json` on `main` @ `edfd057`.

## Runtimes

| Runtime | Version | Pinned in |
| --- | --- | --- |
| .NET | `net10.0` (all six projects) | each `.csproj` `<TargetFramework>` |
| .NET SDK | `10.0.302`, `rollForward: latestFeature` | `RecipeManager/global.json` |
| Node.js | not pinned | no `engines` field, no `.nvmrc` |
| PostgreSQL | 16+ recommended by `README.md` | not enforced anywhere |
| React | 19.1 | `package.json` |
| TypeScript | 5.9 | `package.json` |

The .NET SDK is pinned but Node is not (`BUILD-07` in [known-issues.md](known-issues.md)). Verified working
with Node 24.18 / npm 11.16; `README.md` asks for 20 LTS or newer.

## Backend packages

| Package | Version | Used in | Why / where it shows up |
| --- | --- | --- | --- |
| `Microsoft.EntityFrameworkCore` (+ `.Relational`, `.Design`) | 10.0.10 | Infrastructure, Api | `AppDbContext`, migrations |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | Infrastructure, Api | `options.UseNpgsql(...)`; maps `IReadOnlyList<string>` to a native `text[]` column with no configuration |
| `FluentResults` | 3.16.0 | Domain, Application | Expected-failure channel (ADR-002). `Error` metadata carries `ErrorCode` + `field` |
| `FluentValidation` | 11.11.0 | Application, Api | Payload-shape validation |
| `FluentValidation.AspNetCore` | 11.3.1 | Api | `AddFluentValidationAutoValidation()` — validates the bound request type before the action runs |
| `Scrutor` | 7.0.0 | Api | Only for `services.Decorate<IRecipeRepository, CachedRecipeRepository>()`. Assembly scanning is **not** used — handlers are registered explicitly |
| `Swashbuckle.AspNetCore` | 10.2.3 | Api | Swagger UI, Development environment only. Note the v10 namespace: `using Microsoft.OpenApi;` (not `Microsoft.OpenApi.Models`) |
| `Microsoft.Extensions.DependencyInjection` | 10.0.10 | Application | `GetRequiredService` inside the dispatchers |
| `Ardalis.GuardClauses` | 5.0.0 | Domain | **Referenced but never used** — no `Guard.` call exists in the codebase (`DEC-03` in [known-issues.md](known-issues.md)) |
| `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` | 1.21.0 | Api | Visual Studio Docker tooling for the `Container (Dockerfile)` launch profile |

`Microsoft.EntityFrameworkCore.SqlServer` was **removed** in the PostgreSQL migration — do not reintroduce it.

## Test packages

| Package | Version | Project |
| --- | --- | --- |
| `xunit` | 2.9.3 | Unit + Integration |
| `xunit.runner.visualstudio` | 3.1.5 | Unit + Integration |
| `Microsoft.NET.Test.Sdk` | 18.8.1 | Unit + Integration |
| `FluentAssertions` | 8.10.0 | Unit + Integration |
| `NSubstitute` | 6.0.0 | Unit only — **the mocking library here is NSubstitute, not Moq** |
| `coverlet.collector` / `coverlet.msbuild` | 10.0.1 | Unit (both), Integration (collector only) |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.10 | Integration — `WebApplicationFactory<Program>` |
| `Microsoft.EntityFrameworkCore.InMemory` | 10.0.10 | Integration — test database |

Both test projects set `<Using Include="Xunit" />`, so `using Xunit;` is implicit.

## Frontend packages

| Package | Version | Role |
| --- | --- | --- |
| `react` / `react-dom` | 19.1 | UI |
| `vite` | 7.0 | dev server + build; port **3000**, `/api` → `https://localhost:7231` proxy with `secure: false` |
| `@vitejs/plugin-react` | 4.6 | Babel-based Fast Refresh |
| `typescript` | 5.9 | `strict`, `noUnusedLocals`, `noUnusedParameters`, `noFallthroughCasesInSwitch` all on |
| `@mui/material` + `@mui/icons-material` | 7.x | used sparingly: `Box` for layout, icons (`Search`, `Sunny`, `Bedtime`, `BlenderOutlined`). **No MUI ThemeProvider** — visual styling is CSS Modules |
| `@emotion/react` / `@emotion/styled` | 11.x | MUI peer dependency only; no direct `styled` usage in `src/` |
| `@tanstack/react-query` | 5.85 | server state (`useRecipes`); client configured in `main.tsx` |
| `@tanstack/react-query-devtools` | 5.85 | mounted when `process.env.NODE_ENV === 'development'` |
| `axios` | 1.10 | single `AxiosInstance` in `services/recipeService.ts` |
| `react-router-dom` | 7.7 | `BrowserRouter` + 3 routes in `App.tsx` |
| `eslint` 9 + `typescript-eslint` 8.39 | | flat config, `recommendedTypeChecked` + `stylisticTypeChecked` |

### npm scripts — two are broken or misleading

| Script | Command | Reality |
| --- | --- | --- |
| `dev` | `vite` | works — port 3000 |
| `build` | `vite build` | succeeds, but **does not type-check** (esbuild strips types without checking). `BUILD-04` |
| `lint` | `eslint .` | **fails to start** on a clean install — `eslint.config.ts` needs `jiti`, which is not a dependency. `BUILD-03` |
| `preview` | `vite preview` | works |

There is no `typecheck` script and no `test` script. Run `npx tsc --noEmit` manually until `BUILD-04` is fixed —
it currently passes with 0 errors. See [known-issues.md](known-issues.md).

`npm audit` reports **17 vulnerabilities** (12 high), with `axios`, `react-router-dom`, and `vite` directly
affected — `SEC-03`. The NuGet side is clean.

### Styling

CSS Modules (`*.module.css`) co-located with each component, plus four global sheets imported in `main.tsx`:
`styles/themes/variables.css` (spacing/radius/font-size/transition tokens), `themes/light.css`,
`themes/dark.css` (selected via `data-theme` on `<html>`), and `styles/globals.css`.

### Environment variables

`.env.development` sets `VITE_API_URL=https://localhost:7231/api`. `recipeService.ts` strips trailing slashes
and falls back to the relative `/api` (proxied by Vite) when the variable is absent.

`.env.production` still holds the placeholder `https://your-production-api.com/api` — `SEC-12` in
[known-issues.md](known-issues.md).

### Path aliases

Declared **twice** and must be kept in sync — `vite.config.ts` `resolve.alias` and `tsconfig.json`
`compilerOptions.paths`: `@`, `@components`, `@contexts`, `@pages`, `@hooks`, `@services`, `@types`, `@styles`.

## Notable absences

Stated explicitly so agents do not assume they exist:

- **No frontend test runner.** No Vitest/Jest, no React Testing Library, no `test` script in `package.json`.
- **No CI.** No `.github/` directory, no pipeline configuration anywhere in the repo.
- **No AutoMapper / MediatR / Serilog / Polly.**
- **No authentication or authorization packages** — no `Microsoft.AspNetCore.Authentication.*`, no Identity.
- **No rate limiting** (`AddRateLimiter` is not called).
- **No API versioning** (`Asp.Versioning.*` not referenced).
- **No health checks** (`AddHealthChecks` is not called).
- **No docker-compose** — `RecipeManager.Api/Dockerfile` builds the API alone (base images
  `mcr.microsoft.com/dotnet/aspnet:10.0` and `sdk:10.0`) and needs an externally reachable PostgreSQL.
