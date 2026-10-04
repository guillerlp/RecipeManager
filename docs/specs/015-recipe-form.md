# Spec: Recipe add/edit form

| | |
| --- | --- |
| **ID** | `015` |
| **Status** | draft |
| **Author** | `00-leader` + `07-ux-ui` + `03-senior-react` |
| **Created** | `2026-10-04` |
| **Branch** | `feat/recipe-form` |

---

## 1. Context

The SPA can read recipes but not write them: `/recipes/new` is linked from the shell and falls through to the
404, there is no edit screen, and drafts (`R-19`), tags (`R-20`), structured ingredients (`R-10`) and structured
steps (`R-17`) can only be written through Swagger. Every dependency of `R-21` has shipped, so the form can be
built once against the final contract. It is also the SPA's first form and its first mutation, so it sets both
patterns for every later screen.

## 2. Goal

A user can create, edit, save as draft, publish, unpublish, and delete a recipe from the SPA, typing ingredients
the way they would say them.

## 3. In scope

**PR 1 — the form, end to end**

- [ ] `utils/ingredientLine.ts`: `parseIngredientLine` (spec 010 §9 plus the edge rules in §8.2) and
      `serialiseIngredient`, round-trip exact.
- [ ] `pages/RecipeForm/recipeForm.ts`: the form model — state, reducer, `fromRecipe`, `toRequest`,
      `validate` (§8.3).
- [ ] `utils/serverErrors.ts`: `mapServerErrors` for 422 `ProblemDetails` and 400 `ValidationProblemDetails`
      (§8.5).
- [ ] `hooks/useRecipeMutations.ts`: `useCreateRecipe`, `useUpdateRecipe`, `usePublishRecipe`,
      `useUnpublishRecipe`, `useDeleteRecipe`, each invalidating `['recipes']` (§8.4).
- [ ] `RecipeFormPage` at `/recipes/new` and `/recipes/:id/edit`, inside `AppLayout`: title as page title,
      description, prep/cook minutes with a formatted total, servings, ingredient rows, method steps with an
      optional duration and a per-step ingredient picker, tag chips, Save draft / Publish / Save / Unpublish /
      Delete / Discard, a save-status line, an error summary.
- [ ] Delete with a native `<dialog>` confirmation naming the recipe.
- [ ] `RecipeDetailPage` gains an **Edit** link to `/recipes/:id/edit`.
- [ ] `RecipePage` gains a Published / Drafts `SegmentedControl`, kept in the URL as `?status=draft`;
      `useRecipes(status)` and `recipeService.getAllRecipes(status)` pass `?status=`.
- [ ] ADR-027, decisions-log entry, docs in §16.

**PR 2 — reorder, preview, leave guard**

- [ ] Keyboard-accessible reorder for ingredient rows and method steps (move up / move down buttons, focus
      follows the moved row, the new position announced). Drag is optional on top, never the only way.
- [ ] The live list-row preview rail ("How it will look in the list"), reusing `RecipeCard`.
- [ ] Unsaved-changes guard: migrate `App.tsx` from `<BrowserRouter>` to `createBrowserRouter` +
      `RouterProvider` so `useBlocker` is available; block in-app navigation and `beforeunload` while the form
      is dirty. Discard confirms when dirty.
- [ ] `R-21` removed from the roadmap; spec status set to shipped.

## 4. Out of scope

- **The photo field.** It depends on `R-12`; the form leaves no placeholder for it.
- **Fraction glyphs as input** (`½ cup`). The parser accepts `1/2` and `1 1/2`; glyphs are a follow-up (§15).
- **Autosave.** Saving is explicit (§9, alternatives). "Draft saved 12:41" reports the last explicit save.
- **A form library.** Rejected in §9; adding one would need an `01-architect` ADR.
- **Any backend change.** The contract already supports every write. `08-api-contract` is involved only for the
  `getAllRecipes(status)` service signature (`QUAL-04`: the service file is hand-written).
