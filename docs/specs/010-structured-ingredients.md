# Spec: Structured ingredients

| | |
| --- | --- |
| **ID** | `010` |
| **Status** | implemented — CI green (run 36022927670), awaiting merge. Two §-level **corrections** were made on 2026-09-24 during implementation; both are marked in place, in §6 and §7 |
| **Author** | `00-leader` + `01-architect` |
| **Created** | `2026-09-24` |
| **Branch** | `docs/structured-ingredients-spec` (this spec), then `feat/structured-ingredients` (implementation) |

---

## 1. Context

`Recipe.Ingredients` is `IReadOnlyList<string>` of free text — an acknowledged temporary shortcut recorded in
ADR-004 and confirmed as such on 2026-07-26. Because there is no quantity and no unit, serving scaling, unit
conversion, and shopping lists are impossible rather than merely awkward, and "recipes containing tomato" is a
client-side substring scan over the whole table in
`recipe-manager-frontend/src/components/ui/Recipe/RecipeList/RecipeList.tsx`.

Five roadmap items wait on this one. `R-17`, `R-18`, `R-19`, and `R-21` all assume quantities, units, and
per-step ingredient references; the editorial design's Detail, Add/Edit, and Cooking-mode screens are built on
them. This spec settles the ingredient shape **and** the `Instructions` shape, so that the latter is not decided
incrementally through a later feature.

## 2. Goal

Replace free-text ingredients with an ordered collection of structured `Ingredient` entities owned by `Recipe`,
and record the `InstructionStep` shape that `R-17` will implement.

## 3. In scope

- [x] `Ingredient` owned entity — `Position`, `Quantity`, `Unit`, `Name`, `Notes` — with a `Create` factory
      returning `Result<Ingredient>`
- [x] `Unit` enum (metric, imperial, count), persisted as a string
- [x] `Recipe.Ingredients` becomes `IReadOnlyList<Ingredient>` over a private backing field
- [x] New domain invariants and their `RecipeErrors` factories
- [x] New FluentValidation rules, including a per-item length cap
- [x] First `IEntityTypeConfiguration<Recipe>` and `OnModelCreating`, applied by assembly scan
- [x] Migration `StructureIngredients` with a lossless backfill from the existing `text[]`
- [x] `RecipeDto`, `CreateRecipeCommand`, `UpdateRecipeDto` carry `IngredientDto` / `IngredientInputDto`
- [x] Regenerated OpenAPI snapshot and TypeScript contract
- [x] `RecipeList.tsx` search keeps working against the new shape
- [x] ADR-022 recording the decision, including the `InstructionStep` shape for `R-17`

## 4. Out of scope

- **Implementing `InstructionStep`.** Its shape is decided in ADR-022; the code is `R-17`. `Instructions` stays
  `IReadOnlyList<string>` after this spec ships.
- **The ingredient line parser** ("2 tbsp butter, cold"). Its rules are recorded in ADR-022; it is built with the
  form in `R-21`.
- **Any conversion implementation.** ADR-022 fixes the policy — conversion is a presentation concern. The
  client-side table is written in `R-18`, where the metric/imperial preference first has a screen to act on.
- **Server-side ingredient search.** The child table makes it a tractable SQL query; designing the endpoint and
  its cache keys is `R-11`.
- **`Title` / `Description` length limits.** `OnModelCreating` arriving here makes them possible, but altering
  existing columns is a separate migration and stays with `SEC-08`.
- **The instruction half of `SEC-09`.** Per-item caps land for ingredients here and for instructions in `R-17`.
- **A recipe detail screen or an add/edit form.** `R-18` and `R-21`.

## 5. Decisions taken with the user (2026-09-24)

Four forks were open after reading [../roadmap.md](../roadmap.md) and
[../agents/01-architect.md](../agents/01-architect.md). All four were put to the user with their trade-offs; the
recommendation was taken in each case. The rejected options are argued in §9 rather than listed twice here.

