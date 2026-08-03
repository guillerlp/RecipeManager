# Bugfix workflow

---

## 1. Reproduce and locate the layer — `00-leader`

**Reads:** the report, [../known-issues.md](../known-issues.md) — **check this first, the bug may already be
documented** — and [../architecture.md](../architecture.md#request-flow-put-apirecipesid).

Use the observed HTTP status to narrow the layer before reading any code:

| Symptom | Most likely origin | Start here |
| --- | --- | --- |
| **400** with `ValidationProblemDetails` (`errors` dictionary keyed by property) | FluentValidation auto-validation | `RecipeManager.Application/Validators/Recipes/RecipeValidationRules.cs` |
| **422** with `ProblemDetails` + `field` | domain invariant | `RecipeManager.Domain/Entities/Recipe.cs` → `ValidateProperties`, `RecipeManager.Domain/Errors/RecipeErrors.cs` |
| **404** | recipe missing, or the route did not match | handler `GetByIdAsync` path; check the `{id:guid}` constraint |
| **500** with `ProblemDetails.Type` = an exception name | unhandled exception | `RecipeManager.Api/Middlewares/ErrorHandlerMiddleware.cs` log line |
| Wrong status code for a known failure | `ErrorCode` metadata, or error ordering | `RecipeErrors.WithCode`, `ResultExtensions.CreateProblemDetails` (uses `errors.First()` only) |
| `InvalidOperationException: No service for type ICommandHandler<…>` | handler not picked up by the Scrutor scan — check it is a public, non-abstract class in `RecipeManager.Application` implementing the handler interface | `RecipeManager.Api/Startup/ServiceInitializer.RegisterCqrsDispatchers()` |
| Stale data after a write | cache invalidation | `RecipeManager.Infrastructure/Repositories/Recipes/CachedRecipeRepository.cs` |
| Frontend shows nothing / CORS error in console | CORS origin or API not running | `ServiceInitializer.RegisterCors` (only `http://localhost:3000`), `vite.config.ts` proxy |
| Frontend request fails with a certificate error | untrusted dev cert | run `dotnet dev-certs https --trust` |
| API fails to start, Npgsql connection/auth error | PostgreSQL down, or password missing | password comes from user-secrets / `ConnectionStrings__DefaultConnection`; the committed value is a password-less template |
| API throws `"Error while parsing appsettings data"` | `ConnectionStrings` section missing or empty | `Program.Main` |
| `relation "recipes" does not exist` in psql | PostgreSQL identifier folding | quote it: `SELECT * FROM "Recipes";` |

Frontend-specific suspects worth checking first, since they are known-broken:

- `recipe-manager-frontend/src/types/recipe.ts` declares `id: number` while the API returns a `Guid` string, and omits `servings` and
  `instructions` — see [../domain-model.md](../domain-model.md#frontend-view-of-the-domain--currently-out-of-sync).
- `HomePage` links to `/recipes/new`, which has no route in `App.tsx`.
- `.env.production` still points at the placeholder `https://your-production-api.com/api`.

## 2. Write the failing test first — `06-qa-tester`

**Reads:** [../agents/06-qa-tester.md](../agents/06-qa-tester.md), [../conventions.md](../conventions.md#tests).

- Domain rule ⇒ a `[Fact]`/`[Theory]` in `RecipeManager.UnitTests/Domain/Entities/RecipeTests.cs`.
- Handler behaviour ⇒ a test in `RecipeManager.UnitTests/Application/Handlers/`.
- Wrong status code, serialisation, routing, or persistence ⇒ an integration test in
  `RecipeManager.IntegrationTests/RecipesControllerTests.cs`.
- Name it for the bug: `Handle_WhenRecipeNotFound_ShouldReturnFailureResult`.
- Confirm it fails for the right reason:
  ```bash
  dotnet test RecipeManager.sln
  ```

**Caveat:** integration tests run against **EF InMemory**, not PostgreSQL. Provider-specific bugs (`text[]`
handling, identifier folding, collation, concurrency) will **not** reproduce there — verify those against a real
database manually and say so in the PR.

## 3. Fix at the correct layer — `02-senior-csharp` / `03-senior-react`

**Reads:** [../conventions.md](../conventions.md), the relevant agent file.

Fix where the rule belongs, not where the symptom appears:

- Business rule wrong or missing → `Recipe.ValidateProperties` (**not** the handler, **not** the validator).
- Bound-payload shape wrong (null, length, bounds) → `RecipeValidationRules`.
- Wrong HTTP status → `RecipeErrors.WithCode`, or the order of errors returned.
- Stale/incorrect data → cache invalidation in `CachedRecipeRepository`, not a `try/catch` in the handler.
- Missing null check on repository results → the handler already returns `RecipeErrors.RecipeNotFound`; follow
  that pattern rather than throwing.
- Frontend rendering/state → the component or hook; never patch it by changing the API response shape.

Do not add a `try/catch` to silence a symptom — only `DeleteRecipeHandler` has one, and it logs and returns a
failed `Result`.

## 4. Check the blast radius — `01-architect` (only if needed)

Escalate when the fix requires: a schema/migration change, a new project reference, changing an interface in
`Domain`, changing the `Result`→HTTP mapping for all endpoints, or altering pipeline order in
`ApplicationInitializer.ConfigurePipeline`. Output: an ADR entry in [../architecture.md](../architecture.md).

## 5. Regression sweep — `06-qa-tester`

- The new test passes; the whole suite still passes (currently 84 tests) and no new build warning appeared
  (currently 7, target 0 — see [../known-issues.md](../known-issues.md)).
- If the bug was a cache issue, add a test that performs write-then-read through the API — the integration tests
  exercise the real decorator chain.
- If the bug involved concurrency or ordering, state explicitly in the PR that it is not covered; there is no
  concurrency control in the schema.

## 6. Review — `04-code-reviewer`, plus `05-security-reviewer` when relevant

Security review is mandatory when the bug involved: unvalidated input reaching persistence or the DOM, an error
message leaking internals (`ErrorHandlerMiddleware` echoes `exception.Message` to the client), CORS, or
configuration/secrets.

## 7. Ship and explain — see [release-workflow.md](release-workflow.md)

Branch name: `fix/<short-description>`. The PR description must state the root cause, the layer fixed, and the
test that now covers it.

**Explain the root cause, not just the patch** ([../learning-mode.md](../learning-mode.md)). A bug is the
cheapest teaching material available, because the cost has already been paid:

- **Why the bug was possible at all** — the missing constraint, the untested seam, the convention that was not
  enforced. `BUG-01` (`id: number` against a `Guid` API) was invisible because React `key` stringifies
  anything; that is the real lesson, not the type annotation.
- **Why it surfaced where it did**, which is usually not where the cause lives.
- **Why the fix belongs in the layer you put it in**, and what would have gone wrong fixing it a layer up or
  down — patching a symptom in the handler instead of the entity is the classic version of this.
- **What class of bug this belongs to**, so the next one is recognisable.
- **Whether anything now prevents a recurrence**, or whether the fix relies on remembering.

If the bug taught something worth keeping — a class of mistake, a wrong assumption about how a library behaves,
a gap the process should have caught — append an entry to [../decisions-log.md](../decisions-log.md). Bugs make
the best entries in that file: the tuition has already been paid.
