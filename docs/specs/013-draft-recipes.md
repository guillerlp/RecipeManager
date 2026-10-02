# Spec: Draft recipes

| | |
| --- | --- |
| **ID** | `013` |
| **Status** | approved |
| **Author** | `00-leader` + `01-architect` |
| **Created** | `2026-10-02` |
| **Branch** | `feat/draft-recipes` |

---

## 1. Context

The editorial design's Add/Edit screen (3d) lets a recipe be saved with only a title and finished later. The
domain cannot store such a recipe: `Recipe.ValidateProperties` also requires a description, two times that are
not both zero, servings, and at least one ingredient and one step, so the form as drawn would be rejected with
422 (`UX-05`). On 2026-09-19 this was settled as an explicit Draft/Published status (`R-19`), rejecting both
drafting only in the browser and relaxing the aggregate. `R-21` (the form) waits on it.

## 2. Goal

A recipe can exist as a **draft** that needs only a title, and moves to **published** — where today's full
invariants apply — through an explicit, reversible transition.

## 3. In scope

- [ ] `RecipeStatus` enum and `Recipe.Status`; `Description`, `PreparationTime`, `CookingTime`, `Servings`
      become nullable.
- [ ] Two-tier invariants in `Recipe.ValidateProperties`, selected by status (§6).
- [ ] `Recipe.Publish()` and `Recipe.Unpublish()`, both idempotent.
- [ ] Migration `AddRecipeStatus`: nullable columns, `"Status"` back-filled to `Published`.
- [ ] `POST /api/recipes/{id}/publish`, `POST /api/recipes/{id}/unpublish`, `?status=` on `GET /api/recipes`,
      optional `status` on `POST /api/recipes`, `status` on `RecipeDto`.
- [ ] Per-status list cache keys; every write invalidates all of them plus `recipe_{id}`.
- [ ] Regenerated contract; `recipeService` gains `publishRecipe` / `unpublishRecipe`.
- [ ] `RecipeDetailPage` renders a draft honestly: a "Draft" marker, absent values not rendered, scaling off
      when `servings` is null.
- [ ] ADR-025, decisions-log entry, and every doc in §16 updated.

## 4. Out of scope

- **Publish/unpublish buttons, mutation hooks, a drafts view, the form itself** — `R-21` owns the controls. Until
  it ships, no UI creates a draft; they exist through the API and Swagger only.
- **`?status=all`** — nothing needs it, and `R-11` redesigns the list contract (pagination, saved-view filters).
- **A discriminated-union schema** (`oneOf` on `status`) so the TS type knows published fields are non-null —
  see §9.
- **Draft ownership / privacy.** With no auth (`R-14`), "draft" means *unfinished*, not *private*: anyone who can
  call the API can list and read drafts.
- **Updating the Claude Design helper text** that `UX-05` asks for — it lives outside the repo; it goes in the PR
  description as a manual follow-up.
- `07-ux-ui` screen spec: the only new UI is a text marker on an existing screen, built from existing tokens.

## 6. Domain impact

- **New entities:** none. **New enum** `RecipeStatus { Draft, Published }` in
  `RecipeManager.Domain/Entities/RecipeStatus.cs`.
- **Changed properties on `Recipe`:**
  - `Status: RecipeStatus` — private setter; existing rows get `Published`.
  - `Description: string?`, `PreparationTime: int?`, `CookingTime: int?`, `Servings: int?` — null means
    "not set yet". Existing rows keep their values.
- **Lifecycle:**
  - `Create(title, description, preparationTime, cookingTime, servings, ingredients, instructions, status)` —
    validates against the tier for `status`.
  - `Update(...)` (no status parameter) — validates against the recipe's **current** status; mutates nothing on
    failure, as today.
  - `Publish()` — validates the current state against the published tier; on success sets `Published`. On an
    already-published recipe it returns `Ok` and changes nothing.
  - `Unpublish()` — sets `Draft`; cannot fail, because the draft tier is a strict subset of the published tier.
- **Invariants — always (draft and published):** a value that is present must be valid.

  | Rule | Error | Field |
  | --- | --- | --- |
  | `Title` not null/whitespace | `TitleRequired()` (existing) | `title` |
  | `PreparationTime >= 0` when present | `PreparationTimeNegative()` (existing) | `preparationTime` |
  | `CookingTime >= 0` when present | `CookingTimeNegative()` (existing) | `cookingTime` |
  | `Servings >= 1` when present | `ServingsOutOfRange(1)` (existing) | `servings` |
  | Every step's `IngredientIds` belongs to this recipe | `InstructionIngredientNotFound()` (existing) | `instructions` |
  | Ingredient and step rules | unchanged, enforced in `Ingredient.Create` / `InstructionStep.Create` | — |

