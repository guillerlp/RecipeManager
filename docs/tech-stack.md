# Tech stack

Backend versions are all declared in `RecipeManager/Directory.Packages.props` (central package management,
ADR-011) — that file is the single source of truth, not the `.csproj` files. SDK from `global.json`, frontend
from `recipe-manager-frontend/package.json`.

## Runtimes

| Runtime | Version | Pinned in |
| --- | --- | --- |
| .NET | `net10.0` (all six projects) | `Directory.Build.props` `<TargetFramework>` (ADR-010) |
| .NET SDK | `10.0.302`, `rollForward: latestFeature` | `RecipeManager/global.json` |
| Node.js | `24` used, `^20.19.0 \|\| >=22.12.0` supported | `recipe-manager-frontend/.nvmrc`, `engines` in `package.json` |
| PostgreSQL | 16+ recommended by `README.md` | not enforced anywhere |
| React | 19.3 | `package.json` |
| TypeScript | 7.0 | `package.json` |

Both runtimes are now pinned (`BUILD-07` closed by `R-04`/ADR-013). `.nvmrc` says `24` — what is actually used,
and what CI installs via `node-version-file` — while `engines` says `^20.19.0 || >=22.12.0`, the floor `README.md`
supports. The two differ deliberately: one is the tested version, the other is the compatibility claim. The floor
is not a guess: it is copied from Vite 8's and `@vitejs/plugin-react` 6's own `engines` (ADR-015), because a
looser claim (`>=20`, as it was) promises Node versions the build tool refuses to run on.

The NuGet **transitive closure** is pinned too: `RestorePackagesWithLockFile` is on for every project and each
has a committed `packages.lock.json`. CI restores with `--locked-mode`, so an unexpected graph change is a
failure (`NU1004`) rather than a silent resolution. Changing any package means re-running
`dotnet restore RecipeManager.sln --force-evaluate` and committing the result.

## Backend packages

| Package | Version | Used in | Why / where it shows up |
| --- | --- | --- | --- |
| `Microsoft.EntityFrameworkCore` (+ `.Relational`, `.Design`) | 10.0.12 | Infrastructure, Api | `AppDbContext`, migrations |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | Infrastructure, Api | `options.UseNpgsql(...)`; maps `IReadOnlyList<string>` to a native `text[]` column with no configuration |
| `FluentResults` | 4.0.0 | Domain, Application | Expected-failure channel (ADR-002). `Error` metadata carries `ErrorCode` + `field`. Ships no `net10.0` asset — the `net9.0` build is consumed. `Errors` is `IReadOnlyList<IError>`, and `Result.Fail` with an empty error collection throws |
| `FluentValidation` | 11.12.0 | Application, Api | Payload-shape validation |
| `FluentValidation.AspNetCore` | 11.3.1 | Api | `AddFluentValidationAutoValidation()` — validates the bound request type before the action runs |
| `Scrutor` | 7.0.0 | Api | `services.Scan(...)` registers every `ICommandHandler<,>` / `IQueryHandler<,>` from the Application assembly (ADR-008), and `services.Decorate<IRecipeRepository, CachedRecipeRepository>()` adds the caching decorator |
| `Swashbuckle.AspNetCore` | 10.2.3 | Api | Swagger UI, Development environment only. Note the v10 namespace: `using Microsoft.OpenApi;` (not `Microsoft.OpenApi.Models`) |
| `Microsoft.Extensions.DependencyInjection` | 10.0.12 | Application | `GetRequiredService` inside the dispatchers |
| `Ardalis.GuardClauses` | 5.0.0 | Domain | **Referenced but never used** — no `Guard.` call exists in the codebase (`DEC-03` in [known-issues.md](known-issues.md)) |
| `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` | 1.23.0 | Api | Visual Studio Docker tooling for the `Container (Dockerfile)` launch profile |

`Microsoft.EntityFrameworkCore.SqlServer` was **removed** in the PostgreSQL migration — do not reintroduce it.

## Test packages

| Package | Version | Project |
| --- | --- | --- |
| `xunit` | 2.9.3 | Unit + Integration |
| `xunit.runner.visualstudio` | 4.0.0 | Unit + Integration |
| `Microsoft.NET.Test.Sdk` | 18.10.1 | Unit + Integration |
| `FluentAssertions` | 8.11.0 | Unit + Integration |
| `NSubstitute` | 6.2.0 | Unit only — **the mocking library here is NSubstitute, not Moq** |
| `coverlet.collector` / `coverlet.msbuild` | 10.0.1 | Unit (both), Integration (collector only) |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.12 | Integration — `WebApplicationFactory<Program>` |
| `Microsoft.EntityFrameworkCore.InMemory` | 10.0.12 | Integration — test database |

Both test projects set `<Using Include="Xunit" />`, so `using Xunit;` is implicit.

## Frontend packages

