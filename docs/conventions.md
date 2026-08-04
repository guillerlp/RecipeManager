# Conventions

**These are standards, not observations.** Most are already followed consistently; where the existing code
violates one, that is marked and linked to a defect ID — the code is wrong, not the convention. Never edit a
rule here to legalise existing code.

Legend: **⚠ Target** marks a rule that the current code does not yet satisfy everywhere.

## Universal

- **English** for identifiers, comments, log messages, test names, commit messages, docs.
- File-per-type. File name = type name.
- Comments are rare and explain *why*, not *what* (e.g. the EF-only private constructor in `Recipe.cs`, the
  `ChangeTracker.Clear()` calls in integration tests). Do not add narration comments.

---

## Backend (C#)

### Namespaces and files

- Namespace mirrors folder path: `RecipeManager.Application.Handlers.Recipes`.
- **File-scoped namespaces (`namespace X;`) for all new files.** Older files use the block-scoped style; the
  newer ones (`RecipeErrors`, `ResultExtensions`, `CachedRecipeRepository`, validators, dispatchers) are
  already file-scoped. Convert an old file only when you are already changing it substantially — never as a
  standalone reformat commit.
- `TargetFramework`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, and `EnforceCodeStyleInBuild` live
  in `RecipeManager/Directory.Build.props` and apply to every project (ADR-010). **Never re-declare them in a
  `.csproj`** — a project file carries only what is specific to it (`UserSecretsId`, `IsTestProject`, package
  and project references). A new project needs no boilerplate; it inherits the lot.
- **Package versions live in `RecipeManager/Directory.Packages.props`, never in a `.csproj`** (ADR-011). Adding
  a package is two edits: a `<PackageVersion Include="X" Version="N" />` there, and a bare
  `<PackageReference Include="X" />` — **no `Version` attribute** — in the project that needs it. Per-project
  metadata (`PrivateAssets`, `IncludeAssets`) stays on the `PackageReference`. A `Version` in a `.csproj` is
  `error NU1008`, so this is enforced rather than remembered.

### Naming

| Kind | Convention | Example |
| --- | --- | --- |
| Private field | `_camelCase`, `readonly` | `_recipeRepository` |
| Command / Query | `<Verb><Aggregate>Command` / `Query` | `CreateRecipeCommand`, `GetRecipeByIdQuery` |
| Handler | `<CommandOrQueryName minus suffix>Handler` | `CreateRecipeHandler` |
| DTO | `<Purpose>Dto` | `RecipeDto`, `UpdateRecipeDto` |
| Validator | `<BoundType>Validator` | `CreateRecipeCommandValidator`, `UpdateRecipeDtoValidator` |
| Domain error factory | `RecipeErrors.<Condition>()` | `RecipeErrors.TitleRequired()` |
| Cache key | `snake_case` string constant / format | `recipes_all`, `recipe_{0}` |

### Types

- Commands, queries, and DTOs are **positional `record`s**. Never classes.
- Entities are `sealed class` with `private set` properties and private constructors.
- Repositories and middleware are `sealed`.
- `Entity.Id` is `Guid`, `protected init`, assigned in the entity's private constructor via `Guid.NewGuid()`
  (not database-generated).

### Async

- Every I/O method is `async Task`/`async Task<T>` and takes a **non-optional** `CancellationToken` as the last
  parameter — enforced by `IRecipeRepository`, `ICommandHandler`, `IQueryHandler`. `ICacheService` is the one
  exception (`CancellationToken token = default`).
- Controller actions accept `CancellationToken cancellationToken` and pass it through.
- Dispatchers return the handler's `Task` directly without `await` (no extra state machine) — keep that.
- Never `.Result` / `.Wait()` / `async void`.

### Dependency injection

- Constructor injection only; no service locator outside the two dispatchers (which need `IServiceProvider`
  by design).
- **All registration lives in `RecipeManager.Api/Startup/ServiceInitializer.cs`**, as `IServiceCollection`
  extension methods chained in `Program.cs`. Never call `services.Add…` from another layer.