- **Invariants — published only:** completeness.

  | Rule | Error | Field |
  | --- | --- | --- |
  | `Description` not null/whitespace | `DescriptionRequired()` (existing) | `description` |
  | `PreparationTime` present | **new** `PreparationTimeRequired()` | `preparationTime` |
  | `CookingTime` present | **new** `CookingTimeRequired()` | `cookingTime` |
  | Not both times zero | `BothTimesZero()` (existing) | `preparationTime,cookingTime` |
  | `Servings` present | **new** `ServingsRequired()` | `servings` |
  | At least one ingredient | `IngredientsRequired()` (existing) | `ingredients` |
  | At least one step | `InstructionsRequired()` (existing) | `instructions` |

  All are `ErrorKind.Validation` → 422, and all violations are collected in one call, as today.
  `BothTimesZero` is publish-only on purpose: a draft at 0/0 is unfinished, not wrong.
- **Changed shape validation (`RecipeValidationRules`):** `Description` drops `NotNull`, keeps
  `MaximumLength(1000)`; the time bounds (`>= 0`, `< 1440`) and servings bounds (`> 0`, `< 1000`) apply only
  when the value is present. Ingredient and step list rules are unchanged (an empty list already passes).
- **Migration required:** yes — `AddRecipeStatus`, **non-destructive**: drops `NOT NULL` from `"Description"`,
  `"PreparationTime"`, `"CookingTime"`, `"Servings"`, and adds `"Status" varchar(20) NOT NULL DEFAULT
  'Published'`, which back-fills existing rows. Stored as the enum **name**, like `Unit` (ADR-022). Applied by
  `app.MigrateDatabase()` at startup; reversible by its `Down` only while no draft holds a null.