| # | Fork | Decision |
| --- | --- | --- |
| 1 | `Unit` representation | Closed C# enum, not an open-set value object and not a string |
| 2 | Conversion policy and canonical stored unit | Store as entered; conversion is a presentation concern; there is no canonical unit |
| 3 | What `R-17`'s steps reference | `Ingredient` becomes an owned **entity** with a `Guid Id`, not a value object referenced by index |
| 4 | Persistence shape | `RecipeIngredients` child table via `OwnsMany`, not a `jsonb` column |

A fifth question — whether real data had to survive the migration — was answered "the database is empty". That
does not change the migration, which is written to be lossless regardless, but it does mean the backfill is
never exercised against accumulated data. Recorded in §14.

## 6. Domain impact

- **New/changed entities:** `Ingredient` (new, `RecipeManager.Domain/Entities/Ingredient.cs`) — a `sealed class`
  deriving from `Entity`, owned by `Recipe`, never addressable on its own. `Recipe` remains the only aggregate
  root, so ADR-006 is untouched and every write stays one repository call.
- **New/changed properties on `Recipe`:** `Ingredients` retyped from `IReadOnlyList<string>` to
  `IReadOnlyList<Ingredient>`, exposed over a private `List<Ingredient>` backing field and sorted by `Position`.
- **New enum:** `Unit` (`RecipeManager.Domain/Entities/Unit.cs`, beside `Ingredient` rather than in a new
  `Enums/` folder — the precedent is `ErrorKind`, which lives in `RecipeManager.Domain/Errors/` beside
  `DomainError`, so enums are filed by the concept they serve) — `Gram, Kilogram, Ounce, Pound, Millilitre,
  Litre, Teaspoon, Tablespoon, Cup, FluidOunce, Piece, Clove, Pinch, Slice, Can, Bunch, Sprig`. No `None`
  member: `Unit?` null already means "no unit", and two ways to express nothing is a defect generator.
- **New/changed invariants:**

  | Rule | Error factory | Kind (→ HTTP) | `field` |
  | --- | --- | --- | --- |
  | Ingredient name not null/whitespace | `IngredientNameRequired()` | `Validation` (422) | `ingredients` |
  | `Quantity > 0` when present | `IngredientQuantityNotPositive()` | `Validation` (422) | `ingredients` |
  | A `Unit` without a `Quantity` is meaningless | `IngredientUnitWithoutQuantity()` | `Validation` (422) | `ingredients` |

  `IngredientsRequired()` is unchanged. `IngredientEmpty()` is **removed**, superseded by
  `IngredientNameRequired()`.
- **New/changed shape validation:** `ValidateIngredients` is rewritten for `List<IngredientInputDto>` — list
  `NotNull` and at most 50 items as today, plus per item `Name` `NotNull` + `MaximumLength(200)`,
  `Notes` `MaximumLength(200)`, and `Quantity` `InclusiveBetween(0, 100000)` when present.
- **Migration required:** yes — `StructureIngredients`. Not destructive to data (the backfill is lossless), but
  it **drops a column**, so it is destructive to schema. `Down()` is lossy: quantities, units, and notes are
  discarded when the `text[]` is reconstructed. *As built, two migrations shipped, not one:*
  `20260924153243_SyncIdValueGeneration` carries **no DDL** and exists only to bring
  `AppDbContextModelSnapshot.cs` back in step after `ValueGeneratedNever()` was added to both `Guid` keys — see
  the §7 correction. Without it the next `dotnet ef migrations add` would silently fold that metadata diff into
  an unrelated migration.
- **Known limitations touched:** closes item 1 (unstructured ingredients). Item 2 (unstructured instructions)
  is *decided* here and closed by `R-17`. *Those are the numbers as they stood on 2026-09-24; after item 1 was
  deleted, instructions became item 1.*

### Existing rows

```sql
INSERT INTO "RecipeIngredients" ("Id","RecipeId","Position","Name","Quantity","Unit","Notes")
SELECT gen_random_uuid(), r."Id", t.ord - 1, t.elem, NULL, NULL, NULL
FROM "Recipes" r, unnest(r."Ingredients") WITH ORDINALITY AS t(elem, ord);
```