- New CQRS handler ⇒ **no DI change at all**. Handlers are discovered by Scrutor assembly scanning in
  `RegisterCqrsDispatchers()` (ADR-008): implement `ICommandHandler<,>`/`IQueryHandler<,>` in
  `RecipeManager.Application` and it is registered scoped automatically. **Adding a manual
  `services.AddScoped<ICommandHandler<…>, …>()` line is now wrong** — it duplicates the scan.
  `RecipeManager.IntegrationTests/DependencyInjection/CqrsHandlerRegistrationTests.cs` asserts every handler
  interface resolves, so a handler the container cannot build fails a test rather than a production request.
- Everything is `AddScoped` except `AddMemoryCache()` (singleton by the framework).

### Validation — two layers, no duplication

| Layer | Owns | Failure surfaces as |
| --- | --- | --- |
| FluentValidation validator on the **bound request type** | null checks, max length, numeric bounds, collection size | 400 `ValidationProblemDetails` |
| `Recipe.ValidateProperties` (domain) | required/non-empty, "at least one of prep/cook time > 0", servings ≥ 1, no blank items | 422 `ProblemDetails` via `RecipeErrors` |

- The validator must target the type the controller **binds**: `CreateRecipeCommand` for POST (the command is
  the request body), `UpdateRecipeDto` for PUT (the DTO is the body, the id comes from the route).
- Shared rules go in `RecipeManager.Application/Validators/Recipes/RecipeValidationRules.cs` as `IRuleBuilder` extension methods
  (`ValidateTitle`, `ValidateServings`, …) so create and update stay identical.
- Validators are auto-discovered via `AddValidatorsFromAssembly` — no per-validator registration needed.

### Error handling

- Return `Result.Fail(RecipeErrors.X(...))`; do **not** throw for expected failures.
- Every new error goes in `RecipeErrors` with `.WithCode(<http status>)` and `.Field("<camelCaseFieldName>")`.
  `field` values are camelCase to match the JSON payload.
  **⚠ Target:** `.WithCode(<http status>)` puts HTTP semantics in the Domain layer and is being replaced by a
  semantic error kind mapped to HTTP in the API layer (ADR-009, `R-05`). Follow the current pattern for
  consistency until it lands — do not invent a third convention in the meantime.
- `ResultExtensions.CreateProblemDetails` uses `errors.First()` for the status code, so a `Result` mixing kinds
  returns an arbitrary status. Put the most significant error first when returning several. ADR-009 fixes this
  properly.
- `try/catch` in a handler is the exception, not the rule: only `DeleteRecipeHandler` has one (logs and returns
  a failed `Result`). Unhandled exceptions are the middleware's job.

### EF Core / PostgreSQL

- `AppDbContext` has no `OnModelCreating`; mapping is entirely convention-based.
  **⚠ Target:** convention-based mapping is why every string column is unbounded `text` (`SEC-08`). New
  constraints — max lengths, indexes, required-ness beyond nullability — belong in an
  `IEntityTypeConfiguration<T>` in a new `RecipeManager.Infrastructure/Context/Configurations/` folder, applied via
  `modelBuilder.ApplyConfigurationsFromAssembly(...)`. Introducing the first one is a small architecture change:
  clear it with `01-architect`.
- Reads use `.AsNoTracking()`. Writes call `SaveChangesAsync(cancellationToken)` inside the repository method.
- Provider is Npgsql (`UseNpgsql`). `string` → `text`, `Guid` → `uuid`, `IReadOnlyList<string>` → `text[]`.
- Schema changes require a migration:
  ```bash
  dotnet ef migrations add <Name> --project RecipeManager.Infrastructure --startup-project RecipeManager.Api
  ```
  Commit the migration, the `.Designer.cs`, and the updated `AppDbContextModelSnapshot.cs` together.
- Migrations are applied at startup by `app.MigrateDatabase()`. Never edit an applied migration — add a new one.
- PostgreSQL folds unquoted identifiers to lowercase; EF creates `"Recipes"`. Quote table names in raw SQL and
  in psql.

### Controllers

