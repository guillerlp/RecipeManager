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
| `Ingredients` | `IReadOnlyList<string>` | `text[]`, `NOT NULL` | Native PostgreSQL array. Free text, e.g. `"Flour"`. Read-only only when built via `Create`/`Update`, not when loaded ([BUG-11](known-issues.md#bug-11)) |
| `Instructions` | `IReadOnlyList<string>` | `text[]`, `NOT NULL` | Ordered steps as free text; order = array order. Same read-only gap ([BUG-11](known-issues.md#bug-11)) |

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

| Rule | Error factory | Kind (→ HTTP) | `field` |
| --- | --- | --- | --- |
| `Title` not null/whitespace | `TitleRequired()` | `Validation` (422) | `title` |
| `Description` not null/whitespace | `DescriptionRequired()` | `Validation` (422) | `description` |
| `PreparationTime >= 0` | `PreparationTimeNegative()` | `Validation` (422) | `preparationTime` |
| `CookingTime >= 0` | `CookingTimeNegative()` | `Validation` (422) | `cookingTime` |
| **Not both times zero** | `BothTimesZero()` | `Validation` (422) | `preparationTime,cookingTime` |
| `Servings >= 1` | `ServingsOutOfRange(1)` | `Validation` (422) | `servings` (+ `min` metadata) |
| At least one ingredient | `IngredientsRequired()` | `Validation` (422) | `ingredients` |
| No blank ingredient string | `IngredientEmpty()` | `Validation` (422) | `ingredients` |
| At least one instruction | `InstructionsRequired()` | `Validation` (422) | `instructions` |
| No blank instruction string | `InstructionEmpty()` | `Validation` (422) | `instructions` |
| Recipe exists (repository-level, not in the entity) | `RecipeNotFound(id)` | `NotFound` (404) | `id` |

Note: an empty-but-present ingredients list produces `IngredientsRequired`; a non-empty list containing a blank
string produces `IngredientEmpty`. They are mutually exclusive.

The kind is the domain's; the status in brackets is applied by `ResultExtensions` in the API layer.

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

## Frontend view of the domain

`recipe-manager-frontend/src/types/recipe.ts` aliases the generated contract types (`R-09`/ADR-019):

```ts
// src/types/recipe.ts
// Aliases over the generated contract (R-09 / ADR-019). Never add fields here: change the C# DTO, then
// regenerate (see README, "Changing the API contract").
import type { components } from './generated/api';

type Schemas = components['schemas'];

export type Recipe = Schemas['RecipeDto'];
export type CreateRecipeRequest = Schemas['CreateRecipeCommand'];
export type UpdateRecipeRequest = Schemas['UpdateRecipeDto'];
```

`PUT` and `DELETE` return 204, so their service methods return `AxiosResponse<void>`. The shape is generated
from the OpenAPI snapshot, so drift fails CI (ADR-019). Owner: [agents/08-api-contract.md](agents/08-api-contract.md).

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

### Structured ingredients (`R-10`, designed — ADR-022, not yet built)

`Ingredients` as `IReadOnlyList<string>` was an acknowledged temporary shortcut, not a design choice. The shape
that replaces it is **settled** as of 2026-09-24 in
[specs/010-structured-ingredients.md](specs/010-structured-ingredients.md) and **ADR-022**, which supersedes
ADR-004. The tables above still describe the code; this describes where it is going:

- `Ingredient` is an **owned entity** deriving from `Entity`, with its own `Guid Id`: `Position` (`int`),
  `Quantity` (`decimal?`), `Unit` (`Unit?`), `Name` (`string`), `Notes` (`string?`). It is persisted to a
  `RecipeIngredients` child table via `OwnsMany` and is never addressable outside its recipe.
- `Unit` is a **closed C# enum** — `Gram, Kilogram, Ounce, Pound, Millilitre, Litre, Teaspoon, Tablespoon, Cup,
  FluidOunce, Piece, Clove, Pinch, Slice, Can, Bunch, Sprig` — stored as a string. `Unit?` null means "no unit";
  there is deliberately no `None` member.
- **Conversion is a presentation concern.** There is no canonical stored unit and no domain converter; the
  database keeps what was entered. `R-16`'s metric/imperial preference is satisfied on the client in `R-18`.
- **No ingredient catalogue** (settled 2026-09-19). Ingredients are owned by their recipe, so `Recipe` remains
  the only aggregate root and ADR-006 is untouched.
- **Order needs an explicit `Position`.** `text[]` preserved order for free; a relational child table does not.
- Existing `text[]` rows migrate **losslessly** as name-only ingredients — possible only because `Quantity` and
  `Unit` are optional, which "salt to taste" required independently. They are **not** parsed.
- New invariants: name non-blank (`IngredientNameRequired`), `Quantity > 0` when present
  (`IngredientQuantityNotPositive`), and a `Unit` requires a `Quantity` (`IngredientUnitWithoutQuantity`).
  `IngredientEmpty()` is removed.

**Consequences for anyone working today:**

- **Do not build features that entrench free-text ingredients.** Client-side substring filtering, ad-hoc
  parsing of `"200g flour"`, or UI that assumes one string per row all become rework.
- Anything needing quantities — serving scaling, shopping lists, nutrition — is **blocked** on this, not
  merely awkward. Say so rather than implementing a string-parsing workaround.
- `RecipeDto` will change: `List<string>` becomes `List<IngredientDto>`, and `IngredientDto` carries the id the
  client must round-trip on update or step references break. `R-09` (shipped, ADR-019) will flag every client
  site the change touches.

`Instructions` get the same treatment, and ADR-022 fixes their shape too: `InstructionStep` with `Position`,
`Text`, `DurationMinutes` (`int?`), and `IngredientIds` (`Guid[]`, mapped to `uuid[]`). Step-to-ingredient
integrity is a domain invariant, not a foreign key. Implemented as `R-17`.

### Also decided, not yet designed (2026-09-19)

Brought in by the editorial design ([agents/07-ux-ui.md](agents/07-ux-ui.md#canonical-design-reference)). None
of these exists today. Each needs its own ADR before code:

- **Draft recipes** (`R-19`): a Draft/Published status. A draft needs only a title, and the invariants above
  apply on publish. Until this ships, the invariants table is the whole truth (`UX-05`).
- **Tags** (`R-20`): freeform labels on a recipe.
- **Cook log** (`R-22`): a record of each time a recipe was cooked. If it becomes a separate aggregate, it forces
  the unit-of-work decision (ADR-006).

### Ownership (`R-14`, on the deploy gate)

There is no `User` and no `OwnerId` today. Adding them means a new aggregate, a column on `Recipe` with a
migration for existing rows, and an ownership filter applied in **every** query — not in the UI. Anything
phrased as "my recipes", "private", or "share" depends on this.

### Pagination (`R-11`)

`GET /api/recipes` returning the whole table is a placeholder, not a decision. Design the pagination contract
and the cache-key strategy together — paginating invalidates the single-key `recipes_all` approach.

Full detail and sequencing: [roadmap.md](roadmap.md).
