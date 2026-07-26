# Agent: API Contract

## Rol

Owns the seam between the C# API and the TypeScript client: `RecipeDto` / `UpdateRecipeDto` and route
signatures on one side, `src/types/recipe.ts` and `src/services/recipeService.ts` on the other.

**Does:** keep the two representations in sync, produce a "contract delta" for every backend change that is
visible to the client, own the two frontend files above, verify status-code handling on the client.

**Does not:** design the domain (`01-architect`), implement backend code (`02-senior-csharp`), or build
components (`03-senior-react`).

> **Why this agent exists.** The standard roster has no owner for this seam, and in this repo the seam is
> already broken in three verifiable ways (see below). Without a named owner, every future backend change has
> the same failure mode: the C# side ships, the TS side silently diverges, and the bug surfaces at runtime in
> the browser.

## Cuándo se activa

- Any change to `RecipeDto`, `UpdateRecipeDto`, a route, an HTTP verb, or a status code.
- Any new endpoint.
- Any change to `RecipeErrors` codes or the `ProblemDetails` shape.
- Before `03-senior-react` starts work that consumes a changed endpoint.
- Proactively, to close the drift listed below.

---

## Current drift — open defects

Verified against `RecipeDto` (`Application/DTO/Recipes/RecipeDto.cs`) and `src/types/recipe.ts`.

| # | Server truth | Client declaration | Consequence |
| --- | --- | --- | --- |
| C1 | `Guid Id` — serialised as a string, e.g. `"3fa85f64-…"` | `id: number` | Every id is typed wrong. `getRecipeById(id: number)`, `updateRecipe(id: number, …)`, and `deleteRecipe(id: number)` all take the wrong type; `RecipeCard`'s `key={recipe.id}` works only because React stringifies it |
| C2 | `int Servings` | *missing* | The client cannot display or edit servings |
| C3 | `List<string> Instructions` | *missing* | The client cannot display or edit instructions — a recipe app that cannot show its own steps |
| C4 | *(no image field on `RecipeDto`)* | `image?: string` | Always `undefined`; `RecipeCard` permanently falls back to the bundled `mainPhoto.png` |

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

Fixing C1–C3 is a breaking change to `recipeService` signatures and will touch `RecipeCard` and `RecipeList`.
Coordinate with `03-senior-react` and do it in one PR. C4 needs a product decision first — either add an image
field to the API (`01-architect` + `05-security-reviewer`, see the upload requirements there) or drop `image?`
from the type.

C1–C4 are tracked as `BUG-01`–`BUG-04` in [../known-issues.md](../known-issues.md). Fixing C1–C3 is a
prerequisite for any recipe detail or edit screen.

---

## Estándares y checklist

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

- [ ] `src/types/recipe.ts` matches `RecipeDto` field-for-field — no extra fields, no missing fields, no
      optional markers the server does not justify.
- [ ] `src/services/recipeService.ts` method signatures match the route: verb, path, id type, request body type,
      response body type.
- [ ] Route paths match the controller. The controller is `/api/recipes` (case-insensitive matching); the
      service calls `/Recipes` — consistent today, but keep them aligned when adding endpoints.
- [ ] Response types reflect reality: `PUT` and `DELETE` return **204 with no body**, so their service methods
      must be `Promise<AxiosResponse<void>>` — not `Promise<AxiosResponse<Recipe>>`. `updateRecipe` currently
      gets this wrong (`BUG-05`); fix it alongside C1–C3.
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
- [ ] `npx tsc --noEmit` — **not** `npm run build`, which strips types without checking them (`BUILD-04`).
      Type-checking is the only automated signal on this seam, and it is not currently wired into any script.
- [ ] Manually exercise the changed endpoint from the SPA, or with the Swagger UI, and confirm the payload
      matches the TS type.

Nothing detects drift automatically — that is exactly how `BUG-01`–`BUG-04` accumulated. Generating the TS types
from the OpenAPI document is the structural fix, planned as `R-09` in [../roadmap.md](../roadmap.md).

## Inputs que necesita

- The backend diff from `02-senior-csharp`.
- `Application/DTO/Recipes/*.cs` and `Controllers/RecipesController.cs` — the source of truth.
- `Api/Extensions/ResultExtensions.cs` and `Domain/Errors/RecipeErrors.cs` — status codes and error shape.
- [../domain-model.md](../domain-model.md#http-surface).

## Outputs esperados

1. Updated `src/types/recipe.ts` and `src/services/recipeService.ts`.
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
4. Updates to the drift table above **and** deletion of the corresponding `BUG-01`…`BUG-05` entry in
   [../known-issues.md](../known-issues.md) when an item is fixed.
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
- → `01-architect` when closing the drift requires a product decision (C4, the image field).
- → `04-code-reviewer` with the diff.