- `[ApiController]`, `[Route("api/[controller]")]`, inherit `ControllerBase`.
- Route constraints on ids: `[HttpDelete("{id:guid}")]`, `[HttpPut("{id:guid}")]`. `[HttpGet("{id}")]` is
  missing this constraint — `BUG-07` in [known-issues.md](known-issues.md); do not copy the omission.
- Actions build the command/query, dispatch, and return `result.ToActionResult()` /
  `result.ToCreatedAtActionResult(...)`. **No business logic, no repository access, no try/catch in a controller.**

### Logging

- `ILogger<T>` injected via constructor.
- **Message templates with named placeholders are required**:
  `_logger.LogError(ex, "Error deleting recipe {RecipeId}", id)`. Interpolated strings defeat structured
  logging — the value is baked into the message and cannot be queried as a field.
  **⚠ Target:** `RecipesController` violates this (`QUAL-01`) and `ApplicationInitializer` uses
  `Console.WriteLine` (`QUAL-02`). Both are defects; do not copy either pattern.
- Never log a secret or a configuration value that might contain one.

---

## Frontend (React / TypeScript)

### Folder structure

```
recipe-manager-frontend/src/
  components/
    common/    cross-cutting widgets (SearchBar)
    layout/    AppLayout, Header, Footer
    ui/        presentational pieces (Logo, NavLink, Recipe/RecipeCard, Recipe/RecipeList)
  contexts/    ThemeContext
  hooks/       useTheme, useRecipes
  pages/       Home/HomePage, Recipe/RecipePage
  services/    recipeService (axios)
  styles/      globals.css, themes/{variables,light,dark}.css
  types/       recipe.ts, theme.ts
  assets/      mainPhoto.png, react.svg
```

### Barrel files — required

Every folder has an `index.ts` re-exporting its contents, and folders roll up
(`components/ui/Recipe/RecipeCard/index.ts` → `…/Recipe/index.ts` → `ui/index.ts` → `components/index.ts`).
Import through the barrel and the alias: `import { RecipeCard } from '@/components'`. Adding a component means
adding/updating its `index.ts`.

### Components

- One component per folder: `Foo/Foo.tsx` + `Foo/Foo.module.css` + `Foo/index.ts`.
- **Named exports only** (`export const Foo`). `AppLayout`, `Header`, and `Footer` also have a default export —
  legacy duplication; do not add default exports to new components.
- Props: a local `interface <Name>Props` declared above the component, with plain destructured parameters.
  `React.FC<Props>` appears in most existing files but is discouraged in current React guidance (it adds
  nothing now that implicit `children` is gone) — **prefer plain destructured params for new components**, as
  `NavLink` and `AppLayout` already do. Do not mix styles within a file.
- Styling: `import styles from './Foo.module.css'` and `className={styles.x}`; conditional classes via template
  literals. MUI is used only for `Box` and icons — do not introduce `sx` props or a MUI `ThemeProvider` without
  an architecture decision.

### State

| Kind of state | Mechanism |
| --- | --- |
| Server data | TanStack Query — a hook in `recipe-manager-frontend/src/hooks/` (see `useRecipes`) |
| Cross-cutting UI state | React Context (`ThemeContext` + `useTheme` guard hook) |
| Local UI state | `useState` in the page/component (`searchQuery` in `RecipePage`) |

**No Redux, no Zustand.** Do not add a global store; add a query hook or a context.

- Query hooks live in `recipe-manager-frontend/src/hooks/`, return the `useQuery` result unchanged, and set their own
  `staleTime`/`gcTime`/`retry`. The `queryKey` is an array (`['recipes']`, `['recipes', id]`).
- **Mutations must invalidate the queries they affect**:
  `queryClient.invalidateQueries({ queryKey: ['recipes'] })` in `onSuccess`. `useRecipes` disables
  `refetchOnMount` and `refetchOnWindowFocus`, so nothing else will refresh the list — a mutation without
  invalidation leaves permanently stale data on screen. No mutation hook exists yet; the first one sets the
  precedent.