- **Known limitations touched:** none of the ten; the "Also decided, not yet designed" Draft entry in
  [domain-model.md](../domain-model.md#target-model--where-the-domain-is-going) moves to current state.

## 7. API impact

- **Endpoints:**

  | Verb | Route | Request body | Success | Failures |
  | --- | --- | --- | --- | --- |
  | GET | `/api/recipes?status={Draft\|Published}` | — | 200 + `RecipeDto[]`; `status` defaults to `Published` | 400 unknown status (model binding) |
  | GET | `/api/recipes/{id}` | — | 200, **any** status | 404 |
  | POST | `/api/recipes` | `CreateRecipeCommand`, optional `status` (default `Published`) | 201 | 400 shape, 422 tier of the requested status |
  | PUT | `/api/recipes/{id:guid}` | `UpdateRecipeDto` (no status) | 204 | 400, 404, 422 tier of the current status |
  | POST | `/api/recipes/{id:guid}/publish` | — | 204, idempotent | 404, 422 listing every gap |
  | POST | `/api/recipes/{id:guid}/unpublish` | — | 204, idempotent | 404 |

- **DTO changes:** `RecipeDto` gains `Status`; `Description`, `PreparationTime`, `CookingTime`, `Servings` become
  nullable on `RecipeDto`, `CreateRecipeCommand`, `UpdateRecipeCommand`, `UpdateRecipeDto`.
  `CreateRecipeCommand` gains `RecipeStatus Status` defaulting to `Published`. `RecipeStatus` crosses the wire as
  a string (`JsonStringEnumConverter`, already registered).
- **Application:** `PublishRecipeCommand(Guid Id)` and `UnpublishRecipeCommand(Guid Id)` → `Result`, handlers
  shaped like `UpdateRecipeHandler` (`GetByIdForUpdateAsync` → 404 → domain method → `UpdateAsync`).
  `GetAllRecipesQuery(RecipeStatus Status)`. No DI registration (Scrutor, ADR-008).
- **Breaking for the client?** Additive for writers — a `POST` without `status` behaves as today. Readers see
  nullable fields, so the TS change ships in the same PR (`08-api-contract`), as the snapshot test enforces.
- **Repository:** `IRecipeRepository.GetAllAsync(RecipeStatus status, CancellationToken)`, filtered with a SQL
  `WHERE` in `RecipeRepository`.
- **Cache impact:** `CacheKeys.AllRecipes` (`recipes_all`) is replaced by
  `CacheKeys.GetRecipesByStatusKey(status)` → `recipes_Draft`, `recipes_Published`. `AddAsync`, `UpdateAsync`,
  `DeleteAsync` invalidate **every** status list key plus `recipe_{id}`. Publish and unpublish persist through
  `UpdateAsync`, so they need no invalidation of their own.

## 8. Frontend impact

- **New routes:** none.
- **Contract:** regenerate `contracts/openapi.json` and `src/types/generated/api.ts`; `src/types/recipe.ts`
  gains only a `RecipeStatus` alias over the generated enum (ADR-019).
- **`services/recipeService.ts`:** `publishRecipe(id)` and `unpublishRecipe(id)` → `AxiosResponse<void>`.
  `getAllRecipes()` unchanged — the server default is published.
- **`RecipeDetailPage`:** a "Draft" text marker beside the title when `status === 'Draft'`; no description
  paragraph when `description` is null; prep and cook rows only when the value is `> 0`; the total sums what is
  present and is hidden at 0.
- **`IngredientRail`:** `writtenServings: number | null`. When null, quantities render as written, with no
  `ServingsStepper` and no "Scaled for N" line — there is no base to scale from.
- **`RecipeCard`, `RecipeList`:** minimal narrowing (`?? 0`, `?? ''`), with a comment that the list only receives
  published recipes. No draft UI.
- **States:** a draft with no ingredients or steps uses the existing empty rendering — verify, do not assume.
- **Design tokens:** existing only.

## 9. Architecture impact

- **ADR required:** yes → **ADR-025** in [../architecture.md](../architecture.md).
- **New dependency:** none.
- **Layer/dependency changes:** none. `RecipeStatus` lives in Domain; Application and Api reference it as they
  do `Unit`.
- **New DI registrations:** none.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Draft only in the browser (local storage) | No server change at all | Rejected 2026-09-19: drafts are lost across devices and browsers, and the design's "Draft saved" means saved |
| Relax the aggregate to require only a title | Smallest diff | Rejected 2026-09-19: every published recipe would lose its guarantees, and every reader would have to cope with incomplete recipes forever |
| A separate `RecipeDraft` aggregate | Each aggregate keeps one fixed rule set | Two tables and a copy-on-publish that changes the id and breaks links; a second aggregate also forces the unit-of-work decision ADR-006 deferred |
| State-pattern classes (`DraftState`, `PublishedState`) | Open for more states | Two states and two transitions; an enum and a guard per transition is the same behaviour in a fraction of the code |
| Status field in the `PUT`/`POST` body | One atomic request for "save and publish" | `PUT` would branch on old→new status, and a client that forgets to echo the status silently unpublishes |
| Sentinel values (`""`, `0`) instead of nulls | No contract churn | `PreparationTime = 0` is a legitimate published value, so 0 cannot also mean "unset"; `Servings = 0` would leak into scaling arithmetic |
| Drafts included in `GET /api/recipes` with a status field | One cache key, simplest server | Every client must remember to filter, and the default read would show unfinished recipes |
| Targeted cache invalidation (only the lists the recipe moved between) | One fewer cache miss per write | A write does not know the previous status without another load; clearing two keys is trivially correct |
| `oneOf` schema so published fields are non-null in TS | Compiler-enforced narrowing | Needs custom Swashbuckle schema filters; four `??` sites do not justify it |

- **Pattern applied:** a lightweight **state machine** inside the aggregate root — an enum plus guarded
  transitions — and **task-based commands** (`PublishRecipeCommand`) over CRUD state edits. Existing example of
  the aggregate guarding its own state: `RecipeManager.Domain/Entities/Recipe.cs`.
- **What this makes harder:** every reader of `Recipe` handles nullable fields even for published recipes,
  because the type cannot express "non-null when Published"; `ValidateProperties` takes a mode and is harder to
  read at a glance; the cache now has more than one list key, the first step toward `R-11`'s key-strategy
  problem; "save and publish" from the future form is two requests.

## 10. Security impact

- **New user-controlled input:** `status` on `POST` and `?status=` — closed enums, rejected at model binding
  (400) when unknown. Publish/unpublish take only the route id.
- **User content rendered in the SPA:** unchanged — React escapes text; no new rendering path.
- **File upload:** no.
- **Auth/ownership:** none exists (`R-14`). Drafts are therefore **not private**; anyone can list
  (`?status=Draft`), read, publish, or unpublish any recipe. Acceptable while the app is not deployed; `R-14`
  must add the ownership filter to the status filter, not beside it.
- **Config/secrets touched:** none.
- **Standing gaps affected:** `SEC-07` (unpaginated list) — unchanged in kind; drafts no longer inflate the
  default list. `SEC-08` (unbounded `Title`/`Description`) — unchanged; making `Description` nullable does not
  bound it.

## 11. Acceptance criteria

- [ ] Given only a title and `status: "Draft"`, when `POST /api/recipes`, then 201 with `status: "Draft"` and
      null description, times, and servings.
- [ ] Given no `status`, when `POST /api/recipes` with a complete body, then 201 with `status: "Published"`.
- [ ] Given `status: "Draft"` and `servings: 0`, when `POST`, then 400 (shape) — a present value must still be
      valid. Given `servings` null on a published `POST`, then 422 with `field: "servings"`.
- [ ] Given a draft, when `GET /api/recipes`, then it is absent; when `GET /api/recipes?status=Draft`, then it
      is present; when `GET /api/recipes/{id}`, then 200.
- [ ] Given a draft missing description and servings, when `POST .../publish`, then 422 with both
      `field: "description"` and `field: "servings"`, and the recipe is still a draft.
- [ ] Given a complete draft, when `POST .../publish`, then 204, it appears in the default list and not under
      `?status=Draft` — including when both lists were cached beforehand.
- [ ] Given a published recipe, when `POST .../publish` again, then 204 and nothing changes.
- [ ] Given a published recipe, when `POST .../unpublish`, then 204 and it moves to the draft list.
- [ ] Given a published recipe, when `PUT` with a null description, then 422 `field: "description"`; given a
      draft, the same `PUT` succeeds with 204.
- [ ] Given an unknown id, when publish or unpublish, then 404.
- [ ] Given `?status=Archived`, when `GET /api/recipes`, then 400.
- [ ] Given existing rows before the migration, after startup every row has `"Status" = 'Published'`.
- [ ] Given a draft with null servings, the detail page shows the "Draft" marker, quantities as written, and no
      servings stepper.

## 12. Test plan

- **Domain unit tests (`RecipeTests`):** draft with only a title; each always-tier rule fires in a draft; each
  published-tier rule fires on `Create(Published)`, on `Update` of a published recipe, and on `Publish()`;
  `Publish()` collects every gap and mutates nothing on failure; `Publish()`/`Unpublish()` idempotent;
  `Update` on a draft stays lenient. `[Theory]` tables for the tiers.
- **Validator unit tests:** null description/times/servings accepted; bounds still enforced when present.
- **Handler unit tests:** publish/unpublish — success with `UpdateAsync` `Received(1)`, not found with no write,
  validation failure with no write, cancellation-token propagation; `GetAllRecipesHandler` passes the status.
- **Cache tests (`RecipeCacheTests`):** each status list cached under its own key; add, update, delete, publish,
  and unpublish evict both list keys and `recipe_{id}`.
- **Integration tests (`RecipesControllerTests`):** every API criterion in §11 — status code plus database state,
  `ChangeTracker.Clear()` before asserting after a write.
- **Frontend (Vitest + RTL):** `RecipeDetailPage` with a draft fixture; `IngredientRail` with
  `writtenServings = null`.
- **Not covered, and why:** the migration back-fill of **existing** rows — test databases start empty, so no
  automated test sees pre-migration data. Covered by manual verification.
- **Manual verification:** on the local PostgreSQL (WSL2), with existing recipes, start the API and run
  `SELECT "Id", "Status" FROM "Recipes";` — every row `Published`. Then create a draft in Swagger, publish it,
  unpublish it, and open `/recipes/{id}` in the SPA.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-025 + decisions-log entry |
| 3 | `02-senior-csharp` | domain, application, infrastructure, migration, api, backend tests |
| 4 | `08-api-contract` | regenerated snapshot and TS types, service methods, contract delta |
| 5 | `03-senior-react` | draft rendering on the detail page, narrowing in list components |
| 6 | `06-qa-tester` | tests + coverage statement |
| 7 | `04-code-reviewer` | review |
| 8 | `05-security-reviewer` | review — new stored input and new state-changing endpoints |

## 14. Assumptions made

- Publish and unpublish on an already-matching status return 204, not 409 — chosen for retry safety; no client
  needs to distinguish them.
- `Title` keeps its current rules in a draft; a draft is not "anything at all".
- A present-but-invalid value is a 400 or 422 even in a draft (servings 0, a negative time): drafts are
  incomplete, never wrong.
- The detail page needs no new design-token work for the "Draft" marker.

## 15. Follow-ups

- Update the Add/Edit screen's helper text in the Claude Design file to describe drafts and publishing (`UX-05`'s
  fix, outside the repo) — PR description.
- `R-21` wires publish/unpublish controls and the `['recipes']` invalidation for them.
- `R-14` must combine the ownership filter with the status filter.
- `R-11` inherits per-status list keys when it designs the paginated key strategy.

## 16. Known issues and roadmap items touched

- **Fixes:** `UX-05` (delete its entry); completes `R-19` (delete its entry, mark row 4 shipped, note
  `R-21`'s "Draft saved" dependency as met).
- **Docs updated in the same PR:** `architecture.md` (ADR-025), `decisions-log.md`, `domain-model.md`,
  `roadmap.md`, `known-issues.md`, `agents/01-architect.md` (the "Only a title required" row → shipped),
  `CLAUDE.md` (test counts, once CI confirms them).
- **Depends on:** nothing open (`R-10` shipped).
- **On the [deploy gate](../roadmap.md#deploy-gate)?** no — but see §10: drafts are public until `R-14`.
