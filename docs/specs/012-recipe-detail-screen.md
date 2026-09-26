# Spec: Recipe detail screen

| | |
| --- | --- |
| **ID** | `012` |
| **Status** | approved 2026-09-26 — not yet implemented |
| **Author** | `00-leader` + `07-ux-ui` |
| **Created** | `2026-09-26` |
| **Branch** | `feat/recipe-detail-screen` (PR 1), then `feat/units-preference` (PR 2) |

---

## 1. Context

The SPA can list recipes but cannot show one. `App.tsx` has no `/recipes/:id` route, so list rows link nowhere and
render as inert `<article>`s (`BUG-10`). Structured ingredients (ADR-022) and structured steps (ADR-023) now exist,
so the editorial design's Detail screen (frame 3c) finally has data to render, including the servings stepper
that rescales quantities. ADR-022 also made unit conversion a presentation concern and deferred the client-side
conversion table to this item; the metric/imperial preference that table serves does **not** exist yet (see §14).

## 2. Goal

Let a user open a recipe from the list and read it at any number of servings, with quantities shown in the unit
system they prefer.

## 3. In scope

Delivered as **two PRs**, decided with the user.

**PR 1 — the screen**

- [ ] Route `/recipes/:id` rendering `RecipeDetailPage` inside `AppLayout`
- [ ] `useRecipe(id)` query hook, key `['recipes', id]`, over the existing `recipeService.getRecipeById`
- [ ] Loading, offline, not-found, error, and populated states (§8.3)
- [ ] `ServingsStepper`, `IngredientRail`, `MethodSteps`, `RecipeDetailTabs` (mobile only) in `components/ui/Recipe/`
- [ ] `utils/quantity.ts`: `scaleQuantity`, `formatQuantity`, `unitLabel` (§8.4)
- [ ] `useMediaQuery` hook, used to render the tabs only below 768px
- [ ] `RecipeCard` becomes a React Router `<Link>` to `/recipes/{id}` with a resting chevron cue, closing `BUG-10`
- [ ] Print button and an `@media print` stylesheet for this recipe
- [ ] `.visually-hidden` utility in `styles/globals.css` (no such utility exists today)
- [ ] `BUG-07`: `[HttpGet("{id:guid}")]` on `RecipesController.Get`, plus an integration test
- [ ] ADR-024 and the doc updates listed in §16

**PR 2 — units**

- [ ] `UnitSystem` preference (`'asWritten' | 'metric' | 'imperial'`, default `'asWritten'`), a `UnitsProvider`
      context and `useUnits()` guard hook, persisted to `localStorage`
- [ ] `ThemeControl` generalised into a shared `SegmentedControl` radio group, reused for Theme and Units
- [ ] Units row in Settings (`ProfilePage`)
- [ ] `utils/units.ts`: the conversion table over `Unit` (§8.5), applied between scaling and formatting
- [ ] ADR-024 amended with the conversion policy; `R-18` deleted from the roadmap

## 4. Out of scope

Every element of frame 3c whose feature does not exist is **left out, not stubbed** — spec 009's rule: a control
that silently changes nothing is worse than an absent one.

- **Tag kicker** ("ROAST · FEEDS A TABLE") — `R-20`.
- **Photo slot** in the rail — `R-12`.
- **"Cooked N times"** stat — `R-22`.
- **Start cooking** (desktop button and mobile sticky bar) — `R-23`.
- **Edit** (desktop button and mobile header icon) and the mobile "more" menu — `R-21`.
- **Step durations** — present in `InstructionStepDto`, not shown in 3c; `R-23` uses them.
- **Print the whole catalogue** — needs a route that renders every recipe in full and leans on the unpaginated
  `GET /api/recipes` (`SEC-07`). Moved to a new roadmap item, `R-25`.
- **Servings in the URL** (`?servings=8`) — shareable scaled links were not asked for, and a URL parameter needs
  parsing and bounds-checking of its own.
- **Pre-filling the detail query from the list cache** — the API is local and fast; revisit if the loading state
  is visibly annoying.