- Consumers of a context must go through a guard hook that throws when the provider is missing — copy the
  `useTheme` pattern.

### API access

- All HTTP goes through `recipe-manager-frontend/src/services/recipeService.ts`; components and hooks never call `axios` directly.
- One shared `AxiosInstance` with `baseURL` from `import.meta.env.VITE_API_URL`, trailing slashes stripped,
  falling back to `/api`.
- Service methods return `Promise<AxiosResponse<T>>`; the calling hook unwraps `.data`.
- Endpoint paths use the capitalised controller name: `/Recipes`, `/Recipes/${id}`.

### Types

- Domain-ish types in `src/types/`, re-exported from `recipe-manager-frontend/src/types/index.ts`, imported as `@/types`.
- `strict` TS with `noUnusedLocals` and `noUnusedParameters` — unused imports break the build. `any` is not used
  anywhere in `recipe-manager-frontend/src/`; keep it that way.

### Accessibility (already established, keep it up)

`role="switch"` + `aria-checked` on the theme toggle, `aria-current="page"` on the active nav link,
`aria-label` on icon-only and ambiguous controls, `<time dateTime={ISO8601}>` for durations, semantic
`header`/`nav`/`main`/`footer`/`section`/`article`, `loading="lazy"` + `decoding="async"` + descriptive `alt`
on images.

### Avoid

- `console.log` in committed code. `SearchBar.handleInputChange` and `RecipeList.handleRecipeClick` still have
  some (`BUG-08` in [known-issues.md](known-issues.md)) — do not add more. Note that ESLint would normally catch
  this but currently cannot run at all (`BUILD-03`).
- Inline styles.
- Declaring a path alias in only one of `vite.config.ts` / `tsconfig.json`.

---

## Tests

### Backend

- Test class name: `<TypeUnderTest>Tests` (the one outlier is `UpdateRecipeHandlerTest`, singular).
- Method name: `<Method>_<Scenario>_<ExpectedOutcome>` — `Handle_WhenRecipeNotFound_ShouldReturnFailureResult`.
- `[Fact]` for single cases, `[Theory]` + `[InlineData]` for input tables.
- **Arrange / Act / Assert comment markers are required.** Unit tests use `// Arrange`; integration tests use
  the `// ==================== ARRANGE ====================` banner style. Match the file you are in.
- Assertions use FluentAssertions (`result.IsSuccess.Should().BeTrue()`), never `Assert.*`.
- Mocks use **NSubstitute**: `Substitute.For<IRecipeRepository>()`, `.Returns(...)`,
  `await repo.Received(1).AddAsync(Arg.Is<Recipe>(r => …), Arg.Any<CancellationToken>())`.
- Dependencies are created in the test-class constructor and stored in readonly fields (no `IClassFixture` in
  use today).
- `#region Success Scenarios` / `#region Failure Scenarios` grouping is used in the larger unit-test files —
  follow the surrounding file.
- Integration tests derive from `IntegrationTestBase`, get a fresh InMemory database per test
  (`TestDb_{Guid}`), seed via `SeedDatabase(...)`, and call `DbContext.ChangeTracker.Clear()` before asserting
  post-write state.
- **Zero warnings, enforced.** `TreatWarningsAsErrors` is on for every project (ADR-010), so a warning fails the
  build rather than accumulating. Two idioms exist because of this, both from the NSubstitute 6 / xUnit analyser
  set:
  - Null-guard inside an `Arg.Is<T>` predicate — `Arg.Is<Recipe>(r => r != null && r.Title == command.Title)` —
    because NSubstitute 6 annotates the predicate parameter as nullable. Do not use `r!` or
    `#pragma warning disable`; the guard also makes the assertion honest.
  - A `[Theory]` with `[InlineData(null)]` takes a **nullable** parameter (`string? invalidTitle`) and
    null-forgives at the call site (`Recipe.Create(invalidTitle!, …)`). `!` is correct there and only there:
    passing null *is* the scenario under test.

### Frontend

None exist. See [agents/06-qa-tester.md](agents/06-qa-tester.md) before writing the first one.
