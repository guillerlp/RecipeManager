# Spec: Recipe tags

| | |
| --- | --- |
| **ID** | `014` |
| **Status** | draft — awaiting user review |
| **Author** | `00-leader` + `01-architect` |
| **Created** | `2026-10-03` |
| **Branch** | `feat/recipe-tags` |

---

## 1. Context

A recipe cannot be labelled. The editorial design shows freeform tags in four places — the first tag on each
list row ("ROAST"), a kicker above the detail title ("Roast · feeds a table"), and a chip editor on the Add/Edit
screen (3d) — and the domain has nowhere to keep them (known limitation #6). `R-20` adds them, and comes before
`R-21` so the form is built once, against the final shape.

## 2. Goal

A recipe carries an ordered, normalised list of freeform tags that the API stores and returns, and the SPA
shows on the list and detail screens and can filter by.

## 3. In scope

**PR 1 — backend and contract**

- [ ] `Recipe.Tags` (`IReadOnlyList<string>`), accepted by `Recipe.Create` and `Recipe.Update`, normalised in
      the domain (§6).
- [ ] New `RecipeErrors.TagRequired()` for a tag that is blank after trimming.
- [ ] Shape rules in `RecipeValidationRules`: list `NotNull`, at most 20 items, each `NotNull` and at most 40
      characters.
- [ ] `Tags` column `character varying(40)[] NOT NULL` with a `CHECK` on its cardinality; migration
      `AddRecipeTags` back-fills existing rows with `'{}'`.
- [ ] `tags` on `CreateRecipeCommand`, `UpdateRecipeDto`, and `RecipeDto`.
- [ ] Regenerated `contracts/openapi.json` and `src/types/generated/api.ts`.
- [ ] ADR-026, `domain-model.md` updated (limitation #6 narrowed), decisions-log entry.

**PR 2 — frontend**

- [ ] `RecipeCard` shows the recipe's first tag as plain text in the meta column.
- [ ] `RecipeDetailPage` shows every tag as a kicker above the title, each a link to `/recipes?tag=<tag>`.
- [ ] `RecipePage` reads `?tag=` and filters the list to recipes carrying that exact tag, combined with the text
      search by AND, and shows the active tag with a control to clear it.
- [ ] The text search also matches tags.
- [ ] `R-20` removed from the roadmap; spec status set to shipped.

## 4. Out of scope

- **Editing tags in the UI.** The chip editor belongs to the form, `R-21`. Until it ships, tags are written
  through the API and Swagger only.
- **Server-side filtering (`GET /api/recipes?tag=`) and a GIN index.** Filtering is client-side, like the
  existing search. Both belong to `R-11`, which designs the list's query contract and cache keys together; a
  `?tag=` now would multiply the per-status cache keys before that design exists.
- **Home's "Jump to" chips** ("under 30 minutes", "feeds a table", "never cooked yet"). They are computed saved
  views, not tags — `R-11` and `R-22` own them. A tag that happens to read "feeds a table" is unrelated.
- **A tag catalogue, renaming a tag across recipes, tag suggestions/autocomplete.** Tags belong to their recipe,
  as ingredients do (§9).
- **Capitalised tags** ("BBQ", "Thai"). Normalisation lowercases everything; CSS chooses the display case.
- `07-ux-ui` screen spec: the screens already exist in the canonical design; the only new elements are a text
  span, a kicker of links, and an active-filter chip, built from existing tokens.

## 6. Domain impact

- **New entities:** none.
- **New property on `Recipe`:** `Tags: IReadOnlyList<string>` over a private `List<string>` backing field,
  returned as a read-only copy (the `InstructionStep.IngredientIds` shape). Never null; existing rows get an
  empty list.
- **Lifecycle:** `Create(..., IEnumerable<InstructionStep> instructions, RecipeStatus status = Published,
  IEnumerable<string>? tags = null)` — tags optional and last, because a new recipe with no tags is the truthful
  default and 89 test call sites stay untouched; `Update(..., IEnumerable<string> tags)` — **required**, because an
  omitted argument there would silently clear the tags. `Update` still mutates nothing when validation fails.
  `Publish` and `Unpublish` do not touch tags.
- **Normalisation** — a private static method on `Recipe`, applied before validation and storage, in this order:
  1. trim;
  2. collapse every run of internal whitespace to one space;
  3. `ToLowerInvariant()`;
  4. drop duplicates, keeping the **first** occurrence, so the author's order survives.

  Duplicates are therefore merged silently, never reported. `"  Roast "`, `"roast"`, `"ROAST"` → `["roast"]`;
  `"feeds   a table"` → `"feeds a table"`.
- **New invariant** (both tiers — a draft may have no tags, but never a blank one):

  | Rule | Error | Kind (→ HTTP) | Field |
  | --- | --- | --- | --- |
  | No tag is blank after trimming (reported once per recipe) | `TagRequired()` | `Validation` (422) | `tags` |

  Placed after the instruction checks in `ValidateProperties`, so the primary error `ResultExtensions` picks for
  every existing payload is unchanged.
- **New shape validation** (`RecipeValidationRules.ValidateTags`, wired into both `CreateRecipeCommandValidator`
  and `UpdateRecipeDtoValidator`):
  - `Tags` `NotNull`, at most 20 items.
  - Each item `NotNull`, `MaximumLength(40)`.

  Limits apply to the **raw** input, before normalisation — a validator that normalised first would duplicate
  the domain rule (Global rule 5). Consequence: a 41-character tag fails with 400 even if collapsing its
  whitespace would bring it under 40, and 21 tags that would dedupe to 3 also fail. Both are accepted as
  honest: the limit is on what was sent.
- **Migration required:** yes, `AddRecipeTags`, non-destructive. Adds `"Tags" character varying(40)[] NOT NULL
  DEFAULT '{}'` and `CONSTRAINT "CK_Recipes_Tags_Count" CHECK (cardinality("Tags") <= 20)`. The `'{}'` default
  exists only in the migration SQL, to back-fill existing rows — the same pattern ADR-025 used for `Status`. `Down`
  drops both and loses tag data, which is the expected cost of rolling back a feature that introduced it.
- **Known limitations touched:** #6 — "No categories, tags, ratings, or favourites" becomes "No categories,
  ratings, or favourites".

## 7. API impact

- **New/changed endpoints:** none. The existing write and read endpoints carry the new field.

  | Verb | Route | Change |
  | --- | --- | --- |
  | GET | `/api/recipes`, `/api/recipes/{id}` | `tags: string[]` in every `RecipeDto`, normalised, in stored order |
  | POST | `/api/recipes` | `tags` **required** (`[]` for none); 400 for null / >20 / >40 chars, 422 for a blank tag |
  | PUT | `/api/recipes/{id:guid}` | Same rules; replaces the whole list, as `PUT` replaces every other field |

- **DTO changes:** `List<string> Tags` added to `CreateRecipeCommand`, `UpdateRecipeDto`, and `RecipeDto`.
  Required rather than optional because `PUT` is a full replacement: with a nullable `tags`, "omitted" would have
  to mean either "clear" or "keep", and either reading surprises somebody. `ingredients` and `instructions`
  already follow this rule.
- **Breaking for the client?** Yes, in the request shape: a body without `tags` becomes a 400. The SPA sends no
  writes yet (`R-21`), so only Swagger users and the integration-test payloads are affected; `08-api-contract`
  ships the regenerated types in PR 1.
- **Cache impact:** none. Tags live inside the recipe row, so the existing invalidation — every `recipes_{status}`
  key plus `recipe_{id}` on each write (ADR-025) — already covers them.

## 8. Frontend impact

- **New routes:** none. `/recipes` gains an optional `?tag=` query parameter, read with `useSearchParams`, so a
  filtered view is linkable and survives a reload.
- **Changed components:**
  - `components/ui/Recipe/RecipeCard` — the first tag, as plain text, in the meta column beside the time.
    **Not a link**: the whole row is already a `<Link>` (ADR-024), and an interactive element nested in a link is
    invalid HTML that screen readers announce unpredictably.
  - `pages/RecipeDetail/RecipeDetailPage` — a kicker above the `<h1>`: each tag a `<Link>` to
    `/recipes?tag=<encoded tag>`, separated by a decorative `·` (`aria-hidden`), uppercased by CSS so the
    accessible name stays lowercase. Rendered only when the recipe has tags.
  - `pages/Recipe/RecipePage` and `components/ui/Recipe/RecipeList` — the tag filter (exact match, AND with the
    text search) and the search matching tags as text.
  - Active filter: a chip showing the tag, with a clear button carrying `aria-label="Clear tag filter: <tag>"`.
- **New/changed hooks:** none. Filtering is derived in render from `useRecipes` data (the `useMemo` pattern
  `RecipeList` already uses), never stored in state.
- **States:**
  - No tags: the row shows no tag; the detail shows no kicker.
  - `?tag=` matching nothing: the existing empty-results state, worded for the filter, with the clear control.
  - Loading and error: unchanged.
- **Design tokens:** existing only — the kicker uses `--accent` and the mono stack already used by the design's
  kickers.

## 9. Architecture impact

- **ADR required:** yes → **ADR-026 — Tags are a bounded `varchar[]` on `"Recipes"`, normalised in the domain**.
- **New dependency:** none.
- **Layer/dependency changes:** none.
- **New DI registrations:** none — no new handler, command, or query.
- **Mapping:** `RecipeConfiguration` adds
  `PrimitiveCollection(r => r.Tags).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Field)` with the element
  type bounded to 40 characters, and `ToTable(t => t.HasCheckConstraint("CK_Recipes_Tags_Count", ...))`. The
  generated migration must be read to confirm the column is `character varying(40)[]`; if Npgsql emits `text[]`,
  the element bound is missing and the spec is not met.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| **`varchar(40)[]` primitive collection (chosen)** | One column, order kept without a `Position`, no join, mapping in one line; precedent in `InstructionStep.IngredientIds` | — |
| Owned child table `"RecipeTags"` (`RecipeId`, `Position`, `Name varchar(40)`, unique `(RecipeId, Name)`) | A B-tree index on `Name` and database-enforced dedupe; the shape `Ingredients` and `Instructions` use | A third join on every recipe read, a `Position` column and key mapping, all for a list of plain strings. ADR-022's own test — move to a table once each element needs its own fields — is not met. Its index advantage only pays off with server-side filtering, deferred to `R-11` |
| Same column plus a GIN index now | `R-11` inheriting a ready index for `'roast' = ANY("Tags")` | Index maintenance on every write for a query nothing runs. `R-11` adds it in the same migration that adds `?tag=`, where its cost has a reason |
| `Tag` aggregate with a many-to-many join (a shared catalogue) | Rename once, everywhere; tag counts; autocomplete | A second aggregate forces the unit-of-work decision ADR-006 deferred, and contradicts the settled "no ingredient catalogue" reasoning: tags are owned by their recipe |
| A `Tag` value object instead of `string` | Normalisation enforced by the type, everywhere a tag is built | EF needs a value converter on every array element to unwrap it, for one private normaliser's worth of protection. Revisit if tags ever gain behaviour beyond normalising |

- **Pattern applied:** primitive collection on an aggregate root, with normalisation in the aggregate's factory
  (the aggregate owns its invariants). Existing example: `InstructionStep.IngredientIds` →
  `RecipeConfiguration.cs`.
- **What this makes harder:**
  - "Every recipe tagged *roast*" in SQL is an array predicate (`= ANY`), not a join, and stays a sequential
    scan until `R-11` adds a GIN index.
  - Renaming a tag across all recipes needs `array_replace` in an `UPDATE`, outside the aggregate.
  - PostgreSQL ignores array dimensions in a column type, so the count limit lives in a `CHECK` constraint that
    EF knows only by name. Changing the limit is a migration plus a validator change, in step.

## 10. Security impact

- **New user-controlled input:** `tags`. Bounded by FluentValidation (400) **and** the database (element type
  and `CHECK`), so the field is deploy-gate compliant from the start rather than adding to `SEC-08`.
- **User content rendered in the SPA:** yes — as React text nodes and inside `Link` paths. The `to` value is
  built with `encodeURIComponent`, so a tag such as `a&b` or `x/y` cannot break or redirect the route. No
  `dangerouslySetInnerHTML`.
- **File upload:** no.
- **Auth/ownership:** none exists (`R-14`); anyone can tag or untag any recipe, as they can edit any field today.
- **Config/secrets touched:** none.
- **Standing gaps affected:** `SEC-07` worsens marginally — each unpaginated list payload grows by up to 20×40
  characters per recipe.

## 11. Acceptance criteria

- [ ] Given a create request with `tags: ["  Roast ", "roast", "Feeds   A Table"]`, when it is posted, then
      201 and the response and database both hold `["roast", "feeds a table"]`.
- [ ] Given a create request with `tags: []`, then 201 and `tags: []`.
- [ ] Given a request with no `tags` property or `tags: null`, then 400. (Which layer rejects it — model
      binding's implicit `[Required]` for a non-nullable reference, or FluentValidation's `NotNull` — is pinned by
      the integration test, not assumed here.)
- [ ] Given 21 tags, then 400. Given a 41-character tag, then 400.
- [ ] Given a tag of `"   "`, then 422 with `field: "tags"` — in a draft too.
- [ ] Given a `PUT` with `tags: ["weeknight"]` on a recipe tagged `["roast"]`, then 204 and the recipe is tagged
      `["weeknight"]` only.
- [ ] Given an invalid `PUT`, then nothing about the recipe changes, tags included.
- [ ] Given a recipe that existed before the migration, when read, then `tags: []`.
- [ ] Given a direct SQL insert of a 41-character tag or of 21 tags, then PostgreSQL rejects it.
- [ ] Given a recipe tagged `["roast", "chicken"]`, then its list row shows "roast" and its detail screen shows
      both as links to `/recipes?tag=roast` and `/recipes?tag=chicken`.
- [ ] Given `/recipes?tag=roast`, then only recipes tagged exactly `roast` are listed, the active filter is shown,
      and clearing it removes `?tag=` and lists every recipe.
- [ ] Given `/recipes?tag=roast` and the search "lemon", then only recipes matching both are listed.
- [ ] Given the search "roast" and a recipe tagged `roast` whose title and description do not contain it, then
      that recipe is listed.

## 12. Test plan

- **Domain unit tests** (`RecipeTests`): normalisation table as a `[Theory]` (trim, collapse, lowercase,
  dedupe-keeps-first, order kept); blank tag → `TagRequired` in both tiers, reported once; `Update` replaces tags
  and mutates nothing on failure; the `Tags` getter returns a copy.
- **Validator unit tests:** null list, 21 items, null item, 41 characters → failure; 20 items of 40 → success —
  for both `CreateRecipeCommandValidator` and `UpdateRecipeDtoValidator`.
- **Mapping unit test:** `MapToRecipeDto` carries tags in order.
- **Handler unit tests:** existing create/update tests pass tags through; no new handler.
- **Integration tests** (`RecipesControllerTests`, Testcontainers): the round trip, the 400/422 cases, the `PUT`
  replacement, and a raw-SQL insert proving the element bound and the `CHECK` exist in the real schema —
  the one thing no unit test can see. Existing payload builders gain `tags`.
- **Contract test:** `OpenApiContractTests` snapshot regenerated.
- **Vitest:** `RecipeCard` (first tag shown, none when empty), `RecipeDetailPage` (kicker links, encoded href,
  absent when empty), `RecipePage`/`RecipeList` (`?tag=` filter, AND with search, clear, search matches tags).
- **Not covered, and why:** the back-fill of pre-existing rows is tested by reading a recipe seeded without
  touching tags, not by running the migration against a pre-migration database — the suite always migrates to
  head.
- **Manual verification:** create a tagged recipe through Swagger against local PostgreSQL; `\d "Recipes"` in
  psql shows `character varying(40)[]` and the check constraint; click a detail kicker tag in the SPA.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-026 + decisions-log entry |
| 3 | `02-senior-csharp` | domain, validators, DTOs, mapping, configuration, migration, backend tests (PR 1) |
| 4 | `08-api-contract` | regenerated snapshot and types, contract delta (PR 1) |
| 5 | `03-senior-react` | `RecipeCard`, detail kicker, `?tag=` filter (PR 2) |
| 6 | `06-qa-tester` | tests in §12 + coverage statement |
| 7 | `04-code-reviewer` | review of each PR |
| 8 | `05-security-reviewer` | review — triggered: new user input that is stored and rendered |

`07-ux-ui` is omitted: see §4.

## 14. Assumptions made

Decided with the user on 2026-10-03: client-side filtering only; lowercase + trim + collapse + dedupe; ≤ 20 tags
of ≤ 40 characters; Home's chips are not tags; storage as a `varchar(40)[]` with no index yet. Assumed without
asking:

- `tags` is required in requests, not optional (§7).
- Limits apply to raw input, before normalisation (§6).
- The list row shows only the **first** tag, as the design does; the rest are on the detail screen.
- `TagRequired` is reported once per recipe, like `IngredientsRequired`, rather than once per blank tag.

## 15. Follow-ups

- `R-11`: `?tag=` on `GET /api/recipes` with a GIN index on `"Tags"`, designed with pagination and the cache keys.
- `R-21`: the chip editor ("+ add") on the form, sending the normalised list back on `PUT`.

## 16. Known issues and roadmap items touched

- Fixes: `R-20`; known limitation #6 (tags half).
- Depends on: nothing open.
- Worsens: `SEC-07` (marginally, §10).
- On the [deploy gate](../roadmap.md#deploy-gate)? No — and it adds nothing to it, because the new column is
  bounded in the database from the start.