Every existing string becomes a name-only ingredient at its original index. This is lossless **only because**
`Quantity` and `Unit` are nullable — a requirement that "salt to taste" imposes independently. `"200g flour"`
migrates as `Name = "200g flour"`; it is not parsed. Best-effort parsing was rejected in
[../roadmap.md](../roadmap.md) and is not reconsidered here.

`Name` is capped at 200 characters while the old `text[]` elements were unbounded (`SEC-09`), so a pre-existing
string longer than 200 would survive the backfill but fail a later update. No such row can exist in practice —
`Title` and `Description` were the only unbounded fields anyone exercised — but the cap is deliberately generous
for this reason.

> **Correction, 2026-09-24 (found during implementation).** The sentence above is **wrong** and the
> implementation had to depart from it. Such a string would **not** survive the backfill: the `INSERT`
> writes unbounded `text` into `"RecipeIngredients"."Name"`, which is `character varying(200)`, so PostgreSQL
> raises `22001` (*value too long for type character varying(200)*) and aborts the whole migration
> transaction. Because `Program.cs` calls `app.MigrateDatabase()` at startup and there is no rollback
> procedure (`INFRA-03`), the API would then **crash-loop on boot** with an opaque error code and no
> indication of which row caused it.
>
> The shipped migration therefore carries a **pre-flight guard** ahead of the backfill — a
> `DO $$ … RAISE EXCEPTION … END $$;` block that scans `unnest(r."Ingredients")` for any element longer than
> 200 characters and aborts with a message naming the limit, the column, and the fact that the backfill will
> not truncate. The migration still fails, which is correct — the alternative is silent data loss — but it
> fails with something a human can act on. The generous cap remains the reason no such row is expected; the
> guard is what happens when the expectation is wrong.

## 7. API impact

- **New/changed endpoints:** none. All five routes keep their verbs, paths, and status codes.
- **New DTOs:** `IngredientDto` and `IngredientInputDto`, both positional `record`s in
  `RecipeManager.Application/DTO/Recipes/`, one file per type.
- **`RecipeDto` changes:** `List<string> Ingredients` becomes `List<IngredientDto> Ingredients`, where

  ```csharp
  public record IngredientDto(Guid Id, decimal? Quantity, Unit? Unit, string Name, string? Notes);
  ```

  `Position` is **not** in the DTO — the list is already ordered, so exposing an index the client must keep
  consistent with array order invites the two disagreeing.
- **`CreateRecipeCommand` / `UpdateRecipeDto` changes:** `List<string> Ingredients` becomes
  `List<IngredientInputDto> Ingredients`, where

  ```csharp
  public record IngredientInputDto(Guid? Id, decimal? Quantity, Unit? Unit, string Name, string? Notes);
  ```

  `Id` is always null on create. On update it is the client echoing back an id it was given, so step references
  (`R-17`) survive an edit. A null `Id` on update means "this ingredient is new".
