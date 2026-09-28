# Domain model

**Mandatory reading for every agent before touching code.**

The domain today contains **exactly one aggregate root: `Recipe`**, with two **owned entities** inside it —
`Ingredient` and `InstructionStep` — and one enum, `Unit` (ADR-022, ADR-023). There is no `Category`, `Tag`, or
`User` entity. Do not assume otherwise.

Some of that is deliberate; some is a shortcut with a decided replacement. The
[Target model](#target-model--where-the-domain-is-going) section at the end says which is which — read it
before designing anything that touches instructions, ownership, or listing behaviour.

```
Entity (abstract, RecipeManager.Domain/Shared/Entity.cs)
  Guid Id            protected init
  Equals             concrete type + Id
  GetHashCode        Id only
    ▲
    ├── Recipe (sealed, RecipeManager.Domain/Entities/Recipe.cs)       ← the only aggregate root
    │     ├── owns many ── Ingredient
    │     └── owns many ── InstructionStep ──references by id──▶ Ingredient (same recipe only)
    ├── Ingredient (sealed, RecipeManager.Domain/Entities/Ingredient.cs)
    │     owned by Recipe, never addressable on its own
    └── InstructionStep (sealed, RecipeManager.Domain/Entities/InstructionStep.cs)
          owned by Recipe, never addressable on its own
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
| `Ingredients` | `IReadOnlyList<Ingredient>` | child table `"RecipeIngredients"` | Owned collection over a private `List<Ingredient>` backing field, exposed sorted by `Position`. Genuinely read-only however it was obtained — the getter returns a fresh sorted copy |
| `Instructions` | `IReadOnlyList<InstructionStep>` | child table `"RecipeInstructionSteps"` | Owned collection over a private `List<InstructionStep>` backing field, exposed sorted by `Position` — same shape as `Ingredients`, and just as genuinely read-only |

Table: `"Recipes"` (quoted — PostgreSQL folds unquoted identifiers to lowercase). Four migrations:
`20260725173218_InitialCreate`, `20260924130146_StructureIngredients` (ADR-022),
`20260924153243_SyncIdValueGeneration`, which carries no DDL and exists only to bring the model snapshot back in
step with the corrected key metadata, and `20260926113324_StructureInstructions` (`R-17`), which moved the old
`text[]` into the steps table. No indexes on `"Recipes"` beyond the PK, no unique constraint on `Title` —
**duplicate titles are allowed**.

## `Ingredient`

An **owned entity** of `Recipe` (ADR-022): it derives from `Entity` and has its own `Guid Id`, but it is not an
aggregate root and is never addressable outside its recipe. Every write is still one repository call, so ADR-006
(no unit of work) is untouched.

| Property | C# type | PostgreSQL column | Notes |
| --- | --- | --- | --- |
| `Id` | `Guid` | `uuid`, PK | Minted in `Ingredient.Create` when the caller supplies none. Mapped `ValueGeneratedNever()` — see ADR-022's appended consequences |
| `RecipeId` | — | `uuid`, FK → `"Recipes"("Id")`, cascade delete, indexed | Shadow property; owned types get it from EF |
| `Position` | `int` | `integer`, `NOT NULL` | Assigned by `Recipe` from list order through an `internal` setter. A child table has no inherent row order |
| `Quantity` | `decimal?` | `numeric(9,3)`, nullable | Null for "salt to taste" |
| `Unit` | `Unit?` | `varchar(20)`, nullable | Enum member **name**, not its ordinal, so reordering the enum cannot silently remap stored rows |
| `Name` | `string` | `varchar(200)`, `NOT NULL` | Bounded in the database *and* in FluentValidation — the first field in this codebase to be both |
| `Notes` | `string?` | `varchar(200)`, nullable | |

`Unit` (`RecipeManager.Domain/Entities/Unit.cs`) is a **closed** enum: `Gram, Kilogram, Ounce, Pound, Millilitre,
Litre, Teaspoon, Tablespoon, Cup, FluidOunce, Piece, Clove, Pinch, Slice, Can, Bunch, Sprig`. There is
deliberately no `None` member — `Unit?` null already means "no unit", and two ways to express nothing is a defect
generator. Adding a unit is a code-and-deploy change, not a data change.

`public static Result<Ingredient> Create(Guid? id, decimal? quantity, Unit? unit, string name, string? notes)`
is the only way to build one, and it collects every violation in one call, like `Recipe.Create`.

## `InstructionStep`

The second **owned entity** of `Recipe` (ADR-022, built as `R-17`). Same pattern as `Ingredient`: derives from
`Entity`, owned, never addressable outside its recipe.

| Property | C# type | PostgreSQL column | Notes |
| --- | --- | --- | --- |
| `Id` | `Guid` | `uuid`, PK | **Always** minted in `InstructionStep.Create` — no caller supplies one, because nothing references a step yet. Mapped `ValueGeneratedNever()`, and re-minted on every `PUT` |
| `RecipeId` | — | `uuid`, FK → `"Recipes"("Id")`, cascade delete, indexed | Shadow property |
| `Position` | `int` | `integer`, `NOT NULL` | Assigned by `Recipe` from list order through an `internal` setter |
| `Text` | `string` | `varchar(2000)`, `NOT NULL` | Bounded in the database *and* in FluentValidation |
| `DurationMinutes` | `int?` | `integer`, nullable | Null for a step with no timer — most of them |
| `IngredientIds` | `IReadOnlyList<Guid>` | `uuid[]`, `NOT NULL` | Which of **this recipe's** ingredients the step uses, in the author's order. A primitive collection over a private `List<Guid>`, returned as a read-only copy. **No foreign key** — integrity is the `InstructionIngredientNotFound` invariant below |

`public static Result<InstructionStep> Create(string text, int? durationMinutes, IEnumerable<Guid>? ingredientIds)`
is the only way to build one. It cannot check that the ids belong to the recipe — it does not know the
recipe — so that rule lives in `Recipe.ValidateProperties`.

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
| At least one instruction | `InstructionsRequired()` | `Validation` (422) | `instructions` |
| Every step's `IngredientIds` is an id of **this** recipe's ingredients (reported once per recipe) | `InstructionIngredientNotFound()` | `Validation` (422) | `instructions` |
| Recipe exists (repository-level, not in the entity) | `RecipeNotFound(id)` | `NotFound` (404) | `id` |

Three further invariants live in `Ingredient.ValidateProperties`, because they are properties of one ingredient
rather than of the recipe. They are collected the same way, so several bad ingredients report together:

| Rule | Error factory | Kind (→ HTTP) | `field` |
| --- | --- | --- | --- |
| `Name` not null/whitespace | `IngredientNameRequired()` | `Validation` (422) | `ingredients` |
| `Quantity > 0` when present | `IngredientQuantityNotPositive()` | `Validation` (422) | `ingredients` |
| A `Unit` without a `Quantity` is meaningless | `IngredientUnitWithoutQuantity()` | `Validation` (422) | `ingredients` |

`IngredientEmpty()` **no longer exists** (ADR-022): a blank-named `Ingredient` cannot be constructed at all, so
the rule moved into `Ingredient.Create` as `IngredientNameRequired()`. That leaves `IngredientsRequired` as the
only ingredient invariant `Recipe` itself still enforces.

Two more live in `InstructionStep.ValidateProperties`, for the same reason:

| Rule | Error factory | Kind (→ HTTP) | `field` |
| --- | --- | --- | --- |
| `Text` not null/whitespace | `InstructionTextRequired()` | `Validation` (422) | `instructions` |
| `DurationMinutes > 0` when present | `InstructionDurationNotPositive()` | `Validation` (422) | `instructions` |

`InstructionEmpty()` **no longer exists** (`R-17`) — the same move ADR-022 made for ingredients: a blank-text
step cannot be constructed, so the rule moved into `InstructionStep.Create` as `InstructionTextRequired()`.

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
| `Ingredients` | List: `NotNull`, at most 50 items. Per item (`IngredientInputDtoValidator`): `Name` `NotNull` + `MaximumLength(200)`, `Notes` `MaximumLength(200)`, `Quantity` `InclusiveBetween(0, 100000)` when present |
| `Instructions` | List: `NotNull`, at most 50 items. Per item (`InstructionStepInputDtoValidator`): `Text` `NotNull` + `MaximumLength(2000)`; `DurationMinutes` `>= 0` and `< 1440` when present; `IngredientIndexes` `NotNull`, at most 50 items, each `>= 0`, no duplicates |

Consequence to remember: `Title = ""` passes FluentValidation (`NotNull` is satisfied) and is rejected by the
**domain** with 422. `Title = null` is rejected by FluentValidation with 400. The same split applies to
`DurationMinutes = 0` (422 from the domain) versus `-1` (400 from the validator): "positive" is a business rule,
the bound is shape. The `Title`/`Description` caps (200/1000) exist only in FluentValidation, not in the
database — those columns are unbounded `text` ([SEC-08](known-issues.md#sec-08)). The ingredient and step caps
are the exception: they are enforced in both places.

## Application-layer types

| Type | Shape | Used by |
| --- | --- | --- |
| `CreateRecipeCommand` | Title, Description, PreparationTime, CookingTime, Servings, `List<IngredientInputDto>` Ingredients, `List<InstructionStepInputDto>` Instructions → `Result<RecipeDto>` | `POST /api/recipes` request body |
| `UpdateRecipeCommand` | `Guid Id` + the same seven fields → `Result` | built in the controller from route id + `UpdateRecipeDto` |
| `DeleteRecipeCommand` | `Guid Id` → `Result` | `DELETE /api/recipes/{id:guid}` |
| `GetAllRecipesQuery` | *(empty)* → `IEnumerable<RecipeDto>` | `GET /api/recipes` |
| `GetRecipeByIdQuery` | `Guid Id` → `Result<RecipeDto>` | `GET /api/recipes/{id}` |
| `RecipeDto` | `Id` + the seven fields, with `List<IngredientDto>` Ingredients and `List<InstructionStepDto>` Instructions | every response body |
| `UpdateRecipeDto` | the seven fields, no id; `List<IngredientInputDto>` Ingredients, `List<InstructionStepInputDto>` Instructions | `PUT /api/recipes/{id:guid}` request body |
| `IngredientDto` | `(Guid Id, decimal? Quantity, Unit? Unit, string Name, string? Notes)` | inside every response body |
| `IngredientInputDto` | `(Guid? Id, decimal? Quantity, Unit? Unit, string Name, string? Notes)` | inside `POST`/`PUT` request bodies |
| `InstructionStepDto` | `(Guid Id, string Text, int? DurationMinutes, List<Guid> IngredientIds)` | inside every response body |
| `InstructionStepInputDto` | `(string Text, int? DurationMinutes, List<int> IngredientIndexes)` | inside `POST`/`PUT` request bodies |

`Position` is deliberately **not** in any of these DTOs: the JSON list is already ordered, and exposing an
index the client must keep consistent with array order invites the two disagreeing.

**Step references are asymmetric on purpose (ADR-023).** A request says which ingredients a step uses by
**index into the same request's `ingredients` array** (`IngredientIndexes`); the response says it by **id**
(`IngredientIds`). On create the ingredients have no ids yet, so an index is the only reference a client can
make. `InstructionMappingExtensions.ToInstructionSteps` resolves indexes to the ids `Ingredient.Create` has just
minted; an index outside the array is a 422 with `field: "instructions"`. Every writer must translate ids back
to indexes against the list it is about to send.

`IngredientInputDto.Id` is always null on create. On update it is the client echoing back an id the API gave it,
so the ingredient keeps its identity across the edit; a null `Id` on update means "this ingredient is new". Step
references do **not** depend on the echo — they are re-resolved from indexes on every write. Nothing currently
checks that a supplied id actually belongs to the recipe being updated — [BUG-15](known-issues.md#bug-15).

`Unit` crosses the wire as a string (`"Tablespoon"`), because `JsonStringEnumConverter` is registered in
`RecipeManager.Api/Startup/ServiceInitializer.cs`. An unrecognised value is rejected at model binding with 400.

Mapping is the hand-written `RecipeMappingExtensions.MapToRecipeDto()` (plus `MapToIngredientDto()`). Entity →
DTO only; the one exception is `IngredientMappingExtensions.ToIngredients()`, which turns a list of
`IngredientInputDto` into domain `Ingredient`s through `Ingredient.Create` — it is a validating factory call
rather than a mapper, and it collects every failure instead of stopping at the first. Recipe commands are still
passed as loose arguments to `Recipe.Create`/`Update`.

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
export type Ingredient = Schemas['IngredientDto'];
export type IngredientInput = Schemas['IngredientInputDto'];
export type InstructionStep = Schemas['InstructionStepDto'];
export type InstructionStepInput = Schemas['InstructionStepInputDto'];
```

`Ingredient`, `IngredientInput`, `InstructionStep`, and `InstructionStepInput` are genuinely new generated
schemas (ADR-022, ADR-023), so aliasing them is legal under ADR-019 — none is a hand-written field. `Ingredient['unit']` is nullable, which took a Swashbuckle
schema filter to express: see ADR-022's appended consequences.

`PUT` and `DELETE` return 204, so their service methods return `AxiosResponse<void>`. The shape is generated
from the OpenAPI snapshot, so drift fails CI (ADR-019). Owner: [agents/08-api-contract.md](agents/08-api-contract.md).

## Known limitations

Read these before proposing any recipe feature.

1. **Instruction steps have no grouping and no images.** Steps carry text, an optional duration, and ingredient
   references (ADR-022, `R-17`); "For the sauce:" sections and per-step photos are not modelled.
2. **No ingredient catalogue and no internationalisation.** Ingredients are owned by their recipe, so "tomato"
   and "tomatoes" are unrelated names with nothing to join on (settled 2026-09-19). Times are bare `int`
   minutes; no locale. `Unit` has no server-side conversion: the SPA converts for display only (ADR-022,
   ADR-024 item 6), and nothing converted is stored.
3. **No ownership or multi-tenancy.** No `User`, no `OwnerId`, no auth — every recipe is public and anyone can
   edit or delete any recipe.
4. **No versioning, no soft delete, no audit fields** (`CreatedAt`, `UpdatedAt` do not exist).
5. **No images.** `RecipeDto` has no image field and there is no upload endpoint.
6. **No categories, tags, ratings, or favourites.**
7. **No concurrency control.** No `xmin`/`RowVersion` mapping; concurrent `PUT`s are last-write-wins.
8. **No pagination** on `GET /api/recipes`; the whole table is loaded, mapped, and cached in one memory entry.
9. **No transaction boundary beyond one repository call** — see ADR-006 in [architecture.md](architecture.md).
10. **No length limits on `Title` and `Description` in the database.** Those `text` columns are unbounded and the
    200/1000-character caps live only in FluentValidation, so anything bypassing the API can store arbitrarily
    large values. The ingredient and instruction-step columns are the exception — bounded in both places.

Item 1 used to read "Ingredients / Instructions are unstructured strings". ADR-022 and
[specs/010-structured-ingredients.md](specs/010-structured-ingredients.md) closed the ingredient half on
2026-09-24; `R-17` and [specs/011-structured-instructions.md](specs/011-structured-instructions.md) closed the
instruction half on 2026-09-26, leaving only what the item says now. Any feature touching items
1–3 is an **architecture decision first** — route it to [agents/01-architect.md](agents/01-architect.md) before
writing code.

---

## Target model — where the domain is going

The current shape is not the intended end state. These directions are **decided**; the design detail is not.

Structured ingredients and structured instructions used to head this section. They **shipped** on 2026-09-24
(ADR-022, [specs/010-structured-ingredients.md](specs/010-structured-ingredients.md)) and 2026-09-26 (`R-17`,
ADR-023, [specs/011-structured-instructions.md](specs/011-structured-instructions.md)), and are described as
current state in the tables above; nothing about them belongs here any more. Step references are what cooking
mode's "For this step" and "Already used" lists (`R-23`) are built from.

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
