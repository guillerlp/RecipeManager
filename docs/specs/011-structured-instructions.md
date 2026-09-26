# Spec: Structured instructions

| | |
| --- | --- |
| **ID** | `011` |
| **Status** | draft |
| **Author** | `00-leader` + `01-architect` |
| **Created** | `2026-09-26` |
| **Branch** | `docs/structured-instructions-spec` (this spec), then `feat/structured-instructions` (implementation) |

---

## 1. Context

`Recipe.Instructions` is still `IReadOnlyList<string>` persisted as `text[]` — the surviving half of ADR-004's
acknowledged temporary shortcut. ADR-022 fixed its replacement shape on 2026-09-24 (`InstructionStep` with
`Position`, `Text`, `DurationMinutes`, `IngredientIds`) but deliberately left the implementation to `R-17`.
Until it ships, per-step timing and per-step ingredient highlighting are **blocked**, not merely awkward, and the
Detail (`R-18`), Add/Edit (`R-21`), and Cooking-mode (`R-23`) screens have nothing structured to render. The same
free-text column is also the last home of three open defects: the instruction halves of `BUG-11` and `SEC-09`,
and `TEST-03`.

## 2. Goal

Replace free-text instructions with an ordered collection of `InstructionStep` owned entities that reference the
recipe's ingredients by id, as ADR-022 decided.

## 3. In scope

- [ ] `InstructionStep` owned entity — `Position`, `Text`, `DurationMinutes`, `IngredientIds` — with a `Create`
      factory returning `Result<InstructionStep>`
- [ ] `Recipe.Instructions` becomes `IReadOnlyList<InstructionStep>` over a private backing field
- [ ] New domain invariants and their `RecipeErrors` factories; `InstructionEmpty()` replaced
- [ ] `InstructionStepInputDtoValidator` with per-item caps, closing the validator half of `SEC-09`
- [ ] A second `OwnsMany` in `RecipeConfiguration` into `"RecipeInstructionSteps"`, with `varchar(2000)` on
      `Text`, closing the database half of `SEC-09`
- [ ] Migration `StructureInstructions` with a pre-flight length guard and a lossless backfill from `text[]`
- [ ] `InstructionMappingExtensions.ToInstructionSteps`, resolving payload indexes to ingredient ids
- [ ] `RecipeDto`, `CreateRecipeCommand`, `UpdateRecipeDto`, `UpdateRecipeCommand` carry `InstructionStepDto` /
      `InstructionStepInputDto`
- [ ] Regenerated OpenAPI snapshot and TypeScript contract, plus two new aliases in `recipe.ts`
- [ ] A regression test proving no collection on a materialised `Recipe` is castable to `List<>` (`BUG-11`, both
      halves, and the unpinned acceptance criterion from spec 010 §11)
- [ ] ADR-023 recording that step references cross the wire as payload indexes

## 4. Out of scope

- **`BUG-15`** (a client-supplied ingredient id can trigger a duplicate-key 500). It lives in the same code path,
  but it is a separate defect with a separate fix, and must close before `R-14` regardless. Decided with the
  user: a **separate PR after this one**. This spec neither fixes nor worsens it — see §7.
- **Client-supplied step ids.** Step ids are minted on every write and never accepted as input. Nothing
  references a step today; see §14.
- **Any screen.** The detail screen is `R-18`, the form is `R-21`, cooking mode is `R-23`.
- **`Title` / `Description` length limits** (`SEC-08`). Altering existing columns stays its own migration, as
  spec 010 argued.
- **A step timer, grouping, or images per step.** Known limitation #1 names them; `DurationMinutes` is the only
  one ADR-022 decided.
