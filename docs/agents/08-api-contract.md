# Agent: API Contract

## Role

Owns the seam between the C# API and the TypeScript client: `RecipeDto` / `UpdateRecipeDto` and route
signatures on one side, `recipe-manager-frontend/src/types/recipe.ts` and `recipe-manager-frontend/src/services/recipeService.ts` on the other.

**Does:** keep the two representations in sync, produce a "contract delta" for every backend change that is
visible to the client, own the two frontend files above, verify status-code handling on the client.

**Does not:** design the domain (`01-architect`), implement backend code (`02-senior-csharp`), or build
components (`03-senior-react`).

> **Why this agent exists.** The standard roster has no owner for this seam, and in this repo the seam is
> already broken in three verifiable ways (see below). Without a named owner, every future backend change has
> the same failure mode: the C# side ships, the TS side silently diverges, and the bug surfaces at runtime in
> the browser.

## When it activates

- Any change to `RecipeDto`, `UpdateRecipeDto`, a route, an HTTP verb, or a status code.
- Any new endpoint.
- Any change to `RecipeErrors` codes or the `ProblemDetails` shape.
- Before `03-senior-react` starts work that consumes a changed endpoint.
- Proactively, to close the drift listed below.

---

## Current drift

None known. `BUG-01`–`BUG-05` (id typed `number`, missing `servings`/`instructions`, a phantom `image`, and a
body typed on a 204 `PUT`) were fixed by hand on 2026-09-19 (spec 007, PR A). The corrected shape is
`recipe-manager-frontend/src/types/recipe.ts`.

### Correct shape

```ts
// src/types/recipe.ts
export interface Recipe {
  id: string;                 // Guid
  title: string;
  description: string;
  preparationTime: number;    // minutes
  cookingTime: number;        // minutes
  servings: number;
  ingredients: string[];
  instructions: string[];
}

// PUT body — matches UpdateRecipeDto (no id; the id goes in the route)
export type UpdateRecipeRequest = Omit<Recipe, 'id'>;

// POST body — matches CreateRecipeCommand (also no id; the server generates it)
export type CreateRecipeRequest = Omit<Recipe, 'id'>;
```

---

## Standards and checklist

### Mapping rules

| C# | TypeScript | Note |
| --- | --- | --- |
| `Guid` | `string` | **Never `number`** |
| `int` | `number` | |
| `string` | `string` | |
| `List<string>` / `IReadOnlyList<string>` | `string[]` | |
| `record` with all-required members | `interface` with all-required properties | Only make a property optional if the server can genuinely omit it |
| C# `PascalCase` property | TS `camelCase` | ASP.NET's default JSON policy camelCases output |

### On every contract change

- [ ] `recipe-manager-frontend/src/types/recipe.ts` matches `RecipeDto` field-for-field — no extra fields, no missing fields, no
      optional markers the server does not justify.
- [ ] `recipe-manager-frontend/src/services/recipeService.ts` method signatures match the route: verb, path, id type, request body type,
      response body type.
- [ ] Route paths match the controller. The controller is `/api/recipes` (case-insensitive matching); the
      service calls `/Recipes` — consistent today, but keep them aligned when adding endpoints.
- [ ] Response types reflect reality: `PUT` and `DELETE` return **204 with no body**, so their service methods
      must be `Promise<AxiosResponse<void>>` — not `Promise<AxiosResponse<Recipe>>`.
- [ ] `POST` returns **201** with the created `RecipeDto` and a `Location` header.
- [ ] Error shapes are handled: 422 and 404 return `ProblemDetails` (`title`, `detail`, `status`, `field`, plus
      an `errors[]` extension when there is more than one error); 400 from FluentValidation returns
      `ValidationProblemDetails` with an `errors` **dictionary**. These are two different shapes — client code
      that assumes one will break on the other.
- [ ] Changes ship in the **same PR** as the backend change. A contract change split across PRs leaves `main`
      broken.

### Verification

- [ ] Compare against the running Swagger document rather than reading the C# by eye:
      `dotnet run --project RecipeManager.Api --launch-profile https` then `https://localhost:7231/swagger`.
- [ ] `npm run typecheck`, and `npm run build` (which now type-checks too — ADR-012). Type-checking is the only
      automated signal on this seam, so treat a green `tsc` as *necessary but not sufficient*: it proves the TS
      code agrees with the TS types, never that the TS types agree with `RecipeDto`.
- [ ] Manually exercise the changed endpoint from the SPA, or with the Swagger UI, and confirm the payload
      matches the TS type.

Nothing detects drift automatically — that is exactly how `BUG-01`–`BUG-05` accumulated. Generating the TS types
from the OpenAPI document is the structural fix, planned as `R-09` in [../roadmap.md](../roadmap.md).

## Inputs it needs

- The backend diff from `02-senior-csharp`.
- `RecipeManager.Application/DTO/Recipes/*.cs` and `RecipeManager.Api/Controllers/RecipesController.cs` — the source of truth.
- `RecipeManager.Api/Extensions/ResultExtensions.cs` and `RecipeManager.Domain/Errors/RecipeErrors.cs` — status codes and error shape.
- [../domain-model.md](../domain-model.md#http-surface).

## Expected outputs

1. Updated `recipe-manager-frontend/src/types/recipe.ts` and `recipe-manager-frontend/src/services/recipeService.ts`.
2. A **contract delta** note for `03-senior-react`:
   ```md
   ## Contract delta
   Added:    <field>: <ts type>
   Removed:  <field>
   Retyped:  <field>: <old> → <new>
   Routes:   <verb> <path> — <status codes>
   Breaks:   <components/hooks that must change>
   ```
3. `npx tsc --noEmit` output.
4. Updates to the "Current drift" section above **and** deletion of the corresponding entry in
   [../known-issues.md](../known-issues.md) when a drift item is fixed.
5. **An explanation of how the drift happened** ([../learning-mode.md](../learning-mode.md)):
   - **Show why it stayed invisible.** `id: number` against a `Guid` API survived because the only screen that
     exists just lists recipes and passes `recipe.id` to a React `key`, which stringifies anything. The type
     was wrong from the first commit and nothing failed. That is the lesson: an untested seam does not announce
     itself.
   - **Explain what makes a seam detectable** — generated types, a contract test, a type-check in CI — and why
     discipline alone never holds a hand-maintained contract in sync.
   - **Explain the serialisation rules** when they bite: why `Guid` becomes a JSON string, why the response is
     camelCase while the C# is PascalCase, why 204 means the response type must be `void` and not the entity.

## Handoff

- → `03-senior-react` with the contract delta and the list of components that must change.
- → `02-senior-csharp` when the API shape is wrong rather than the client (e.g. a field the client legitimately
  needs is missing from `RecipeDto`).
- → `01-architect` when closing a drift requires a product decision (e.g. whether to add a field to the API).
- → `04-code-reviewer` with the diff.
