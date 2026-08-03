# Feature workflow

End-to-end path for a new feature. All `dotnet` commands run from `RecipeManager/`.

---

## 1. Clarify and spec — `00-leader`

**Reads:** the user request, [../domain-model.md](../domain-model.md), [../roadmap.md](../roadmap.md),
[../specs/_template.md](../specs/_template.md).

- **Check the roadmap first.** The request may already be a planned item, may be blocked by an open Phase 1
  item, or may conflict with something on the [deliberately-not-planned](../roadmap.md#deliberately-not-planned)
  list.
- Copy `docs/specs/_template.md` to `docs/specs/<NNN>-<kebab-name>.md` and fill it in.
- Stop and ask the user if any of these are unanswered: who owns a recipe (there is no `User` entity), whether
  ingredients must become structured data, what happens to existing rows.
- Output: a filled spec + an agent assignment list.

## 2. Architecture check — `01-architect`

**Reads:** the spec, [../architecture.md](../architecture.md), [../domain-model.md](../domain-model.md).

Required whenever the feature touches any of: a new entity or aggregate, a change to `Recipe`'s shape, a new
project reference, a new NuGet/npm dependency, authentication, multi-record transactions, or any of the eleven
items in [Known limitations](../domain-model.md#known-limitations).

- Output: an ADR entry appended to [../architecture.md](../architecture.md) (Status / Decision / Consequences),
  or an explicit "no architectural impact" line in the spec.
- Skip only when the feature is a new endpoint over the existing `Recipe` shape.

## 3. Domain — `02-senior-csharp`

**Reads:** [../conventions.md](../conventions.md#backend-c), [../domain-model.md](../domain-model.md).

1. Add/modify the invariant inside `Recipe.ValidateProperties` (never in the handler).
2. Add the corresponding factory to `RecipeErrors` with `.WithCode(...)` and `.Field("camelCaseName")`.
3. Extend `Create` and `Update` together — they share the same validation path.
4. Unit-test the invariant in `RecipeManager.UnitTests/Domain/Entities/RecipeTests.cs` before moving on.

## 4. Application — `02-senior-csharp`

1. `RecipeManager.Application/Commands/Recipes/` or `.../Queries/Recipes/`: a positional `record` implementing `ICommand<TResult>` /
   `IQuery<TResult>`.
2. `RecipeManager.Application/Handlers/Recipes/`: one handler, constructor-injected `IRecipeRepository`, `CancellationToken` last.
3. `RecipeManager.Application/DTO/Recipes/` if the response shape changes; extend `RecipeMappingExtensions.MapToRecipeDto`.
4. `RecipeManager.Application/Validators/Recipes/`: a validator for the type the controller **binds**, reusing/extending
   `RecipeValidationRules`.
5. **No DI registration step** — Scrutor discovers the handler by assembly scan (ADR-008). Never add a manual
   `AddScoped` line for it.
6. Unit-test the handler with NSubstitute in `RecipeManager.UnitTests/Application/Handlers/`.

## 5. Infrastructure — `02-senior-csharp`

Only if persistence changes.

1. Add the method to `IRecipeRepository` (in **Domain**), implement it in `RecipeRepository` with
   `AsNoTracking()` for reads and `SaveChangesAsync(ct)` for writes.
2. Implement it in `CachedRecipeRepository` too — the decorator must satisfy the whole interface. Decide the
   cache key (`RecipeManager.Infrastructure/Constants/CacheKeys.cs`), duration (`RecipeManager.Infrastructure/Constants/CacheDuration.cs`), and **which keys a write
   invalidates**.
3. Schema change ⇒ new migration:
   ```bash
   dotnet ef migrations add <Name> --project RecipeManager.Infrastructure --startup-project RecipeManager.Api
   ```
   Commit migration + `.Designer.cs` + `AppDbContextModelSnapshot.cs` together. Review the generated SQL types —
   Npgsql maps `string` → `text` and `IReadOnlyList<string>` → `text[]`, which is rarely what you want for a
   bounded field.

## 6. API — `02-senior-csharp`

1. Action in `RecipesController`: build the command/query, dispatch, return `result.ToActionResult()` or
   `ToCreatedAtActionResult(...)`. No logic in the controller.
2. Use `{id:guid}` route constraints.
3. Confirm the status codes the new `RecipeErrors` entries produce — `ErrorCode` metadata drives them, and only
   `errors.First()` sets the response status.

## 7. Contract sync — `08-api-contract`

**Reads:** [../agents/08-api-contract.md](../agents/08-api-contract.md), [../domain-model.md](../domain-model.md#frontend-view-of-the-domain--currently-out-of-sync).

- Update `recipe-manager-frontend/src/types/recipe.ts` and `services/recipeService.ts` to match the new
  `RecipeDto` / route signature, in the same PR as the backend change.
- Output: a short "contract delta" note (fields added/removed/retyped, status codes) handed to `03-senior-react`.

## 8. Frontend — `03-senior-react`

**Reads:** [../conventions.md](../conventions.md#frontend-react--typescript), [../agents/07-ux-ui.md](../agents/07-ux-ui.md).

1. Service method in `services/recipeService.ts` (never call axios from a component).
2. Query/mutation hook in `recipe-manager-frontend/src/hooks/`, exported from `recipe-manager-frontend/src/hooks/index.ts`. Mutations must invalidate the
   `['recipes']` query key — no mutation hook exists yet, so establish the pattern deliberately.
3. Component in the right bucket (`ui/` presentational, `common/` cross-cutting, `layout/` chrome) with
   `Foo.tsx` + `Foo.module.css` + `index.ts`, and update every parent barrel.
4. Page wiring in `recipe-manager-frontend/src/pages/` and a route in `App.tsx` if needed — note `/recipes/new` is already linked from
   `HomePage` but **has no route**, so it currently renders nothing.
5. Design tokens and a11y per [../agents/07-ux-ui.md](../agents/07-ux-ui.md).

## 9. Tests — `06-qa-tester`

**Reads:** [../agents/06-qa-tester.md](../agents/06-qa-tester.md).

- Domain unit tests for every new invariant (`[Theory]` for input tables).
- Handler unit tests: success, not-found, validation failure, cancellation-token propagation, repository
  interaction (`Received(1)`).
- Integration test in `RecipesControllerTests` for each new endpoint: status code + database state, with
  `DbContext.ChangeTracker.Clear()` before asserting after a write.
- Run and compare against the current numbers — 84 passing, 7 build warnings (target 0, see
  [../known-issues.md](../known-issues.md)):
  ```bash
  dotnet test RecipeManager.sln
  ```

## 10. Review — `04-code-reviewer` then `05-security-reviewer`

- `04-code-reviewer`: [../agents/04-code-reviewer.md](../agents/04-code-reviewer.md) checklist.
- `05-security-reviewer`: mandatory when the feature accepts user input that is stored or rendered, adds a file
  upload, changes CORS/config/connection strings, or touches anything auth-adjacent.

## 11. Ship and explain — `00-leader`

Follow [release-workflow.md](release-workflow.md). Update `README.md`, `CLAUDE.md`, `docs/domain-model.md`, and
`docs/architecture.md` in the same PR when behaviour they describe has changed.

**Then consolidate the reasoning** ([../learning-mode.md](../learning-mode.md)). Each agent explained its own
decisions along the way; the leader turns that into one coherent account rather than nine fragments:

- The two or three decisions in this feature that had real alternatives, and why each went the way it did.
- Any pattern applied for the first time — named, with the file to read.
- What the feature made harder or foreclosed.
- What was left undone and why.

Append an entry to [../decisions-log.md](../decisions-log.md) for anything that met the bar there — a decision
that would be hard to justify in six months, or a reversal of an earlier call.

A feature is not delivered until the user could explain it back.

---

## Quick agent routing

| Change | Agents, in order |
| --- | --- |
| New endpoint over existing `Recipe` | 02-senior-csharp → 08-api-contract → 06-qa-tester → 04-code-reviewer |
| New field on `Recipe` | 01-architect → 02-senior-csharp (+ migration) → 08-api-contract → 03-senior-react → 06-qa-tester → 04 → 05 |
| New entity / aggregate | 01-architect (ADR **required**) → 02-senior-csharp → 06-qa-tester → 04 → 05 |
| Frontend-only screen | 07-ux-ui → 03-senior-react → 06-qa-tester → 04 |
| Auth, ownership, multi-tenancy | 01-architect + 05-security-reviewer **before** any code |
| Recipe image upload | 01-architect + 05-security-reviewer before any code |
| Dependency or runtime upgrade | 01-architect (ADR) → 02-senior-csharp / 03-senior-react → 06-qa-tester |