- **Caching a read model instead of the entity** (`BUG-14`'s real fix). This spec removes that bug's last
  exposure but does not change what the cache stores.

## 5. Decisions taken with the user (2026-09-26)

| # | Fork | Decision |
| --- | --- | --- |
| 1 | How a request body says which ingredients a step uses | **Indexes into the same payload's ingredient list** (`ingredientIndexes: int[]`), resolved to ids in the Application layer. Responses carry ids. |
| 2 | Whether to close `BUG-15` here | No — a separate PR after `R-17` |
| 3 | Per-step caps | `Text` ≤ 2000 (validator **and** `varchar(2000)`); `DurationMinutes` > 0 and < 1440 when present; ≤ 50 references per step; ≤ 50 steps |
| 4 | Whether real data must survive the migration | The database is empty; the migration is written lossless regardless (§14) |

Fork 1 is the one ADR-022 did not see. It says steps reference ingredient **ids**, but on `POST` the ingredients
have no ids yet — the server mints them — and `BUG-15`'s agreed fix (route c) forbids trusting a client-supplied
id on create. Rejected options are argued in §9.

## 6. Domain impact

- **New entity:** `InstructionStep` (`RecipeManager.Domain/Entities/InstructionStep.cs`) — a `sealed class`
  deriving from `Entity`, owned by `Recipe`, never addressable on its own. It mirrors `Ingredient` deliberately:

  ```csharp
  public sealed class InstructionStep : Entity
  {
      private readonly List<Guid> _ingredientIds = [];

      public int Position { get; private set; }
      public string Text { get; private set; }
      public int? DurationMinutes { get; private set; }
      public IReadOnlyList<Guid> IngredientIds => _ingredientIds.ToList().AsReadOnly();

      public static Result<InstructionStep> Create(string text, int? durationMinutes, IEnumerable<Guid> ingredientIds);

      // Order belongs to the recipe, not to the step, so only Recipe may set this.
      internal void SetPosition(int position);
  }
  ```

  `Create` always mints the id with `Guid.NewGuid()`; it takes no id parameter. `IngredientIds` is exposed over a
  private backing field returning a fresh copy — the `BUG-11` lesson applied from the start rather than fixed
  afterwards. `Recipe` remains the only aggregate root, so ADR-006 is untouched and every write stays one
  repository call.
- **Changed property on `Recipe`:** `Instructions` retyped from `IReadOnlyList<string>` to
  `IReadOnlyList<InstructionStep>`, exposed over a private `List<InstructionStep> _instructions` and sorted by
  `Position`, exactly like `Ingredients`. A private `ReplaceInstructions` assigns positions from list order and is
  called from the constructor and from `Update`.
- **New/changed invariants:**

  | Rule | Where | Error factory | Kind (→ HTTP) | `field` |
  | --- | --- | --- | --- | --- |
  | Step text not null/whitespace | `InstructionStep.ValidateProperties` | `InstructionTextRequired()` | `Validation` (422) | `instructions` |
  | `DurationMinutes > 0` when present | `InstructionStep.ValidateProperties` | `InstructionDurationNotPositive()` | `Validation` (422) | `instructions` |
  | Every `IngredientIds` element is the id of one of this recipe's ingredients | `Recipe.ValidateProperties` | `InstructionIngredientNotFound()` | `Validation` (422) | `instructions` |

  `InstructionsRequired()` is unchanged. `InstructionEmpty()` is **removed**, superseded by
  `InstructionTextRequired()` — the same move ADR-022 made when `IngredientNameRequired()` replaced
  `IngredientEmpty()`: a blank-text step cannot be constructed at all, so the rule belongs to the step.

  The membership rule is ADR-022's invariant, unchanged: **the domain only ever sees ids.** `Update` still
  validates first and mutates nothing when validation fails.
- **New/changed shape validation:** `ValidateInstructions` is rewritten for `List<InstructionStepInputDto>` —
  list `NotNull` and at most 50 items as today, plus per item (`InstructionStepInputDtoValidator`): `Text`
  `NotNull` + `MaximumLength(2000)`; `DurationMinutes` `GreaterThanOrEqualTo(0)` + `LessThan(1440)` when
  present; `IngredientIndexes` `NotNull`, at most 50 items, each `>= 0`, and distinct.

  The duration's lower bound is deliberately `>= 0`, not `> 0`: "positive" is the domain's rule
  (`InstructionDurationNotPositive`, 422), and repeating it in the validator would duplicate a business rule
  (CLAUDE.md global rule 5) and make the domain error unreachable over HTTP. This is the split already used for
  `Ingredient.Quantity` — `InclusiveBetween(0, 100000)` in `IngredientInputDtoValidator`, `> 0` in the domain.
- **Migration required:** yes — `StructureInstructions`. Not destructive to data (the backfill is lossless), but
  it **drops a column**, so it is destructive to schema. `Down()` is lossy — see §6.1.
- **Known limitations touched:** closes item 1 in [../domain-model.md](../domain-model.md#known-limitations)
  (instructions are unstructured strings) as far as ADR-022 goes. Grouping and per-step images stay unplanned;
  the item is rewritten to say so rather than deleted outright.

### 6.1 Persistence

A second `OwnsMany` in `RecipeManager.Infrastructure/Context/Configurations/RecipeConfiguration.cs`:

| Column (`"RecipeInstructionSteps"`) | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid`, PK | `ValueGeneratedNever()` — ADR-022's appended consequence: domain-minted keys and EF's default `Guid` key convention are incompatible |
| `RecipeId` | `uuid`, FK → `"Recipes"("Id")`, cascade delete, indexed | Shadow property, as for ingredients |
| `Position` | `integer`, `NOT NULL` | A child table has no inherent row order |
| `Text` | `character varying(2000)`, `NOT NULL` | Closes the database half of `SEC-09` |
| `DurationMinutes` | `integer`, nullable | |
| `IngredientIds` | `uuid[]`, `NOT NULL` | Npgsql primitive collection; no join table (ADR-022) |

Both the `Instructions` navigation and `IngredientIds` are mapped with
`UsePropertyAccessMode(PropertyAccessMode.Field)`, so EF populates the private lists and never assigns a mutable
list through a setter. That is what closes the instruction half of `BUG-11`.

No check constraints. The `2000` cap is a column type; `DurationMinutes > 0` and reference membership are domain
invariants, consistent with `Ingredient.Quantity > 0`, which is not a database constraint either.

**Implementation risk, stated up front.** A primitive collection *inside* an owned type, mapped through a backing
field whose type (`List<Guid>`) differs from the property type (`IReadOnlyList<Guid>`), is the one combination
`R-10` did not exercise. The first backend task is therefore a failing integration test that round-trips it
through PostgreSQL. If EF rejects the mapping, the fallback is to configure the field explicitly with `HasField`
— an implementation adjustment, not a design change — and it is reported either way.

### 6.2 Existing rows

The migration runs in this order, modelled on `20260924130146_StructureIngredients`:

1. Create `"RecipeInstructionSteps"`.
2. **Pre-flight guard.** A `DO $$ … RAISE EXCEPTION … END $$;` block aborts if any element of
   `"Recipes"."Instructions"` is longer than 2000 characters, naming the limit, the column, and the fact that the
   backfill will not truncate. Without it, PostgreSQL raises a bare `22001` and — because `Program.cs` calls
   `app.MigrateDatabase()` at startup — the API crash-loops on boot with no indication of which row caused it.
   That is the correction spec 010 §6 learned during implementation.
3. Backfill:

   ```sql
   INSERT INTO "RecipeInstructionSteps" ("Id","RecipeId","Position","Text","DurationMinutes","IngredientIds")
   SELECT gen_random_uuid(), r."Id", t.ord - 1, t.elem, NULL, '{}'::uuid[]
   FROM "Recipes" r, unnest(r."Instructions") WITH ORDINALITY AS t(elem, ord);
   ```

4. Drop `"Recipes"."Instructions"`.

Every existing string becomes a step at its original index with no duration and no references. That is lossless
because both are optional — a requirement the domain imposes independently, since most real steps use no timer.

`Down()` rebuilds the `text[]` with `array_agg("Text" ORDER BY "Position")` and drops the table. It is **lossy**:
durations and ingredient references are discarded. With `app.MigrateDatabase()` at startup and no rollback
procedure (`INFRA-03`), the implementation PR description must call this out, as `R-10`'s did.

## 7. API impact

- **New/changed endpoints:** none. All five routes keep their verbs, paths, and status codes.
- **New DTOs:** both positional `record`s in `RecipeManager.Application/DTO/Recipes/`, one file per type:

  ```csharp
  public record InstructionStepDto(Guid Id, string Text, int? DurationMinutes, List<Guid> IngredientIds);
  public record InstructionStepInputDto(string Text, int? DurationMinutes, List<int> IngredientIndexes);
  ```

  `Position` is in neither, for the reason spec 010 gave for ingredients: the JSON list is already ordered, and
  an index the client must keep consistent with array order invites the two disagreeing.
- **`RecipeDto` changes:** `List<string> Instructions` becomes `List<InstructionStepDto> Instructions`.
- **`CreateRecipeCommand` / `UpdateRecipeDto` / `UpdateRecipeCommand` changes:** `List<string> Instructions`
  becomes `List<InstructionStepInputDto> Instructions`.
- **Reference semantics.** `IngredientIndexes[k] = i` means "the ingredient at index `i` of **this same
  request's** `ingredients` array". It is identical on `POST` and `PUT`. The response then carries the resolved
  ids in `IngredientIds`.
- **Where indexes become ids.** A new `InstructionMappingExtensions.ToInstructionSteps(this
  IEnumerable<InstructionStepInputDto>? inputs, IReadOnlyList<Ingredient> ingredients)` in
  `RecipeManager.Application/Mappings/` resolves each index to `ingredients[i].Id` and builds each step through
  `InstructionStep.Create`, collecting every failure like `ToIngredients` does. This works because
  `Ingredient.Create` already mints the id at construction, before `Recipe.Create` runs. An index outside
  `ingredients` fails there with `InstructionIngredientNotFound()` — the same error the domain raises, so the
  client sees one error for one concept. Negative and duplicate indexes never reach it; the validator rejects
  them as payload shape (400).
- **Handler flow.** Both `CreateRecipeHandler` and `UpdateRecipeHandler` become: `ToIngredients()` → return on
  failure → `ToInstructionSteps(ingredients.Value)` → return on failure → `Recipe.Create`/`Update`. The early
  return after ingredients is the existing pattern and is kept: steps cannot be resolved against ingredients
  that failed to build. The cost is that a payload with both a bad ingredient and a bad step reports only the
  ingredient error on the first attempt.
- **Update semantics.** `Recipe.Update` **replaces** the step collection wholesale, like ingredients. Step ids are
  re-minted on every `PUT`. EF's change tracker computes the deletes and inserts, which requires the recipe to
  be loaded through `GetByIdForUpdateAsync` — already the case since `R-10` (spec 010 §7 correction).
- **Breaking for the client?** Yes — `08-api-contract` ships `contracts/openapi.json` and
  `recipe-manager-frontend/src/types/generated/api.ts` in the same PR. Verify, rather than assume, that
  `DurationMinutes` (`int?`) is emitted as nullable and not listed in `required`: `NullableEnumSchemaFilter`
  exists because Swashbuckle drops nullability on a `$ref`, and a nullable primitive should not be affected, but
  `RequireNonNullablePropertiesSchemaFilter` is the thing to check.
- **Cache impact:** none. Same keys (`recipes_all`, `recipe_{guid}`), same invalidation.
- **Effect on `BUG-15`.** Neither fixed nor worsened. Because steps are wired by index on input, the ingredient
  id echo no longer carries step references: a client that rebuilds the ingredient list from scratch now gets new
  ingredient ids **and** correctly re-resolved step references. The three duplicate-key routes all still stand.

## 8. Frontend impact

- **New routes:** none.
- **New/changed components:** none. The only non-generated reference to instructions in `src/` is the fixture
  `instructions: []` in `RecipeList.test.tsx`, which the new type still accepts. `RecipeList`'s search haystack
  does not include instructions and gains nothing here.
- **New/changed hooks:** none.
- **New type aliases:** `recipe.ts` gains `InstructionStep = Schemas['InstructionStepDto']` and
  `InstructionStepInput = Schemas['InstructionStepInputDto']`. Both are genuinely new generated schemas, so the
  aliases are legal under ADR-019.
- **States to design:** none. **Design tokens needed:** none.
- **What `R-21` inherits.** The form translates each step's selected ingredients from ids (what it read) to
  indexes (what it sends) at submit time, against the ingredient array it is about to send. That translation is
  the whole contract; record it in `R-21`'s entry in the roadmap.

## 9. Architecture impact

- **ADR required:** yes — **ADR-023** in [../architecture.md](../architecture.md): *step references cross the
  wire as payload indexes.* It amends ADR-022 only at the wire; ADR-022's domain shape and its "integrity is a
  domain invariant" rule are unchanged. ADR-022 gains a one-line pointer to ADR-023.
- **New dependency:** none.
- **Layer/dependency changes:** none. `InstructionStep` lives in `Domain`, which still references no project.
- **New DI registrations:** none. No new handler; the new validator is instantiated inside
  `ValidateInstructions`, as `IngredientInputDtoValidator` is inside `ValidateIngredients`.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Ingredient **ids** in the input DTO, as ADR-022 wrote it | Symmetry: input and output reference the same thing | On `POST` there are no ids yet. Either the client mints them — which `BUG-15`'s route (c) fix forbids — or a new recipe cannot have references until a second `PUT`. |
| Client-chosen string keys per ingredient, steps referencing keys | Explicit references that survive client-side reordering | A new concept on the wire that nothing else in the API uses, and the server must validate key uniqueness — the same class of problem as `BUG-15`, relocated. Indexes give the same result with nothing new. |
| Passing indexes into `Recipe.Create`/`Update` and resolving them in the domain | One place for all reference logic | Leaks a transport concept into the aggregate, and gives the domain two ways to express the same reference. ADR-022's invariant is stated in ids; keeping it that way keeps the aggregate independent of payload layout. |
| A `StepIngredients` join table with real foreign keys | Database-enforced integrity | An FK from one owned row to a sibling owned row duplicates what the aggregate root already guarantees in code, and adds a third table to every recipe load. ADR-022 rejected it; nothing has changed that. |
| Keeping `text[]` and adding a parallel `jsonb` for durations and references | Smallest schema change | Two columns that must agree positionally, with nothing enforcing it — the index-reference hazard ADR-022 rejected for ingredients, moved into storage. |
| Accepting client-supplied step ids on update | Stable step ids across edits | Nothing references a step today, and it would open a fourth `BUG-15`-style route. Add it when something needs it (§15). |

- **Pattern applied:** *owned entity within an aggregate* (EF Core `OwnsMany`), *static factory returning
  `Result<T>`*, and *anti-corruption at the boundary* — the wire's local references (indexes) are translated
  into the domain's global ones (ids) before the aggregate sees them. Existing examples in this repo:
  `RecipeManager.Domain/Entities/Ingredient.cs` and
  `RecipeManager.Application/Mappings/IngredientMappingExtensions.cs`.
- **What this makes harder:**
  - Input and output are asymmetric: the client reads ids and writes indexes. Every future writer (`R-21`,
    `R-24` import) must perform that translation, and the type system only half-helps — `number[]` against
    `string[]` stops a direct copy, but not an index computed against the wrong array.
  - `uuid[]` has no foreign key. A raw `DELETE FROM "RecipeIngredients"` leaves dangling ids that nothing in the
    database notices. Only writes through the aggregate are safe, which `R-24`'s import must respect.
  - Step ids change on every `PUT`. Anything that later remembers "the user is on step X" across an edit
    (`R-23`) must key on position, or this decision has to be revisited.
  - Reference membership is checked twice — range in the mapper, membership in the domain. The domain check is
    the real guard; the mapper check exists only because an out-of-range index cannot produce an id at all.
  - `Down()` is lossy, and migrations apply at startup with no rollback procedure (`INFRA-03`).

## 10. Security impact

- **New user-controlled input:** `Text`, `DurationMinutes`, `IngredientIndexes`. `Text` is bounded in
  FluentValidation **and** the database (`varchar(2000)`), closing the last instruction-side gap in `SEC-09`.
  `DurationMinutes` is bounded by the validator. `IngredientIndexes` is capped at 50 per step and 50 steps, so the
  worst case is 2,500 index lookups per request against at most 50 ingredients.
- **User content rendered in the SPA:** not yet — no screen renders instructions. When `R-18` does, React escapes
  by default; nothing may use `dangerouslySetInnerHTML`.
- **File upload:** no.
- **Auth/ownership implications:** none new. No authentication, no ownership (`SEC-01`, `SEC-02`). Index-based
  references cannot name another recipe's ingredient at all — an index is resolved only against the same payload
  — so this adds no cross-recipe reference route.
- **Config/secrets touched:** none.
- **Standing gaps affected:** **closes `SEC-09`**. `SEC-07` (unpaginated `GET /api/recipes`) is **worsened in
  degree**: each step now carries an id, a duration, and an id array. `R-11` is the fix and is unchanged.

## 11. Acceptance criteria

- [ ] Given a payload with ingredients `[flour, butter]` and a step `{"text": "Rub in", "durationMinutes": 5,
      "ingredientIndexes": [1, 0]}`, when `POST /api/recipes`, then 201 and the response step carries a non-empty
      `id`, the text, `5`, and `ingredientIds` equal to `[butter.id, flour.id]` in that order.
- [ ] Given a recipe created with steps A, B, C, when `GET /api/recipes/{id}`, then they are returned in exactly
      that order (`TEST-03`).
- [ ] Given a stored recipe whose step references butter, when `PUT` sends the ingredients reordered with butter
      at a new index and the step's index updated to match, then a subsequent `GET` shows the step still
      referencing butter's id.
- [ ] Given a stored recipe, when `PUT` sends the ingredient list **without** echoing ids and the step references
      butter by index, then the step references butter's **new** id.
- [ ] Given a payload with 2 ingredients and a step with `"ingredientIndexes": [2]`, when `POST`, then 422 with
      `field: "instructions"`.
- [ ] Given a step with `"ingredientIndexes": [-1]` or `[0, 0]`, when `POST`, then 400.
- [ ] Given a step whose `text` is `"  "`, when `POST`, then 422 with `field: "instructions"`.
- [ ] Given a step whose `text` is 2001 characters, when `POST`, then 400.
- [ ] Given a step with `"durationMinutes": 0`, when `POST`, then 422 with `field: "instructions"`.
- [ ] Given a step with `"durationMinutes": -1` or `1440`, when `POST`, then 400.
- [ ] Given a recipe with steps, when it is deleted, then no orphan rows remain in `"RecipeInstructionSteps"`.
- [ ] Given a `Recipe` materialised by EF from PostgreSQL, when `Ingredients`, `Instructions`, or any step's
      `IngredientIds` is cast to `List<>`, then the cast fails (`BUG-11`, both halves).
- [ ] Given a database at `SyncIdValueGeneration` with a recipe whose `Instructions` is `{'mix','bake'}`, when the
      migration is applied, then two rows exist at `Position` 0 and 1 with those texts, null duration, and an
      empty `IngredientIds`.
- [ ] Given that same database with an instruction of 2001 characters, when the migration is applied, then it
      aborts with the guard's message and `"Recipes"."Instructions"` still exists.

## 12. Test plan

- **Domain unit tests** (`RecipeManager.UnitTests/Domain/Entities/`): `InstructionStep.Create` — valid; blank
  text; zero and negative duration; null duration and empty references accepted; returned `IngredientIds` not
  castable to `List<Guid>`. `Recipe.Create` and `Recipe.Update` — `Position` assigned from list order; an unknown
  ingredient id fails with `InstructionIngredientNotFound`; errors aggregate across several bad steps in one
  call; `Update` mutates nothing when validation fails (extend
  `Update_WithInvalidData_ShouldNotUpdatePropertiesAndReturnFailure`). Existing `InstructionEmpty` tests move to
  `InstructionTextRequired`.
- **Mapping unit tests:** `ToInstructionSteps` — indexes resolve to the ids at those positions, in the order
  given; an out-of-range index fails with `InstructionIngredientNotFound`; failures across several steps are
  collected.
- **Validator unit tests:** text at 2000 passes and 2001 fails; duration 0 and 1439 pass, -1 and 1440 fail;
  negative index; duplicate indexes; 51 indexes; list cap still at 50.
- **Handler unit tests:** existing `CreateRecipeHandler` and `UpdateRecipeHandler` tests moved to the new shape —
  success, not-found, validation failure (ingredient and step), cancellation propagation, `Received(1)`.
- **Ordering:** asserted with `Should().Equal(...)`, never `BeEquivalentTo`, wherever order is the property under
  test. That is what closes `TEST-03`; existing instruction assertions using `BeEquivalentTo` are converted.
- **Integration tests** (`RecipesControllerTests`, real PostgreSQL via Testcontainers): every §11 HTTP criterion,
  each asserting database state after `DbContext.ChangeTracker.Clear()`. The first one written is the
  `IngredientIds` round-trip, because it proves the §6.1 mapping risk.
- **`BUG-11` regression test:** reads a recipe through a fresh `DbContext` and asserts all three collections are
  not `List<>`.
- **Migration test:** `StructureInstructionsMigrationTests`, modelled on `StructureIngredientsMigrationTests` —
  the backfill criterion and the guard criterion from §11.
- **Contract tests:** `OpenApiContractTests` fails until the snapshot is regenerated — the intended signal.
- **Frontend:** `npm run typecheck`, `npm run lint`, `npm test`, `npm run build` all green after regeneration.
  No test changes expected.
- **Not covered, and why:** the migration runs only against a container — the developer database is empty
  (confirmed 2026-09-26). `BUG-15` gains no test here; its own PR adds them.
- **Manual verification:** run the API against local PostgreSQL, create a recipe through Swagger with a timed
  step, an untimed step, and a step referencing two ingredients, then
  `SELECT * FROM "RecipeInstructionSteps" ORDER BY "Position";` and confirm `"Recipes"` has no `Instructions`
  column.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-023, and the pointer on ADR-022 |
| 3 | `02-senior-csharp` | domain + application + infrastructure + migration + backend tests |
| 4 | `08-api-contract` | OpenAPI snapshot + TS types + two aliases + contract delta |
| 5 | `06-qa-tester` | tests + coverage statement |
| 6 | `04-code-reviewer` | review |
| 7 | `05-security-reviewer` | review — triggered by new stored user input |

`07-ux-ui` and `03-senior-react` are not involved: this item ships no screen, and the frontend change is
regeneration only (§8).

## 14. Assumptions made

- **The developer database is empty** (confirmed by the user, 2026-09-26). The backfill is lossless by
  construction but is exercised only against test fixtures.
- **Nothing references a step by id.** True today; `R-22` (cook log) and `R-23` (cooking mode) are the first
  candidates to change that. If one does, accepting echoed step ids is an additive change to the input DTO.
- 2000 characters is a sensible step cap — twice `Description`'s 1000, chosen so a long paragraph step fits while
  a multi-megabyte payload does not. Not derived from data.
- `DurationMinutes` shares `PreparationTime`/`CookingTime`'s `< 1440` bound for consistency; a step longer than a
  day (a 36-hour dough rest) would have to be split or written in the text.
- Wholesale replacement on update is acceptable at 50 steps or fewer, as it is for ingredients.

## 15. Follow-ups

- **`BUG-15`** — the next PR, as decided (§5). Its entry gains a note that index-based step wiring removes its role
  in step references while all three routes stand.
- **Step-id stability**, if `R-22` or `R-23` needs it — recorded in §14 rather than filed, since nothing needs it
  yet.
- **`R-21`'s roadmap entry** gains the id-to-index translation requirement from §8.

## 16. Known issues and roadmap items touched

- **Fixes:** `BUG-11` (instruction half — the entry is deleted and GitHub
  [#8](https://github.com/guillerlp/RecipeManager/issues/8) closed), `SEC-09` (instruction half — the entry is
  deleted), `TEST-03` (deleted), known limitation #1 in [../domain-model.md](../domain-model.md#known-limitations)
  (rewritten to what remains).
- **Improves:** `BUG-14` — its last exposure (a castable `List<string>` on a cached entity) is gone; the entry is
  updated, not deleted, because caching a shared entity is still the design.
- **Completes:** `R-17` in [../roadmap.md](../roadmap.md), and unblocks `R-23`; `R-21` still waits on `R-19` and
  `R-20`.
- **Depends on:** `R-10` (shipped 2026-09-24).
- **Worsens in degree:** `SEC-07`.
- **On the [deploy gate](../roadmap.md#deploy-gate)?** Partly: the "length limits enforced at the database" row
  names `SEC-08` and `SEC-09`; this closes `SEC-09`, and the row stays open on `SEC-08`.
