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
body typed on a 204 `PUT`) were fixed by hand on 2026-09-19 (spec 007, PR A). Since PR B (`R-09`, ADR-019) the
shape is no longer hand-written — it is generated from `RecipeManager/contracts/openapi.json` and aliased.

### Correct shape

`recipe-manager-frontend/src/types/recipe.ts` in full — never add a field here; regenerate instead (see
"Verification" below):

```ts
// src/types/recipe.ts
// Aliases over the generated contract (R-09 / ADR-019). Never add fields here: change the C# DTO, then
// regenerate (see README, "Changing the API contract").
import type { components } from './generated/api';

type Schemas = components['schemas'];

export type Recipe = Schemas['RecipeDto'];
export type CreateRecipeRequest = Schemas['CreateRecipeCommand'];
export type UpdateRecipeRequest = Schemas['UpdateRecipeDto'];
export type Ingredient = Schemas['IngredientDto'];
export type IngredientInput = Schemas['IngredientInputDto'];
```

`Recipe` is `id: string`, `title`, `description`, `preparationTime`, `cookingTime`, `servings`,
`ingredients: Ingredient[]`, `instructions: string[]` — the generator produces it from `RecipeDto` rather than a
hand-written interface. `CreateRecipeRequest` and `UpdateRecipeRequest` alias `CreateRecipeCommand` and
`UpdateRecipeDto` directly rather than being computed with `Omit<Recipe, 'id'>`, because those are separate
generated schemas, not derived types. The last two aliases arrived with ADR-022 and are legal under ADR-019
precisely because they are genuinely new generated schemas, not hand-written fields.

Three things about the ingredient pair are deliberate and must survive any regeneration:

- **`IngredientDto.id` is required, `IngredientInputDto.id` is nullable.** The server always knows an
  ingredient's id; the client only sometimes does, and a null on the way in means "this one is new".
- **`unit` is nullable on both, and that took work to express.** OpenAPI 3.0 forbids sibling keywords beside a
  `$ref`, so Swashbuckle emitted the enum reference with its nullability silently dropped, and
  `RequireNonNullablePropertiesSchemaFilter` then listed it in `required`. The generated TypeScript claimed
  `unit` was always present while the API returns `null` for "salt to taste".
  `RecipeManager.Api/Startup/Swagger/NullableEnumSchemaFilter.cs` wraps those references in `allOf` so
  `nullable: true` can sit beside them. **If a regenerated `api.ts` ever shows `unit` as non-nullable, that
  filter has stopped applying** — fix the filter, never the generated file.
- **`unit` is a closed enum**, so its members are part of the contract: adding one is a code-and-deploy change
  on both sides, not a data change.

---

## Standards and checklist

### Mapping rules

| C# | TypeScript | Note |
| --- | --- | --- |
| `Guid` | `string` | **Never `number`** |
| `int` | `number` | |
| `string` | `string` | |
| `List<string>` / `IReadOnlyList<string>` | `string[]` | |
| `List<T>` of a record | `T[]` of the generated schema | e.g. `List<IngredientDto>` → `IngredientDto[]` |
| `enum` | a string union | `JsonStringEnumConverter` is registered, so it crosses the wire as the member name |
| nullable `enum` (`Unit?`) | union `\| null` | **Needs `NullableEnumSchemaFilter`** — see above. Without it the nullability is lost and the property is wrongly `required` |
| `decimal?` | `number \| null` | |
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
- [ ] Snapshot regenerated and `npm run gen:api` run; `contracts/openapi.json` and `src/types/generated/api.ts`
      committed in the same PR.

### Verification

- [ ] Compare against the running Swagger document rather than reading the C# by eye:
      `dotnet run --project RecipeManager.Api --launch-profile https` then `https://localhost:7231/swagger`.
- [ ] `npm run typecheck`, and `npm run build` (which now type-checks too — ADR-012). Treat a green `tsc` as
      *necessary but not sufficient*: it proves the TS code agrees with the TS types, never that the TS types
      agree with `RecipeDto`. That second link is what the drift gate below is for — `tsc` is one signal, the
      two-link CI gate (the snapshot test and the generated-types diff) is the other, and the gate is what
      actually watches `RecipeDto` itself.
- [ ] Manually exercise the changed endpoint from the SPA, or with the Swagger UI, and confirm the payload
      matches the TS type.

**The drift gate (`R-09`, ADR-019).** Nothing here is discipline any more — it is enforced in CI, in two links.
CI is not yet *required* to merge (`INFRA-07`), so a red run can still be merged past, but the two links below
are checked on every PR.
`OpenApiContractTests` (backend job) compares Swashbuckle's `v1` document against the committed
`RecipeManager/contracts/openapi.json` and fails if they disagree; `openapi-typescript` (frontend job) fails if
regenerating `src/types/generated/api.ts` from that snapshot changes anything. `src/types/recipe.ts` is now only
aliases over the generated schemas — there is nothing left in it to drift by hand.

A contract change is a three-step ritual, from `RecipeManager/`:

```bash
UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --filter OpenApiContractTests
```

then from `RecipeManager/recipe-manager-frontend/`:

```bash
npm run gen:api
```

and commit both files. See the README's "Changing the API contract" for the PowerShell form and for the second
acceptance route — pushing and downloading CI's `openapi-received` artifact — used when the snapshot test cannot
run locally at all (Windows Smart App Control, `INFRA-06`).

## Inputs it needs

- The backend diff from `02-senior-csharp`.
- `RecipeManager.Application/DTO/Recipes/*.cs` and `RecipeManager.Api/Controllers/RecipesController.cs` — the source of truth.
- `RecipeManager.Api/Extensions/ResultExtensions.cs` and `RecipeManager.Domain/Errors/RecipeErrors.cs` — status codes and error shape.
- [../domain-model.md](../domain-model.md#http-surface).

## Expected outputs

1. Regenerated types (`recipe.ts` holds aliases only) and updated `recipe-manager-frontend/src/services/recipeService.ts`.
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