- **Server-side `?tag=`, pagination** — `R-11`.
- **Updating the Claude Design helper text** for drafts (`UX-05`'s follow-up) — outside the repo; noted in the
  PR description, and the in-app helper text is written per §8.6.

## 6. Domain impact

- **New/changed entities:** none
- **New/changed properties on `Recipe`:** none
- **New/changed invariants:** none. The client mirrors the existing rules (§8.3) as a courtesy; the domain stays
  the authority.
- **New/changed shape validation:** none
- **Migration required:** no
- **Known limitations touched:** none

## 7. API impact

- **New/changed endpoints:** none. First SPA consumer of `POST`, `PUT`, `DELETE`, `POST …/publish`,
  `POST …/unpublish`, and of `GET /api/recipes?status=Draft`.
- **`RecipeDto` / `UpdateRecipeDto` changes:** none
- **Breaking for the client?** no
- **Cache impact (server):** none — existing writes already invalidate every `recipes_{status}` key and
  `recipe_{id}` (ADR-025).

## 8. Frontend impact

### 8.1 Routes and entry points

| Route | Screen | Reached from |
| --- | --- | --- |
| `/recipes/new` | `RecipeFormPage`, create mode | Header "New recipe" pill, `BottomNav`, Home |
| `/recipes/:id/edit` | `RecipeFormPage`, edit mode | **Edit** link on `RecipeDetailPage`; redirect after the first draft save |

`/recipes/:id/edit` nests under the detail URL. Edit mode loads through the existing `useRecipe(id)`, so it
inherits the GUID guard and the detail page's loading / not-found / error states. Both routes are declared
before `/recipes/:id` is matched — React Router 7 ranks static segments above params, so `new` never reaches
`useRecipe`.

### 8.2 Ingredient lines — `utils/ingredientLine.ts`

Each ingredient is one row holding a line of text. The line is parsed on every change and the parse is
previewed under the row (`→ 2 tbsp · butter, cold`); a failure is shown inline and blocks submit.

`parseIngredientLine(text)` returns `{ ok: true, value: { quantity, unit, name, notes } }` or
`{ ok: false, error }`. Rules, in order, against the trimmed line (spec 010 §9, unchanged):

1. A leading number — integer, decimal (`.` separator), `1/2`, or `1 1/2` — becomes `quantity`. No leading
   number: `quantity` and `unit` are null.
2. The next token is matched case-insensitively against a symbol table **derived from `UNIT_NAMES` in
   `utils/quantity.ts`** (`abbr`, `one`, `many` of each `Unit`), including the two-word `fl oz`. One record
   feeds both the display and the parser, so a new C# enum member fails the typecheck in one place.
3. Text after the first comma is `notes`; the rest is `name`. Both trimmed; empty `notes` is null.
4. `name` is required.

Edge rules this spec adds:

| Input | Result | Why |
| --- | --- | --- |
| `tbsp butter` | `(null, null, "tbsp butter", null)` | ADR-022 allows a unit only with a quantity; rule 2 only runs after rule 1 matched. |
| `0 eggs` | failure: "Quantity must be more than zero" | The domain rejects it (`IngredientQuantityMustBePositive`); say so before the round-trip. |
| `1/0 cup` | failure: "That fraction doesn't work" | Division by zero. |
| `2` | failure: "Add the ingredient's name" | Empty name. |
| `2 cups` | failure: "Add the ingredient's name" | Unit consumed, name empty. |
| `200000 g flour` | failure: "Quantity is too large" | Over the domain's 100000 cap. |
| Line over 200 characters of name or notes | failure naming the part | Mirrors the 200-character caps. |

`serialiseIngredient(ingredient)` turns a stored ingredient back into a line for edit mode. It writes the
**stored** quantity (`0.333`, not `⅓`; `12.34`, not `12`) and the unit's `abbr`, else its singular or plural
name. It must not reuse `formatQuantity`, which rounds for display: opening a recipe and pressing Save without
touching anything would otherwise rewrite its quantities. `parse(serialise(x))` equals `x` for every unit, and a
test pins it.

### 8.3 Form model — `pages/RecipeForm/recipeForm.ts`

Pure TypeScript, no React, owned by a `useReducer` in `RecipeFormPage`.

```ts
interface IngredientRow { key: string; id: string | null; text: string }
interface StepRow { key: string; text: string; duration: string; ingredientKeys: string[] }
interface RecipeFormState {
  title: string; description: string; prep: string; cook: string; servings: string;
  ingredients: IngredientRow[]; steps: StepRow[]; tags: string[];
}
```

- **Identity.** `key` is a client-only `crypto.randomUUID()`, used as the React `key` and as the target of
  step references; it is never sent. `id` is the server's ingredient id, echoed back on update, and `null` for
  a new row. The form never invents an `id` (`BUG-15`).
- **Steps reference ingredient keys**, not indexes and not ids. Removing an ingredient removes its key from every
  step in the **same** reducer action, so a dangling reference cannot exist in state.
- `fromRecipe(recipe)` builds state from a `RecipeDto`: each ingredient gets a key and its `id`, each step's
  `ingredientIds` become the matching keys.
- `toRequest(state)` builds the request body in **one pass**: it parses each line, records each row's index in
  the array it is building, and maps every step's `ingredientKeys` to those indexes (ADR-023). Indexes are
  computed against the exact array being sent, so no stale array exists to compute them against. `tags` is
  always sent (the field is required). Blank optional fields become `null`.
- `validate(state, intent)` with `intent` `'draft' | 'publish'` returns field errors. Always: title required
  and ≤ 200, description ≤ 1000, times integers 0–1439, servings 1–999, ≤ 50 ingredients, ≤ 50 steps, every line
  parses, step text required and ≤ 2000, step duration an integer 1–1439 when given, ≤ 20 tags, each ≤ 40. On
  `'publish'` also: description, both times, servings present; not both times zero; at least one ingredient and
  one step. Every limit above was checked on 2026-10-04 against `RecipeValidationRules`,
  `IngredientInputDtoValidator`, `InstructionStepInputDtoValidator`, and `Recipe.ValidateProperties`.
- Blank trailing rows (an empty ingredient line, an empty step) are dropped by `toRequest` rather than sent:
  the "add" row is always present and would otherwise fail every submit.

### 8.4 Mutation hooks — `hooks/useRecipeMutations.ts`

One file, five hooks, exported from `hooks/index.ts`. Each `onSuccess` returns
`queryClient.invalidateQueries({ queryKey: ['recipes'] })`, so the mutation stays pending until the refetch is
triggered. The prefix covers every list key and every detail key. `useDeleteRecipe` additionally calls
`removeQueries({ queryKey: ['recipes', id] })` so the dead detail entry is not refetched into a 404.

`useRecipes(status)` moves to the key `['recipes', 'list', status]`. The `'list'` segment keeps list keys and
the existing `['recipes', id]` detail keys from sharing a shape; both stay under the `['recipes']` prefix.

### 8.5 Server-error mapping — `utils/serverErrors.ts`

`mapServerErrors(error) → { fields: Partial<Record<FormField, string[]>>; form: string[] }`.

- **422** (`ProblemDetails`, `ResultExtensions.CreateProblemDetails`): `field` plus `detail`, or an `errors[]`
  of `{ message, field }` when there are several. Fields are section-level camelCase: `title`, `description`,
  `preparationTime`, `cookingTime`, `servings`, `ingredients`, `instructions`, `tags`, and the cross-field
  `preparationTime,cookingTime`, which maps to the **times group**.
- **400** (`ValidationProblemDetails`): `errors` keyed by property path (`Ingredients[2].Name`, `Title`). The
  first path segment, lower-camel-cased, picks the section.
- Anything unrecognised — no field, an unknown field, a 404, a network error — goes to `form`.

### 8.6 Screen

Layout, design frame 3d (light: empty new recipe; dark: mid-entry):

- **Header bar:** save-status line, **Discard**, primary action(s).
- **Main column:** the title `<input>` typeset with `--type-title` (placeholder "New recipe"), description
  textarea, the stats row (Hands on / In the oven / Serves / Total), Ingredients, Method, Tags.
- **Right rail** (≥ 1024px; stacked below on narrower screens): Tags in PR 1; the list-row preview joins it in
  PR 2.

Actions by state:

| Recipe state | Buttons | Behaviour |
| --- | --- | --- |
| New | **Save draft**, **Publish** | Save draft: `POST` with `status: "Draft"`, then `navigate('/recipes/:id/edit', { replace: true })`. Publish: `POST` with no status; a 422 marks every gap and creates nothing; success goes to the detail page. |
| Draft | **Save draft**, **Publish**, **Delete** | Save draft: `PUT`. Publish: `PUT`, then `POST …/publish`; a 422 from publish leaves the draft saved and marks every gap; success goes to the detail page. |
| Published | **Save**, **Unpublish**, **Delete** | Save: `PUT` — the server applies the published tier. Unpublish: `PUT` if dirty, then `POST …/unpublish`. |
| Any | **Discard** | `navigate(-1)`, or `/recipes` with no history. Confirms when dirty from PR 2. |

Copy (sentence case):

| Where | Text |
| --- | --- |
| Status line | "Not saved yet" / "Draft saved 12:41" / "Saved 12:41" / "Saving…" |
| Title helper, new recipe | "A title is all a draft needs. Publishing needs the rest." |
| Ingredients helper | "One per line, the way you'd say it — 2 tbsp butter, cold" |
| Ingredients footnote | "Stored as quantity · unit · name, so the recipe page can rescale them." |
| Time hint | "minutes, 0–1439" |
| Limits | "Up to 50" beside the Ingredients and Method headings; "up to 20" beside Tags |
| Step picker legend | "Ingredients used in step {n}" |
| Delete dialog | "Delete {title}?" / "This can't be undone." / **Delete** / **Cancel** |
| Error summary heading | "Fix {n} thing(s) before saving" / "… before publishing" |

States:

- **Loading** (edit mode): the detail page's loading treatment.
- **Not found / error** (edit mode): the detail page's not-found and error states, with `refetch()` retry.
- **Empty:** a new recipe — the form itself, with one empty ingredient row and one empty step.
- **Populated:** an existing recipe.
- **Saving:** buttons disabled with `aria-disabled` while a mutation is pending; the status line reads
  "Saving…".

Accessibility:

- The title input has a visually-hidden `<label>` ("Recipe title"); the page's `<h1>` is the input's visible
  text in edit mode, visually hidden "New recipe" in create mode, so the landmark structure stays intact.
- The status line is a `role="status"` region present from first render, so a save is announced.
- On a failed submit, focus moves to the error summary (`tabIndex={-1}`); each entry is a link to its field.
  Each invalid input has `aria-invalid="true"` and `aria-describedby` pointing at its message.
- The times-group error sits under both time inputs and both reference it.
- Each remove control's accessible name includes its target: "Remove butter", "Remove step 2",
  "Remove tag roast".
- The per-step ingredient picker is a `<fieldset>` of native checkboxes with a `<legend>`, listing each
  ingredient by parsed name.
- The delete confirmation is a native `<dialog>` opened with `showModal()`: focus trap, `Esc`, and an inert
  background without a library (ADR-014's stated trigger for one).
- Tag input commits on `Enter` or `,`; `Backspace` in an empty input does **not** delete the last chip (an
  invisible destructive shortcut).
- Prep/cook/servings/duration are `<input type="number" inputMode="numeric">` with `min`/`max`.

Design tokens: existing only. `--danger` for error text and the Delete button, `--field-border` for every input
boundary (`UX-06`).

### 8.7 Drafts toggle on `/recipes`

A `SegmentedControl` (Published / Drafts) above the list, bound to `?status=draft` (absent means Published),
composed with `?tag=` and the text search. Empty state for drafts: "No drafts. Save a recipe as a draft and
it'll wait here." The draft row reuses `RecipeCard`; a draft has nullable fields, which `RecipeCard` must render
honestly (no "0 min", no "serves null") — check, and fix in the same PR if it does not.

## 9. Architecture impact

- **ADR required:** yes → ADR-027 in [../architecture.md](../architecture.md): the form-model pattern (pure
  reducer, client keys, one-pass index translation), the mutation-hook invalidation pattern, and the data-router
  migration (PR 2).
- **New dependency:** none.
- **Layer/dependency changes:** none.
- **New DI registrations:** none.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| `useState` per field | The least code for a small form | Ingredient↔step references would spread across handlers and the key→index translation would live in the submit handler, the hardest place to test. |
| `react-hook-form` with `useFieldArray` | Dirty tracking, field arrays, registration for free | A new dependency (ADR) that solves the easy part: its field ids are its own, so step→ingredient mapping is still ours, and the parser still is. |
| Textarea, whole ingredient list re-parsed | Fastest typing of a full list | Rebuilds identity on every keystroke: ids must be re-matched heuristically, and step selections break when a line changes. |
| Structured fields per ingredient (qty / unit `<select>` / name / notes) | No parser ambiguity | Drops the design's "type it the way you'd say it" and leaves spec 010 §9 unused. |
| Autosave drafts | The design's "Draft saved" feel | A debounce, an in-flight race, a create-then-update state machine, and a write per pause; and a published recipe cannot autosave without going live mid-edit. |
| One "Save recipe" button that falls back to draft on 422 | Fewest controls | Silently saving a draft when the user meant to publish. |
| `/drafts` as its own route | Separation | A nav destination the design does not have; the list, card, and `?status=` already exist. |
| Hours + minutes time inputs | Matching the display | Four inputs for two values and a combined-validation edge, for an echo the formatted total already gives. |

- **Pattern applied:** reducer-owned form model with a pure serialisation boundary — the frontend counterpart
  of the anti-corruption mapper in `RecipeManager.Application/Mappings/InstructionMappingExtensions.cs`, which
  translates indexes into ids on the server side. Cache invalidation by query-key prefix (TanStack Query).
- **What this makes harder:** every new form field touches the state type, the reducer, `fromRecipe`,
  `toRequest`, and `validate`. Client-side rules duplicate server rules and can drift; the server stays the
  authority, and `mapServerErrors` renders whatever it says.

## 10. Security impact

- **New user-controlled input:** none new at the API; the SPA now writes every field Swagger already could.
  Validated by FluentValidation (shape) and the domain (rules), unchanged.
- **User content rendered in the SPA:** yes — titles, names, notes, steps, tags, in the form, the preview, the
  delete dialog. React escapes them; nothing uses `dangerouslySetInnerHTML`.
- **File upload:** no.
- **Auth/ownership implications:** none new. Anyone can still create, edit, publish, and delete any recipe,
  drafts included (`SEC-01`, `SEC-02`); a UI makes that easier to do, not more possible.
- **Config/secrets touched:** none.
- **Standing gaps affected:** `SEC-04` (no rate limiting on writes) becomes reachable from the UI; unchanged in
  substance.

## 11. Acceptance criteria

- [ ] Given `2 tbsp butter, cold`, when parsed, then `(2, Tablespoon, "butter", "cold")`; `salt to taste` gives
      `(null, null, "salt to taste", null)`; `3 eggs` gives `(3, null, "eggs", null)`; `1 1/2 cups flour` gives
      `(1.5, Cup, "flour", null)`.
- [ ] Given every `Unit` and a quantity like `0.333`, when serialised then parsed, then the result equals the
      input.
- [ ] Given ingredients A, B, C where step 1 uses C, when B is moved to the end and the form submitted, then step
      1's `ingredientIndexes` is the index of C in the sent array.
- [ ] Given step 1 uses B, when B is removed, then step 1 references nothing and the request is valid.
- [ ] Given an existing recipe, when its edit form is submitted unchanged, then the request carries the same
      quantities, units, names, notes, ingredient `id`s, step references, and tags it was loaded with.
- [ ] Given a new recipe with only a title, when **Save draft**, then `POST` with `status: "Draft"` and the URL
      becomes `/recipes/{id}/edit` without a new history entry.
- [ ] Given a new recipe with only a title, when **Publish**, then the client marks description, times,
      servings, ingredients, and method without sending a request.
- [ ] Given the server answers 422 with an `errors[]` of several fields, then every one is marked and listed in
      the summary, and focus is on the summary.
- [ ] Given 422 `field: "preparationTime,cookingTime"`, then the message appears once, under the times group.
- [ ] Given 400 with `errors: { "Ingredients[2].Name": [...] }`, then the message appears on the Ingredients
      section.
- [ ] Given a draft, when **Publish** and the publish call returns 422, then the draft's `PUT` has succeeded and
      every gap is marked.
- [ ] Given a saved recipe, when **Delete** is confirmed, then `DELETE` is sent, the user lands on `/recipes`, and
      the recipe is gone from the list without a reload.
- [ ] Given **Delete**, when the dialog is cancelled or `Esc` pressed, then nothing is sent and focus returns to
      the Delete button.
- [ ] Given any successful write, then the list and detail screens show the change without a reload.
- [ ] Given `/recipes?status=draft`, then only drafts are listed; the toggle is a radio group.
- [ ] Given the detail screen, then an **Edit** link leads to `/recipes/{id}/edit`.
- [ ] Given a tag typed with `Enter` or `,`, then it appears as a chip with a "Remove tag {name}" button.

## 12. Test plan

- **Domain / handler / integration tests:** none — no backend change. Suite stays at 282.
- **Frontend (Vitest + RTL):**
  - `utils/ingredientLine.test.ts` — the §8.2 table, every `Unit` symbol form, round-trip.
  - `pages/RecipeForm/recipeForm.test.ts` — reducer actions; `fromRecipe`/`toRequest` round-trip;
    index translation after reorder and removal; `validate` for both intents.
  - `utils/serverErrors.test.ts` — 422 single, 422 multiple, cross-field, 400, unknown/network.
  - `pages/RecipeForm/RecipeFormPage.test.tsx` — service mocked: create draft → redirect; publish blocked
    client-side; draft publish 422; edit loads and submits unchanged; delete confirm/cancel; error summary focus.
  - `RecipePage.test.tsx` — drafts toggle drives `?status=` and the service call.
  - `RecipeDetailPage.test.tsx` — Edit link.
- **Not covered, and why:** the real `<dialog>` modal behaviour (focus trap, inert background) — jsdom does not
  implement `showModal` faithfully; covered by manual verification.
- **Manual verification:** against a real API (WSL2): create a draft, reload, find it under Drafts, publish it
  with gaps, fill them, publish, edit, unpublish, delete — in light and dark, at 375px and desktop, keyboard only.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-027 |
| 3 | `08-api-contract` | `getAllRecipes(status)` signature; contract delta: none |
| 4 | `07-ux-ui` | §8.6 screen spec; review in both themes |
| 5 | `03-senior-react` | parser, form model, hooks, screen, toggle, Edit link |
| 6 | `06-qa-tester` | tests + coverage statement |
| 7 | `04-code-reviewer` | review |
| 8 | `05-security-reviewer` | review — the feature makes stored user input writable from the UI |

`02-senior-csharp` is not involved: no backend change.

## 14. Assumptions made

- The design's "Nothing here is required except the title" applies to drafts only; the helper copy in §8.6 says
  so (ADR-025).
- Decimal separator is `.` only; `1,5` would be read as quantity `1` with notes `5…`. Acceptable for an
  English-only UI; noted in the follow-ups.
- The detail page's loading/not-found/error treatment is reusable by the form without a shared component; if it
  is not, extract one in PR 1.

## 15. Follow-ups

- Fraction glyphs (`½`, `1¼`) as parser input — new known-issues entry.
- Comma decimal separator (`1,5 kg`) — same entry, or its own if it is decided separately.
- `UX-05`'s design-file helper text — PR description.

## 16. Known issues and roadmap items touched

- **Fixes:** completes `R-21` (PR 2 deletes its entry and marks row 6 shipped).
- **Docs updated in the same PR:** `architecture.md` (ADR-027), `decisions-log.md`, `domain-model.md`
  (frontend view: writes now exist), `agents/03-senior-react.md` ("Mutations do not exist yet", "no form in the
  codebase today"), `agents/07-ux-ui.md` ("Recipe form screens — not built yet", the `/recipes/new` note),
  `workflows/feature-workflow.md` (step 8.2 and 8.4 notes), `roadmap.md`, `known-issues.md`, `CLAUDE.md` (test
  counts, once CI confirms them).
- **Depends on:** nothing open.
- **On the [deploy gate](../roadmap.md#deploy-gate)?** no.