- **Mass ↔ volume conversion** — needs per-ingredient density, which ADR-022's no-catalogue decision leaves
  nowhere to store.
- **Any backend change beyond `BUG-07`.** No DTO, endpoint, or contract change.

## 5. Decisions taken with the user (2026-09-26)

| # | Question | Decision |
| --- | --- | --- |
| 1 | The units preference does not exist — how does R-18 handle it? | **Two PRs**: the screen first, the preference and conversion second |
| 2 | What element is a clickable list row? | **`<Link>`**, not `<button>` + `navigate()` — reverses the fix `BUG-10` and `07-ux-ui` prescribed (§9) |
| 3 | Fold `BUG-07` into PR 1? | **Yes** — R-18 is the first consumer to send a user-typed id |
| 4 | How is frame 3c obtained? | Read from Claude Design in the browser pane; the relevant decisions are copied into §8 |
| 5 | How are rescaled quantities displayed? | **Kitchen fractions for tsp, tbsp, cup, fl oz; trimmed decimals for everything else** |
| 6 | What print support? | **This recipe only**; the whole catalogue becomes `R-25` |
| 7 | What makes a row look clickable? | **A resting chevron in `--ink-2`, plus a title underline on hover and focus** |
| 8 | How is the mobile Ingredients/Method switch built? | **WAI-ARIA tabs**, rendered only below 768px |
| 9 | Does a quantity with no unit ("2 lemons") scale? | **Yes.** Only a null quantity stays fixed. Corrects the roadmap sentence, which gave no reason and contradicts 3c |
| 10 | Units options and default? | **As written / Metric / Imperial, default As written** — departs from 3c's two options so nothing converts until asked |

## 6. Domain impact

- **New/changed entities:** none
- **New/changed properties on `Recipe`:** none
- **New/changed invariants:** none
- **New/changed shape validation:** none
- **Migration required:** no
- **Known limitations touched:** item 2 — PR 2 adds client-side conversion; the limitation's wording changes in
  [../domain-model.md](../domain-model.md), the domain itself does not

Scaling and conversion are **presentation only**. They never write back and never reach the server (ADR-022).
The screen relies on two existing invariants: every recipe has at least one ingredient and one step, and
`servings >= 1`, so `quantity × current / written` never divides by zero.

## 7. API impact

- **New/changed endpoints:**

  | Verb | Route | Request body | Success | Failures |
  | --- | --- | --- | --- | --- |
  | GET | `/api/recipes/{id:guid}` | — | 200 + `RecipeDto` (unchanged) | 404 not found — **now also for a malformed id** (was 400, `BUG-07`) |

- **`RecipeDto` / `UpdateRecipeDto` changes:** none
- **Breaking for the client?** No. The OpenAPI snapshot changes only if Swashbuckle records the route
  constraint — if it does, regenerate `contracts/openapi.json` and `src/types/generated/api.ts` in PR 1.
- **Cache impact:** none. `recipe_{guid}` is already written by `GetByIdAsync`. A malformed id no longer reaches
  the controller at all.

## 8. Frontend impact

### 8.1 Routes and data

- `/recipes/:id` → `RecipeDetailPage` (`pages/RecipeDetail/`), wrapped in `AppLayout` like every other route.
- `useRecipe(id)` in `recipe-manager-frontend/src/hooks/`, exported from the barrel. Key `['recipes', id]`, so the
  first mutation hook (`R-21`) invalidates it through the existing `['recipes']` prefix. Same `staleTime`/`gcTime`
  as `useRecipes`. **No retry on 404** — retrying a missing recipe only delays the not-found state.
- The loading state is gated on **`isPending`**, not `isLoading` (`BUG-12`'s lesson, applied to the new screen
  from the start; `BUG-12` itself, in `RecipeList`, stays open).

### 8.2 Components