| Package | Version | Role |
| --- | --- | --- |
| `react` / `react-dom` | 19.3 | UI |
| `vite` | 8.3 | dev server + build; port **3000**, `/api` → `https://localhost:7231` proxy with `secure: false`. Bundles with Rolldown, transforms with Oxc, minifies CSS with Lightning CSS (ADR-015) |
| `@vitejs/plugin-react` | 6.1 | Fast Refresh via Oxc — no Babel. Its `babel` option no longer exists; Babel plugins would need `@rolldown/plugin-babel` |
| `typescript` | 7.0.2 (exact) | the Go-native compiler: a `tsc` binary, **no JavaScript API**. `strict`, `noUnusedLocals`, `noUnusedParameters`, `noFallthroughCasesInSwitch` all on (ADR-016) |
| `@tanstack/react-query` | 5.102 | server state (`useRecipes`); client configured in `main.tsx` |
| `@tanstack/react-query-devtools` | 5.102 | mounted when `import.meta.env.DEV` (Vite's own flag — browser code does not rely on Node's `process` types) |
| `axios` | 1.20 | single `AxiosInstance` in `services/recipeService.ts` |
| `react-router-dom` | 7.18 | `BrowserRouter` + 3 routes in `App.tsx` |
| `oxlint` | 1.82.0 (exact) | the linter. `.oxlintrc.json` holds the 71 type-aware rules ESLint + typescript-eslint enforced before ADR-016, scoped to `src/**` (plus `no-console`), and turns on Oxlint's `correctness` category for every file so root tooling files are checked too — 159 rules in all (`BUILD-10`) |
| `oxlint-tsgolint` | 7.0.2001 (exact) | Oxlint's type-aware backend. Embeds its own typescript-go and is versioned after it (7.0.2, patch 001) — keep it in step with `typescript`; the `typescript` Dependabot group moves all three together |

### npm scripts

| Script | Command | Reality |
| --- | --- | --- |
| `dev` | `vite` | works — port 3000 |
| `build` | `tsc -b tsconfig.json tsconfig.node.json && vite build` | type-checks first, so a type error fails before Vite bundles (ADR-012) |
| `typecheck` | `tsc -b tsconfig.json tsconfig.node.json` | the same check without bundling; 0 errors. `tsconfig.json` covers `src/` (browser, no Node types); `tsconfig.node.json` covers `vite.config.ts` (Node types) — kept apart so `process` can never creep back into browser code |
| `lint` | `oxlint` | type-aware (`options.typeAware` in `.oxlintrc.json`), 0 problems (ADR-016) |
| `preview` | `vite preview` | works |

There is still no `test` script — no frontend test runner exists (`TEST-01`, planned as `R-07`). And note what
the three working scripts do *not* give you on their own — but CI now runs all of them on every PR
(`R-04`/ADR-013), pinned to the Node version in `.nvmrc`.

`npm audit` reports **0 vulnerabilities** as of 2026-08-08, and the NuGet side is clean by both
`dotnet list package --vulnerable --include-transitive` and Dependabot. The 13 npm advisories previously tracked
as `SEC-03` were all resolved by `npm audit fix` **without changing `package.json`** — every fix was already
inside the declared semver ranges, so only `package-lock.json` moved.

The lesson worth carrying: a version range in `package.json` tells you what is *permitted*, not what is
*installed*. The lock file is the only statement of the latter, and it had drifted a long way behind. Nothing
re-resolves it on your behalf — which is why `R-04` runs `npm audit` on every PR, and why the same reasoning
produced committed NuGet lock files in that item.

### Styling

CSS Modules (`*.module.css`) co-located with each component, plus four global sheets imported in `main.tsx`:
`styles/themes/variables.css` (spacing/radius/font-size/transition tokens), `themes/light.css`,
`themes/dark.css` (selected via `data-theme` on `<html>`), and `styles/globals.css`.

There is **no component library** (ADR-014 removed MUI and Emotion). The four icons live in
`recipe-manager-frontend/src/components/ui/Icon/` as plain SVG using Material Icons path data (Apache-2.0); add
new ones there the same way rather than installing an icon package for a handful of glyphs.

### Environment variables

`.env.development` sets `VITE_API_URL=https://localhost:7231/api`. `recipeService.ts` strips trailing slashes
and falls back to the relative `/api` (proxied by Vite) when the variable is absent.

`.env.production` still holds the placeholder `https://your-production-api.com/api` — `SEC-12` in
[known-issues.md](known-issues.md).

### Path aliases

Declared **twice** and must be kept in sync — `vite.config.ts` `resolve.alias` and `tsconfig.json`
`compilerOptions.paths`: `@`, `@components`, `@contexts`, `@pages`, `@hooks`, `@services`, `@types`, `@styles`.
The `paths` targets start with `./` and there is **no `baseUrl`**: TypeScript 6 deprecates `baseUrl` and 7
(and Oxlint's type-aware mode) drops it, so `paths` resolve relative to `tsconfig.json` itself. Do not re-add it.

## Notable absences

Stated explicitly so agents do not assume they exist:

- **No frontend test runner.** No Vitest/Jest, no React Testing Library, no `test` script in `package.json`.
- **No deployment pipeline.** `.github/workflows/ci.yml` verifies every PR (ADR-013) but builds no artifact and
  deploys nothing (`INFRA-04`).
- **No AutoMapper / MediatR / Serilog / Polly.**
- **No authentication or authorization packages** — no `Microsoft.AspNetCore.Authentication.*`, no Identity.
- **No rate limiting** (`AddRateLimiter` is not called).
- **No API versioning** (`Asp.Versioning.*` not referenced).
- **No health checks** (`AddHealthChecks` is not called).
- **No docker-compose** — `RecipeManager.Api/Dockerfile` builds the API alone (base images
  `mcr.microsoft.com/dotnet/aspnet:10.0` and `sdk:10.0`) and needs an externally reachable PostgreSQL.
