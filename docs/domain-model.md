# Domain model

**Mandatory reading for every agent before touching code.**

The domain today contains **exactly one aggregate: `Recipe`**. There is no `Ingredient`, `Step`, `Unit`,
`Category`, `Tag`, or `User` entity. Do not assume otherwise.

Some of that is deliberate; some is a shortcut with a decided replacement. The
[Target model](#target-model--where-the-domain-is-going) section at the end says which is which — read it
before designing anything that touches ingredients, ownership, or listing behaviour.

```
Entity (abstract, RecipeManager.Domain/Shared/Entity.cs)
  Guid Id            protected init
  Equals             concrete type + Id
  GetHashCode        Id only
    ▲
    │
  Recipe (sealed, RecipeManager.Domain/Entities/Recipe.cs)   ← the only aggregate root
```

`Equals` and `GetHashCode` deliberately use different inputs. This satisfies the equality contract — objects
that are equal always hash the same — but two entities of different types sharing an id hash identically while
comparing unequal.

## `Recipe`

| Property | C# type | PostgreSQL column | Notes |
| --- | --- | --- | --- |
| `Id` | `Guid` | `uuid`, PK | Generated in the constructor with `Guid.NewGuid()`, **not** by the database |
| `Title` | `string` | `text`, `NOT NULL` | |
| `Description` | `string` | `text`, `NOT NULL` | |
| `PreparationTime` | `int` | `integer` | **Minutes** |
| `CookingTime` | `int` | `integer` | **Minutes** |
| `Servings` | `int` | `integer` | Count of portions |
| `Ingredients` | `IReadOnlyList<string>` | `text[]`, `NOT NULL` | Native PostgreSQL array. Free text, e.g. `"Flour"` |
| `Instructions` | `IReadOnlyList<string>` | `text[]`, `NOT NULL` | Ordered steps as free text; order = array order |

Table: `"Recipes"` (quoted — PostgreSQL folds unquoted identifiers to lowercase). Single migration
`20260725173218_InitialCreate`. No indexes beyond the PK, no unique constraint on `Title` —
**duplicate titles are allowed**.

### Lifecycle

- `public static Result<Recipe> Create(title, description, preparationTime, cookingTime, servings, ingredients, instructions)`
  — the only way to build a valid recipe. Validates first, then constructs.
- `public Result Update(...)` — same parameter list minus id; validates first and **mutates nothing if
  validation fails** (verified by `Update_WithInvalidData_ShouldNotUpdatePropertiesAndReturnFailure`).
- `private Recipe()` exists solely for EF Core materialisation and is wrapped in
  `#pragma warning disable CS8618`. Never call it, never make it public.
- There is **no delete method** on the entity; deletion is a repository concern (hard delete, no soft-delete
  flag, no audit trail).

## Invariants (`Recipe.ValidateProperties`)

All violations are collected — a single call can return several errors at once.

| Rule | Error factory | HTTP code | `field` |
| --- | --- | --- | --- |
| `Title` not null/whitespace | `TitleRequired()` | 422 | `title` |
| `Description` not null/whitespace | `DescriptionRequired()` | 422 | `description` |
| `PreparationTime >= 0` | `PreparationTimeNegative()` | 422 | `preparationTime` |
| `CookingTime >= 0` | `CookingTimeNegative()` | 422 | `cookingTime` |
| **Not both times zero** | `BothTimesZero()` | 422 | `preparationTime,cookingTime` |
| `Servings >= 1` | `ServingsOutOfRange(1)` | 422 | `servings` (+ `min` metadata) |
| At least one ingredient | `IngredientsRequired()` | 422 | `ingredients` |
| No blank ingredient string | `IngredientEmpty()` | 422 | `ingredients` |
| At least one instruction | `InstructionsRequired()` | 422 | `instructions` |
| No blank instruction string | `InstructionEmpty()` | 422 | `instructions` |
| Recipe exists (repository-level, not in the entity) | `RecipeNotFound(id)` | 404 | `id` |

Note: an empty-but-present ingredients list produces `IngredientsRequired`; a non-empty list containing a blank
string produces `IngredientEmpty`. They are mutually exclusive.

## Shape validation (FluentValidation, `RecipeValidationRules`)

Complementary to the invariants — bounds and null-safety only, applied **before** the handler runs.

| Field | Rule |
| --- | --- |
| `Title` | `NotNull`, `MaximumLength(200)` |
| `Description` | `NotNull`, `MaximumLength(1000)` |
| `PreparationTime` | `>= 0`, `< 1440` (24 h) |
| `CookingTime` | `>= 0`, `< 1440` (24 h) |
| `Servings` | `> 0`, `< 1000` |
| `Ingredients` | `NotNull`, at most 50 items |
| `Instructions` | `NotNull`, at most 50 items |

Consequence to remember: `Title = ""` passes FluentValidation (`NotNull` is satisfied) and is rejected by the
**domain** with 422. `Title = null` is rejected by FluentValidation with 400. The length caps (200/1000) exist
only in FluentValidation, not in the database — the columns are unbounded `text`.

## Application-layer types

| Type | Shape | Used by |
| --- | --- | --- |
| `CreateRecipeCommand` | Title, Description, PreparationTime, CookingTime, Servings, `List<string>` Ingredients, Instructions → `Result<RecipeDto>` | `POST /api/recipes` request body |
| `UpdateRecipeCommand` | `Guid Id` + the same seven fields → `Result` | built in the controller from route id + `UpdateRecipeDto` |
| `DeleteRecipeCommand` | `Guid Id` → `Result` | `DELETE /api/recipes/{id:guid}` |
| `GetAllRecipesQuery` | *(empty)* → `IEnumerable<RecipeDto>` | `GET /api/recipes` |
| `GetRecipeByIdQuery` | `Guid Id` → `Result<RecipeDto>` | `GET /api/recipes/{id}` |
| `RecipeDto` | `Id` + the seven fields, collections as `List<string>` | every response body |
| `UpdateRecipeDto` | the seven fields, no id | `PUT /api/recipes/{id:guid}` request body |

Mapping is the hand-written `RecipeMappingExtensions.MapToRecipeDto()`. Entity → DTO only; there is **no**
DTO → entity mapper (commands are passed as loose arguments to `Recipe.Create`/`Update`).

## HTTP surface

Base route `api/[controller]` ⇒ `/api/recipes` (matching is case-insensitive; the frontend calls `/Recipes`).

| Verb | Route | Success | Failures |
| --- | --- | --- | --- |
| GET | `/api/recipes` | 200 + `RecipeDto[]` (empty array when none) | — no `Result`, no error path |
| GET | `/api/recipes/{id}` | 200 + `RecipeDto` | 404 not found |
| POST | `/api/recipes` | 201 + `Location` + `RecipeDto` | 400 shape, 422 invariants |
| PUT | `/api/recipes/{id:guid}` | **204 No Content** (no body) | 400 shape, 404 not found, 422 invariants |
| DELETE | `/api/recipes/{id:guid}` | 204 No Content | 404 not found |

`GET /api/recipes` returns **all** recipes — no pagination, filtering, sorting, or projection. Search is done
client-side.

## Caching keys

| Key | Written by | Invalidated by |
| --- | --- | --- |
| `recipes_all` | `GetAllAsync` (10 min abs / 5 min sliding) | `AddAsync`, `UpdateAsync`, `DeleteAsync` |
| `recipe_{guid}` | `GetByIdAsync` (10/5), `AddAsync` (30 min abs / 15 min sliding) | `UpdateAsync`, `DeleteAsync` |

## Frontend view of the domain — currently out of sync

`recipe-manager-frontend/src/types/recipe.ts`:

```ts
export interface Recipe {
  id: number;              // ❌ backend returns a Guid (string)
  title: string;
  description: string;
  preparationTime: number;
  cookingTime: number;
  ingredients: string[];
  image?: string;          // ❌ no such field on RecipeDto
}
// ❌ missing: servings, instructions
```

`recipeService.ts` types `getRecipeById(id: number)`, `updateRecipe(id: number, …)`, and
`deleteRecipe(id: number)` — all wrong against a `Guid` API. `RecipeCard` always falls back to the bundled
placeholder image because `recipe.image` is permanently `undefined`.

This is a live defect, not a design choice. Owner: [agents/08-api-contract.md](agents/08-api-contract.md).

## Known limitations

Read these before proposing any recipe feature.

1. **Ingredients are unstructured strings.** No quantity, no unit, no ingredient catalogue. `text[]` *is*
   queryable in PostgreSQL, but nothing uses that — `RecipeList.tsx` filters in the browser over the full list.
   **This is a temporary shortcut with a decided replacement** — see [Target model](#target-model--where-the-domain-is-going).
2. **Instructions are unstructured strings.** No per-step duration, image, or grouping.
3. **No units of measure, no internationalisation.** Times are bare `int` minutes; no locale, no metric/imperial
   handling.
4. **No ownership or multi-tenancy.** No `User`, no `OwnerId`, no auth — every recipe is public and anyone can
   edit or delete any recipe.
5. **No versioning, no soft delete, no audit fields** (`CreatedAt`, `UpdatedAt` do not exist).
6. **No images.** `RecipeDto` has no image field and there is no upload endpoint.
7. **No categories, tags, ratings, or favourites.**
8. **No concurrency control.** No `xmin`/`RowVersion` mapping; concurrent `PUT`s are last-write-wins.
9. **No pagination** on `GET /api/recipes`; the whole table is loaded, mapped, and cached in one memory entry.
10. **No transaction boundary beyond one repository call** — see ADR-006 in [architecture.md](architecture.md).
11. **No length limits in the database.** `text` columns are unbounded; the 200/1000-character caps live only in
    FluentValidation, so anything bypassing the API can store arbitrarily large values.

Any feature touching items 1–4 is an **architecture decision first** — route it to
[agents/01-architect.md](agents/01-architect.md) before writing code.

---

## Target model — where the domain is going

The current shape is not the intended end state. These directions are **decided**; the design detail is not.

### Structured ingredients (`R-10`, decided)

`Ingredients` as `IReadOnlyList<string>` was an acknowledged temporary shortcut, not a design choice. The
project intends to replace it with structured data:

- An `Ingredient` value object or entity carrying at minimum `Quantity`, `Unit`, and `Name`.
- A `Unit` value object or enum covering metric and imperial, with an explicit conversion policy and a
  canonical stored unit.
- A decision on whether an ingredient **catalogue** exists (shared across recipes, enabling "what can I cook
  with X") or ingredients remain owned by their recipe.
- A migration strategy for existing `text[]` rows, which cannot be parsed into structured data reliably.

**Consequences for anyone working today:**

- **Do not build features that entrench free-text ingredients.** Client-side substring filtering, ad-hoc
  parsing of `"200g flour"`, or UI that assumes one string per row all become rework.
- Anything needing quantities — serving scaling, shopping lists, nutrition — is **blocked** on this, not
  merely awkward. Say so rather than implementing a string-parsing workaround.
- `RecipeDto` will change, so `R-09` (generated TS types) should land first.

Consider the same treatment for `Instructions` (per-step duration, image, grouping) — decide together with
ingredients, implement separately.

### Ownership (`R-14`, on the deploy gate)

There is no `User` and no `OwnerId` today. Adding them means a new aggregate, a column on `Recipe` with a
migration for existing rows, and an ownership filter applied in **every** query — not in the UI. Anything
phrased as "my recipes", "private", or "share" depends on this.

### Pagination (`R-11`)

`GET /api/recipes` returning the whole table is a placeholder, not a decision. Design the pagination contract
and the cache-key strategy together — paginating invalidates the single-key `recipes_all` approach.

Full detail and sequencing: [roadmap.md](roadmap.md).