| Unit | Bucket | Responsibility |
| --- | --- | --- |
| `RecipeDetailPage` | `pages/` | Reads `:id`, picks the state, owns `servings` (`useState`, seeded from `recipe.servings`, content keyed by `recipe.id` so it resets per recipe) |
| `ServingsStepper` | `ui/Recipe/` | Two native `<button>`s around an `<output>`, range 1–999 (the `Servings` validator's bounds) |
| `IngredientRail` | `ui/Recipe/` | Sticky `<aside>`: heading, stepper, ingredient list, scale note |
| `MethodSteps` | `ui/Recipe/` | `<ol>` of steps with visible numbers |
| `RecipeDetailTabs` | `ui/Recipe/` | WAI-ARIA tabs over the rail and the method, rendered only below 768px |
| `RecipeCard` | `ui/Recipe/` *(changed)* | A `<Link>`; the `onClick` prop and the `article`/`button` element switch are deleted |
| `useMediaQuery` | `hooks/` | Subscribes to one media query's `change` event — the same pattern `ThemeProvider` uses for `prefers-color-scheme` |

**Where `servings` lives.** It is local state because it describes this visit, not the recipe. Context was
rejected (nothing else reads it) and so was the URL (§4).

### 8.3 States and copy (frame 3c)

| State | Condition | Rendered |
| --- | --- | --- |
| Loading | `isPending` and fetching | "Loading recipe…" |
| Offline | `isPending` and `fetchStatus === 'paused'` | "You appear to be offline. The recipe will load when you reconnect." |
| Not found | 404 | Heading **"Recipe not found"**, body "It may have been deleted, or the link is wrong.", link "← All recipes" |
| Error | any other failure | "Couldn't load this recipe." and a **Retry** button calling `refetch()`. The raw error message is not shown — it can carry server detail (`SEC-05`) |
| Populated | data | Below |
| Empty | — | **Cannot occur**: the domain guarantees at least one ingredient and one step. Not built |

Populated, desktop (≥ 768px):

- Back link "← All recipes" to `/recipes`.
- Title (`--type-display`, serif) and description.
- Stats row, label/value pairs: **Total**, **Hands on** (`preparationTime`), **Cooking** (`cookingTime`),
  **Serves**. 3c labels cooking time "In the oven", which assumes an oven; "Cooking" is the honest label. A time
  stat whose value is 0 is omitted (the domain allows either time to be zero, not both). Durations use the
  existing `formatDuration`/`getISODuration` and `<time dateTime>`. **Serves shows the stepper's value**, so the
  page never shows two different servings counts.
- **Method** in the wide column: numbered steps.
- **Ingredients** rail on the right, sticky: heading "Ingredients" with the stepper beside it; each row is the
  amount (monospace, tabular) then the name, with `notes` after a comma; below the list, "Scaled for {n}. Change
  the number and every quantity follows."
- **Print** button below the method.

Populated, mobile (< 768px, frame 3c mobile):

- Back link, title, description, stats as above, stacked.
- Tabs **Ingredients** | **Method**, Ingredients selected by default. The stepper sits at the top of the
  Ingredients panel with the label "Scaled for".
- No sticky bottom bar (it would hold Start cooking, `R-23`).

### 8.4 Quantities — `utils/quantity.ts`

Pure functions, no React:

- `scaleQuantity(quantity, written, current)` → `quantity × current / written`; `null` stays `null`. A quantity
  with no unit scales like any other. Always computed from the **stored** quantity, never from a previously
  displayed value, so stepping 4 → 7 → 4 cannot drift.
- `formatQuantity(value, unit)`:
  - **`Teaspoon`, `Tablespoon`, `Cup`, `FluidOunce`** snap to the nearest of ⅛ ¼ ⅓ ⅜ ½ ⅝ ⅔ ¾ ⅞ and render as a
    whole number plus a vulgar-fraction glyph (`1¼`, `¾`, `2`). A positive value never renders as `0`; the
    smallest shown is `⅛`.
  - **Everything else**, including no unit: a decimal with trailing zeros trimmed — 2 places below 1, 1 place
    below 10, none from 10 up (`0.25`, `1.5`, `450`).
- `unitLabel(unit, value)`: abbreviations never pluralise — `g`, `kg`, `oz`, `lb`, `ml`, `l`, `tsp`, `tbsp`,
  `fl oz`; words do when the value is not 1 — `cup`, `piece`, `clove`, `pinch`, `slice`, `can`, `bunch`,
  `sprig`.
- Each amount is rendered twice: the visible short form `aria-hidden`, and a `.visually-hidden` long form
  (`1¼ tablespoons`), because screen readers read `tbsp` letter by letter.

### 8.5 Conversion — `utils/units.ts` (PR 2)

Pipeline: **scale → convert → format**. Conversion applies only when the preference is not `asWritten`.

| Stored unit | Metric | Imperial |
| --- | --- | --- |
| `Gram`, `Kilogram` | as written | `Ounce`, or `Pound` at ≥ 1 lb |
| `Ounce`, `Pound` | `Gram`, or `Kilogram` at ≥ 1000 g | as written |
| `Millilitre`, `Litre` | as written | `FluidOunce`, or `Cup` at ≥ ¼ cup |
| `FluidOunce`, `Cup` | `Millilitre`, or `Litre` at ≥ 1000 ml | as written |
| `Teaspoon`, `Tablespoon` | never — both systems use them | never |
| `Piece`, `Clove`, `Pinch`, `Slice`, `Can`, `Bunch`, `Sprig`, none | never | never |

Factors: 1 oz = 28.3495 g, 1 lb = 453.592 g, **US customary** 1 fl oz = 29.5735 ml, 1 cup = 236.588 ml. When
any ingredient was converted, the rail note gains "Converted from the recipe's own units."

Settings row (PR 2): title **"Units"**, description **"How quantities are shown"** — 3c says "shown and stored",
which contradicts ADR-022. Options **As written / Metric / Imperial**.

### 8.6 Accessibility

- **Stepper:** `role="group"` with `aria-label="Servings"`; buttons named "Decrease servings" / "Increase
  servings", `disabled` at 1 and 999; the value is an `<output aria-live="polite">`.
- **Tabs:** the WAI-ARIA tabs pattern — `role="tablist"`/`tab`/`tabpanel`, `aria-selected`, `aria-controls`,
  `aria-labelledby`, roving `tabindex` (only the selected tab is in the tab order), Left/Right wrap, Home/End,
  and the inactive panel carries `hidden`. Hand-written ARIA, because HTML has no native tabs element; that is
  why it is tested with the keyboard (§12).
- **Rows:** a real link — middle-click, open in new tab, and copy link work, and a screen reader announces
  "link". Its accessible name is the recipe title; the chevron is `aria-hidden`.
- **Row cue (`BUG-10`):** the chevron is visible at rest, in `--ink-2`, so touch users can identify the control
  too. The title underlines on `:hover` and `:focus-visible`; the app-wide focus outline stays. The chevron's
  ratio is measured against `--paper` and `--paper-2` in both themes and recorded, per ADR-021; it must reach
  3:1. A fill change (`--paper-2` on `--paper`, 1.09:1) and `--rule` (1.29:1) cannot carry it.
- **Rail amounts in `--accent`** are measured on the rail's surface in both themes; if they fail 4.5:1, they use
  `--ink`.
- **Settings Units control (PR 2):** a native radio group, the `ThemeControl` pattern.

### 8.7 Print

The Print button calls `window.print()`; its icon is the Material "print" path copied into `components/ui/Icon/`.
The `@media print` block hides the header, navigation, footer, back link, stepper buttons, tabs, and the Print
button; lays the page out in one column with ingredients before the method; and prints black on white. It
prints at the **currently selected** servings (and, after PR 2, the selected units).

### 8.8 Tokens

Existing tokens only. No new colour token.

## 9. Architecture impact

- **ADR required:** yes → **ADR-024** in [../architecture.md](../architecture.md), written in PR 1 (detail screen,
  rows as links, scaling rules, mobile-only tabs) and amended in PR 2 (conversion policy).
- **New dependency:** none
- **Layer/dependency changes:** none
- **New DI registrations:** none

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Row as `<button>` + `navigate()` (what `BUG-10` prescribed) | Reusing `RecipeCard`'s existing element switch | Navigation on a button loses middle-click, open-in-new-tab, copy-link, and the URL preview, and is announced as an action rather than a destination |
| Servings in the URL | Shareable, reload-proof scaled links | Not asked for; adds parsing and bounds-checking for a value that describes one visit |
| Servings in context | Access from anywhere | Nothing outside the page reads it |
| Tabs always rendered, hidden by CSS on desktop | No media-query hook | Desktop screen readers would meet `tabpanel`s that do not behave as tabs |
| Mobile radio group (the `ThemeControl` pattern) | Zero hand-written ARIA | Announced as radio buttons, which is not what switching panels is |
| Stack ingredients above the method on mobile | Least code | Departs from 3c and makes a cook scroll past every ingredient to reach step 1 |
| Decimals everywhere | The simplest formatter | `0.33 cup` is not something anyone measures |
| Metric/Imperial only, default Metric (3c as drawn) | Matching the design | Every imperial-entered recipe would appear converted without the user asking |
| Pre-fill the detail query from the list cache | No spinner when arriving from the list | Extra code to hide a spinner that barely shows against a local API |

- **Pattern applied:** *derived state* (scaled and converted quantities are computed during render, never
  stored) — existing example: `RecipeList.filteredRecipes`; *context + guard hook* (PR 2) — existing example:
  `contexts/ThemeContext.ts` and `hooks/useTheme.ts`; *WAI-ARIA tabs* — first use in this repo.
- **What this makes harder:** the row is now always a link, so a future "select rows" mode needs a different
  element; the tabs are the first hand-written ARIA widget, so regressions are caught only by keyboard tests; US
  customary cups are an assumption a UK or Australian reader would not share.

## 10. Security impact

- **New user-controlled input:** the `:id` route parameter. It is interpolated into the request path; a
  malformed value is now a 404 at routing (`BUG-07`). No other input.
- **User content rendered in the SPA:** yes — title, description, ingredient names and notes, step text. All are
  rendered as React text children, which React escapes. No `dangerouslySetInnerHTML`.
- **File upload:** no
- **Auth/ownership implications:** none new — every recipe is already world-readable (`SEC-01`, `SEC-02`).
- **Config/secrets touched:** none. PR 2 stores a UI preference in `localStorage`, which holds no secret.
- **Standing gaps affected:** none. The error state deliberately does not render the server's message, so
  `SEC-05` does not gain a new display surface.

## 11. Acceptance criteria

- [ ] Given the recipes list, when a row is activated (click, Enter, or middle-click), then `/recipes/{id}` opens
      for that recipe.
- [ ] Given a list row at rest, then a chevron identifies it as a control, with ≥ 3:1 contrast in both themes.
- [ ] Given `/recipes/{id}` for an existing recipe, then title, description, non-zero times, servings, every
      ingredient, and every step render, ingredients and steps in stored order.
- [ ] Given a recipe written for 4, when servings is set to 8, then every non-null quantity doubles, including
      quantities with no unit, and null-quantity ingredients are unchanged.
- [ ] Given servings stepped 4 → 7 → 4, then every displayed amount equals the original.
- [ ] Given 1 tsp scaled by 5/4, then `1¼ tsp` is shown; given 1.6 kg scaled by 5/4, then `2 kg`.
- [ ] Given servings is 1, then "Decrease servings" is disabled; given 999, "Increase servings" is disabled.
- [ ] Given an unknown well-formed id, then "Recipe not found" renders with a link to `/recipes`.
- [ ] Given `/recipes/not-a-guid`, then the API answers 404 (not 400) and the screen shows "Recipe not found".
- [ ] Given a network failure, then "Couldn't load this recipe." renders and Retry refetches.
- [ ] Given a viewport under 768px, then Ingredients/Method tabs render; arrow keys, Home, and End move between
      them; and only the selected panel is visible.
- [ ] Given a viewport of 768px or more, then no tablist is in the DOM and both columns show.
- [ ] Given the print dialog, then only the recipe prints, in one column, at the selected servings.
- [ ] *(PR 2)* Given the preference is As written, then no quantity is converted.
- [ ] *(PR 2)* Given Metric and an ingredient of 8 oz, then about `227 g` is shown; given Imperial and 500 ml,
      then `2⅛ cups` (2.11 cups, snapped); given either system and 1 tbsp, then `1 tbsp`.
- [ ] *(PR 2)* Given a stored preference, when the app reloads, then it is still selected; given an unrecognised
      stored value, then As written applies.

## 12. Test plan

- **Frontend unit (`utils/quantity.test.ts`):** table-driven — null quantity, unitless scaling, the 4 → 7 → 4
  round trip, fraction snapping (0.75 → ¾, 1.25 → 1¼, 0.34 → ⅓, 0.01 → ⅛, 1.97 → 2), the three decimal bands,
  pluralisation.
- **Frontend unit (`utils/units.test.ts`, PR 2):** every row of §8.5, including the never-convert units and both
  thresholds.
- **Components:** `ServingsStepper` (bounds, clicks); `RecipeDetailTabs` (keyboard, `hidden`, roving
  `tabindex`); `RecipeList` (a row is a link to `/recipes/{id}`).
- **Page:** `RecipeDetailPage` with `@/services` mocked through `vi.mock`, as `RecipeList.test.tsx` does —
  loading, 404, error with retry, populated, and servings changing the displayed amounts.
- **Backend integration:** `GET /api/recipes/not-a-guid` returns 404. Needs Docker, so it runs on CI.
- **Not covered, and why:** print output (jsdom has no print layout — verified manually); contrast ratios
  (measured and recorded manually, as in ADR-021); visual layout (`UX-07`, no visual-regression tooling).
- **Manual verification:** in the browser against the local API, both themes, at 375px and ≥ 1024px — open a
  recipe from the list, step servings, switch tabs with the keyboard, print preview, visit a bad id.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-024 |
| 3 | `07-ux-ui` | §8.3–§8.8, contrast measurements, review in both themes |
| 4 | `02-senior-csharp` | `BUG-07` route constraint and its integration test |
| 5 | `03-senior-react` | route, hooks, components, utils, print stylesheet |
| 6 | `06-qa-tester` | tests in §12 |
| 7 | `04-code-reviewer` | review |
| 8 | `05-security-reviewer` | review of §10 — user content is rendered |

`08-api-contract` is not involved unless the OpenAPI snapshot changes (§7).

## 14. Assumptions made

- **The units preference never shipped.** Spec 010 §alternatives says "`R-16` already shipped the metric/imperial
  toggle in Settings"; spec 009 §4 says it was "Not rendered, not even disabled", and `ProfilePage` agrees. Spec
  010 is corrected in PR 1.
- Stepper bounds are 1–999, the `Servings` validator's bounds, so the stepper cannot show a count the API would
  refuse to store.
- Cups and fluid ounces are US customary.
- The "Cooking" label replaces 3c's "In the oven".

## 15. Follow-ups

- **`R-25` Print the whole catalogue** — new roadmap item (§4).
- **`BUG-12`** stays open for `RecipeList`; this screen avoids it but does not fix it.
- **The design file** should drop "and stored" from the Units description and gain the As written option, so it
  stops contradicting ADR-022.

## 16. Known issues and roadmap items touched

- Fixes: `BUG-07`, `BUG-10` (both deleted from [../known-issues.md](../known-issues.md) in PR 1); `R-18` (deleted
  from [../roadmap.md](../roadmap.md) in PR 2).
- Updates: `UX-06` (its live-consequence note moves from `BUG-10` to the chevron's measured ratio); the roadmap's
  `R-18` sentence on unitless quantities; spec 010's claim about `R-16`; known limitation 2 in
  [../domain-model.md](../domain-model.md) (PR 2); the a11y checklists in
  [../agents/07-ux-ui.md](../agents/07-ux-ui.md) and [../agents/03-senior-react.md](../agents/03-senior-react.md)
  ("interactive cards are `<button>`" becomes "navigating rows are links"); test counts in `CLAUDE.md` and
  `README.md`.
- Adds: `R-25`.
- Depends on: nothing open.
- On the [deploy gate](../roadmap.md#deploy-gate)? No.