- **Update semantics:** `Recipe.Update` **replaces** the collection wholesale rather than diffing it. Supplied
  ids are preserved; nulls mint new ones. EF deletes and re-inserts the owned rows, which is correct and cheap
  at 50 items or fewer.

  > **Correction, 2026-09-24 (found during implementation).** "EF deletes and re-inserts the owned rows" is
  > true only of a **change-tracked** graph. As first written, the update path loaded the recipe with
  > `AsNoTracking()` and called `DbSet.Update()` on the detached result, which marks the whole graph
  > `Modified` — so EF issued an `UPDATE` for every ingredient, including ones that did not exist yet, and
  > every `PUT` returned 500 with `DbUpdateConcurrencyException` ("expected to affect 1 row(s), but actually
  > affected 0").
  >
  > What the code actually does now: `UpdateRecipeHandler` loads through a dedicated
  > `IRecipeRepository.GetByIdForUpdateAsync`, which returns a **change-tracked** entity and deliberately
  > bypasses the cache (`CachedRecipeRepository` neither reads nor writes a cache entry for it, because a
  > cached instance is detached and shared across requests). `RecipeRepository.UpdateAsync` then calls only
  > `SaveChangesAsync` — no `Update()`, no `Attach()` — and throws `InvalidOperationException` if it is
  > handed a detached recipe, so the silent-no-op variant of this bug cannot come back unnoticed. EF's change
  > tracker computes the real inserts, updates, and deletes from the replaced collection.
- **Breaking for the client?** yes — `08-api-contract` ships `contracts/openapi.json` and
  `src/types/generated/api.ts` in the same PR.
- **Enum serialisation:** no change needed. `JsonStringEnumConverter` is already registered in
  `RecipeManager.Api/Startup/ServiceInitializer.cs:112`, so `Unit` crosses the wire as `"Tablespoon"`.
- **Cache impact:** none. Same keys (`recipes_all`, `recipe_{guid}`), same invalidation. Note that `BUG-14`
  (an update mutates the cached instance before it is persisted) is **unaffected and not fixed here** — it gets
  marginally more visible, since a failed save now leaves a structurally different object in the cache.

## 8. Frontend impact

- **New routes:** none.
- **New/changed components:** `RecipeList.tsx` only. Line 32's `...recipe.ingredients` becomes
  `...recipe.ingredients.map(i => i.name)` so the search haystack keeps working. Its test fixtures in
  `RecipeList.test.tsx` are updated to the new shape.
- **New/changed hooks:** none. `useRecipes` is unchanged; only the type it returns moves.
- **New type alias:** `recipe.ts` gains `export type Ingredient = Schemas['IngredientDto'];`. This is a genuinely
  new generated schema, so the alias is legal under ADR-019 — it is not a hand-written field.
- **States to design:** none new. Loading, error, empty, and populated are unchanged.
- **Design tokens needed:** existing only.

## 9. Architecture impact

- **ADR required:** yes — **ADR-022** in [../architecture.md](../architecture.md), superseding ADR-004.
- **New dependency:** none.
- **Layer/dependency changes:** none. `Ingredient` and `Unit` live in `Domain`, which still references no
  project.
- **New DI registrations:** none. No new handler, and `ApplyConfigurationsFromAssembly` is a `DbContext`
  concern, not an `IServiceCollection` one.
- **First `OnModelCreating`:** `AppDbContext` gains one, applying configurations from the Infrastructure
  assembly. `RecipeConfiguration` lands in `RecipeManager.Infrastructure/Context/Configurations/`, the folder
  [../conventions.md](../conventions.md) already reserves for it. `01-architect` clears this as ADR-022 requires.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| `Unit` as an open-set value object | Never needing a code change for "sprig" or "handful" | No compile-time safety, and normalising `tbsp`/`Tbsp`/`tablespoon` becomes runtime logic we own. It also leaves `R-21`'s parser with no fixed vocabulary, so "2 bananas butter" parses as `unit=bananas`. |
| `Unit` as a nullable string | The smallest possible diff | Defers exactly the work this item exists to do, while losing type safety. ADR-004's shortcut applied one level down. |
| Canonical base unit (g/ml), converted on write | Aggregation across recipes as a pure SQL problem | Mass-to-volume needs per-ingredient density, and the no-catalogue decision (2026-09-19) removed the place to store it. Round-trips are lossy: "2 tbsp" redisplays as "29.57 ml". |
| No conversion at all, ever | Smallest scope | The editorial design (3e) has a Units preference, and `R-18` builds it; this would mean dropping a designed control. *(Corrected 2026-09-26: `R-16` did not ship the toggle — spec 009 §4 left it out.)* |
| `Ingredient` as a pure value object, steps referencing it by array index | Clean value-object semantics and the smallest DTO | Reordering or deleting an ingredient silently corrupts every step reference — and `R-21` is specified with keyboard reorder. Requires reindex-on-write logic nothing enforces. |
| Deferring step references to `R-17` | Smallest diff today | Contradicts the roadmap, which makes settling `Instructions` here non-optional, and re-creates the incremental-arrival failure [../agents/01-architect.md](../agents/01-architect.md) warns against. |
| `jsonb` column via `OwnsMany(...).ToJson()` | One column, no join, atomic read and write of the whole recipe | EF's LINQ translation into JSON is limited, so `R-11`'s ingredient search would fall back to raw SQL or the client — re-creating the problem this item exists to fix. |
| Rational `(Numerator, Denominator)` quantity | Exact fractions: "1/3 cup" redisplays as "1/3" | Doubles the column count and complicates every comparison, to buy a display nicety the client can reconstruct from a decimal. |
| Best-effort parse of existing `text[]` during migration | No manual cleanup of old rows | Unreliable by construction, and irreversible: a wrong parse is indistinguishable from real data afterwards. Rejected in the roadmap. |

- **Pattern applied:** *owned entity within an aggregate* (EF Core `OwnsMany`) plus *static factory returning
  `Result<T>`* — existing example in this repo: `Recipe.Create` in
  `RecipeManager.Domain/Entities/Recipe.cs`, and identity equality in `RecipeManager.Domain/Shared/Entity.cs`.
- **What this makes harder:**
  - `RecipeDto` grows an id the client must round-trip. A form that rebuilds the ingredient list from scratch
    silently breaks every step reference, and nothing in the type system prevents it.
  - The exposed `Ingredients` getter allocates a sorted copy on every access. Irrelevant at 50 items or fewer,
    but it is no longer a field read.
  - Adding a unit is a code-and-deploy change, not a data change.
  - `Down()` is lossy, and `app.MigrateDatabase()` applies migrations at startup with no rollback procedure
    (`INFRA-03`). The implementation PR description must call this out.

### Ordering

A `text[]` preserved ingredient order as array order, for free. A relational child table has no inherent row
order — usually insertion order, never guaranteed, and reliably not insertion order after an update rewrites
the rows. `Ingredient` therefore carries an explicit `int Position`, assigned by the domain from list order in
`Create` and `Update`:

```csharp
private readonly List<Ingredient> _ingredients = [];
public IReadOnlyList<Ingredient> Ingredients => _ingredients.OrderBy(i => i.Position).ToList();
```

The backing field is also the idiomatic EF pattern for owned collections, and it **closes the ingredient half of
`BUG-11`**: EF populates the private field instead of assigning a mutable `List<string>` straight through a
`private set`. The instruction half of `BUG-11` stays open until `R-17`.

### `InstructionStep` — decided here, implemented as `R-17`

```csharp
public sealed class InstructionStep : Entity
{
    public int Position { get; private set; }
    public string Text { get; private set; }
    public int? DurationMinutes { get; private set; }
    public IReadOnlyList<Guid> IngredientIds { get; private set; }  // maps to uuid[]
}
```

Step-to-ingredient references are a `uuid[]` primitive collection, not a join table. Referential integrity is a
**domain** invariant — `Recipe.ValidateProperties` rejects any id not present in `Ingredients` — not a foreign
key, which is consistent with ingredients being owned and unaddressable from outside the aggregate.

### Ingredient line parser — rules recorded, built in `R-21`

The parser runs **on the client** and produces an `IngredientInputDto`; the server never parses free text.
Rules, in order, against one trimmed line:

1. A leading number — integer, decimal, `1/2`, or `1 1/2` — becomes `Quantity`. No leading number means
   `Quantity` and `Unit` are both null.
2. The next token is matched case-insensitively against a symbol table (`g`, `gram`, `grams`, `kg`, `tbsp`,
   `tablespoon`, and so on) mapping to a `Unit`. No match means `Unit` is null and the token belongs to `Name`.
3. Text after the first comma becomes `Notes`; the remainder is `Name`.
4. `Name` is required. A line that yields an empty `Name` is a parse failure shown inline, never submitted.

Examples: `2 tbsp butter, cold` gives `(2, Tablespoon, "butter", "cold")`; `salt to taste` gives
`(null, null, "salt to taste", null)`; `3 eggs` gives `(3, null, "eggs", null)`.

## 10. Security impact

- **New user-controlled input:** `Ingredient.Name`, `Ingredient.Notes`, `Quantity`, `Unit`. `Name` and `Notes`
  are bounded in FluentValidation *and* in the database (`HasMaxLength(200)`) — the first fields in this
  codebase bounded in both. `Quantity` is `numeric(9,3)`, so an oversized value is a 400, not a stored row.
  `Unit` is an enum, so an unrecognised value is rejected at model binding.
- **User content rendered in the SPA:** yes — ingredient names reach `RecipeList`'s search haystack. React
  escapes by default and nothing here uses `dangerouslySetInnerHTML`.
- **File upload:** no.
- **Auth/ownership implications:** none new. The app still has no authentication and no ownership; every recipe
  is public and anyone can edit any recipe (`SEC-01`, `SEC-02`). This feature neither improves nor worsens that.
- **Config/secrets touched:** none.
- **Standing gaps affected:** improves `SEC-09` (per-item caps, ingredient half). Partially unblocks `SEC-08` by
  introducing `OnModelCreating`, without closing it. `SEC-07` (unpaginated `GET /api/recipes`) is **worsened in
  degree**: each recipe's payload grows, so the single unpaginated response gets larger. `R-11` is the fix and
  is unchanged by this.

## 11. Acceptance criteria

- [x] Given a valid payload with `{"quantity": 2, "unit": "Tablespoon", "name": "butter", "notes": "cold"}`, when
      `POST /api/recipes`, then 201 and the response ingredient round-trips all four values plus a non-empty `id`.
- [x] Given a payload whose ingredient has `"name": "  "`, when `POST /api/recipes`, then 422 with
      `field: "ingredients"`.
- [x] Given a payload whose ingredient has `"quantity": 0`, when `POST /api/recipes`, then 422 with
      `field: "ingredients"`.
- [x] Given a payload whose ingredient has `"unit": "Gram"` and no `quantity`, when `POST /api/recipes`, then 422
      with `field: "ingredients"`.
- [x] Given a payload whose ingredient `name` is 201 characters, when `POST /api/recipes`, then 400.
- [x] Given a payload whose ingredient has `"unit": "Furlong"`, when `POST /api/recipes`, then 400.
- [x] Given a recipe created with ingredients in the order A, B, C, when `GET /api/recipes/{id}`, then they are
      returned in exactly that order.
- [x] Given a stored recipe, when `PUT` sends its ingredients reordered as C, A, B with their existing ids, then a
      subsequent `GET` returns C, A, B **and** the three ids are unchanged.
- [x] Given a stored recipe, when `PUT` sends one ingredient with `"id": null`, then that ingredient receives a new
      id and the others keep theirs.
- [x] Given a recipe with ingredients, when the recipe is deleted, then no orphan rows remain in
      `"RecipeIngredients"`.
- [ ] Given a `Recipe` materialised by EF, when its `Ingredients` is cast to `List<Ingredient>`, then the cast
      fails (`BUG-11`, ingredient half). **Not pinned by a test.** The property holds by construction — the
      getter returns a fresh `.OrderBy(...).ToList().AsReadOnly()` — but nothing asserts it, so a later refactor
      of that getter could remove the guarantee silently. Recorded in [BUG-11](../known-issues.md#bug-11); write
      the assertion for both collections when `R-17` closes the instruction half.
- [x] Given a database at `InitialCreate` with a recipe whose `Ingredients` is `{'flour','water'}`, when the
      migration is applied, then two rows exist at `Position` 0 and 1 with those names and null quantity, unit,
      and notes.

## 12. Test plan

- **Domain unit tests** (`RecipeManager.UnitTests/Domain/Entities/`): `Ingredient.Create` — valid, blank name,
  zero and negative quantity, unit-without-quantity, null quantity and unit both accepted. `Recipe.Create` and
  `Recipe.Update` — errors aggregate across several bad ingredients in one call; `Position` is assigned from
  list order; `Update` mutates nothing when validation fails (extend the existing
  `Update_WithInvalidData_ShouldNotUpdatePropertiesAndReturnFailure`).
- **Ordering tests:** asserted with `Should().Equal(...)`, never `BeEquivalentTo` — order is the property under
  test. This is `TEST-03`'s lesson applied prospectively to ingredients; `TEST-03` itself stays open for
  instructions until `R-17`.
- **Handler unit tests:** existing `CreateRecipeHandler` and `UpdateRecipeHandler` tests updated to the new
  shape. Success, not-found, validation failure, cancellation propagation, `Received(1)`.
- **Validator unit tests:** per-item name length, notes length, quantity bounds, list cap still at 50.
- **Integration tests** (`RecipesControllerTests`, real PostgreSQL via Testcontainers): create and read
  round-trip including ids and order; reorder-preserves-ids via `PUT`; cascade delete leaves no orphan rows.
  Each asserts database state after `DbContext.ChangeTracker.Clear()`.
- **Migration test:** apply `InitialCreate`, insert a row with a `text[]`, migrate to `StructureIngredients`,
  assert the backfilled rows. Runs on the Testcontainers database.
- **Contract tests:** `OpenApiContractTests` fails until the snapshot is regenerated — that is the intended
  signal, not a problem to work around.
- **Frontend:** `RecipeList.test.tsx` fixtures updated; the "matches on an ingredient" case must still pass,
  which is what proves the search haystack was migrated rather than dropped.
- **Not covered, and why:** the migration is verified against a container, **not** against a populated
  production database — there is none, and the developer database is empty (confirmed 2026-09-24). Instruction
  ordering (`TEST-03`) stays uncovered until `R-17`. `BUG-14` is not addressed and gains no test here.
- **Manual verification:** run the API against local PostgreSQL, create a recipe through Swagger with a mix of
  quantified, unitless, and "to taste" ingredients, confirm
  `SELECT * FROM "RecipeIngredients" ORDER BY "Position";`, and check that `npm run dev` still filters by
  ingredient name.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-022, superseding ADR-004 |
| 3 | `02-senior-csharp` | domain + application + infrastructure + api + migration + backend tests |
| 4 | `08-api-contract` | OpenAPI snapshot + TS types + contract delta |
| 5 | `03-senior-react` | `RecipeList.tsx` search haystack + fixtures |
| 6 | `06-qa-tester` | tests + coverage statement |
| 7 | `04-code-reviewer` | review |
| 8 | `05-security-reviewer` | review — triggered by new stored user input |

`07-ux-ui` is not involved: this item ships no screen. The detail screen is `R-18` and the form is `R-21`.

## 14. Assumptions made

- **The developer database is empty** (confirmed by the user, 2026-09-24). The backfill SQL is lossless by
  construction, but it is exercised only against test fixtures, never against real accumulated data.
- `Unit`'s seventeen members are enough for the foreseeable recipes. Adding one is a code-and-deploy change.
- `HasPrecision(9, 3)` is sufficient: quantities up to 999999.999 with three decimals, which covers `1/3` as
  `0.333` and `0.125` tsp.
- 200 characters is a sensible cap for both `Name` and `Notes`. Chosen to match the existing `Title` cap rather
  than derived from data.
- Wholesale replacement on update is acceptable at 50 ingredients or fewer. If that stops being true, the fix is
  a diff, not a schema change.

## 15. Follow-ups

- The `Ingredients` getter allocates a sorted copy per access. Measure before optimising; no issue is filed
  because nothing has demonstrated a cost.
- `SEC-08` proper — `HasMaxLength` on `Title` and `Description` — becomes a small migration now that
  `OnModelCreating` exists. Not done here; the entry stays open with that note.
- `BUG-14` (cached instance mutated before persistence) is untouched and slightly more visible after this
  change. Its entry gains a line saying so.

## 16. Known issues and roadmap items touched

- **Fixes:** `BUG-11` (ingredient half — instruction half stays open), `SEC-09` (ingredient half — instruction
  half stays open), known limitation #1 in [../domain-model.md](../domain-model.md#known-limitations).
- **Supersedes:** ADR-004.
- **Completes:** `R-10` in [../roadmap.md](../roadmap.md), and unblocks `R-17`, `R-18`, `R-19`, `R-21`.
- **Depends on:** nothing. `R-10` is order 1 with no prerequisites.
- **Partially unblocks:** `SEC-08` (`OnModelCreating` now exists), `R-11` (ingredient search becomes an
  indexable SQL join).
- **Worsens in degree:** `SEC-07` — each recipe's payload grows, and `GET /api/recipes` is still unpaginated.
- **On the [deploy gate](../roadmap.md#deploy-gate)?** no.
