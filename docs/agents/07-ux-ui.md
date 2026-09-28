# Agent: UX / UI

## Role

Owns the visual system and interaction design: design tokens, layout, states, theming, and accessibility
requirements.

**Does:** specify new screens before they are built, decide token usage and when a new token is needed, define
loading/empty/error states, set the accessibility bar, review implemented screens in both themes.

**Does not:** write React (`03-senior-react`), introduce a UI dependency without `01-architect`, or run the
security checklist. UX produces a specification and a review, not components.

> This agent is kept despite there being no separate designer, because the repo has a real token system, a
> light/dark theme switch, and consistent accessibility work that would otherwise erode.

## When it activates

- Any new screen or route.
- Any new reusable component in `components/ui/` or `components/common/`.
- Any change to `styles/themes/*` or `styles/globals.css`.
- Any change to loading, empty, or error presentation.
- Accessibility findings from `04-code-reviewer`.

Not needed for: pure logic changes in a hook, or a component whose visuals do not change.

---

## Canonical design reference

The target visual design for the whole app is the **editorial design** in Claude Design:
[Recipe Manager - Editorial App](https://claude.ai/design/p/01d38965-d8a7-4a03-8a5b-84c32973a5cf?file=Recipe+Manager+-+Editorial+App.dc.html)
(frames 3a–3g). It covers six screens (Home, Recipes, Detail, Add/Edit, Profile & settings, Cooking mode), each in
light and dark, plus mobile at 390×844 for Recipes, Detail, and Cooking mode. The same project holds
`Recipe Manager - Current UI` (the app as it stood on 2026-09-13) and `Recipe Manager - New Screens` (earlier,
superseded explorations).

How to use it:

- **It is the target, not the current state.** The rest of this file describes what exists. The design is where
  `R-16`–`R-24` in [../roadmap.md](../roadmap.md) are heading, and each item says which frames it implements.
- **The design does not overrule the domain.** Where its copy or behaviour contradicts
  [../domain-model.md](../domain-model.md), the domain wins until a roadmap item changes it. The known case is
  `UX-05`: the design says only the title is required.
- **Its implementation choices are not decisions.** The design loads Newsreader and Material Symbols from Google
  Fonts. The project self-hosts fonts and keeps inline SVG icons instead (`R-16`).
- **It is external and can change.** A frame's link can go stale or be edited. When a screen is specified from it,
  copy the relevant decisions (tokens, copy, states) into the spec, not only the link.

---

## The design system as it exists

### Tokens — `recipe-manager-frontend/src/styles/themes/`

`variables.css` (`:root`, theme-independent):

| Group | Tokens |
| --- | --- |
| Font families | `--font-serif` (`'Newsreader Variable', Georgia, serif` — user-written content), `--font-ui` (`system-ui` — interface text), `--font-mono` (labels and tabular quantities) |
| Type scale | `--type-display`, `--type-title`, `--type-recipe-title`, `--type-body`, `--type-ui`, `--type-label` — the six type roles (spec [009](../specs/009-editorial-design-system-and-shell.md) §8.1), each a `font` shorthand carrying weight/size/line-height/family in one declaration |
| Tracking | `--tracking-display` `-.02em`, `--tracking-label` `.12em` |
| Spacing | `--spacing-xs` .25rem, `-sm` .5rem, `-md` 1rem, `-lg` 1.5rem, `-xl` 2rem, `-2xl` 2.5rem |
| Radius | `--radius-sm` 4px, `-md` 8px, `-lg` 12px, `--radius-pill` 999px (the editorial shell's default for buttons, nav links, fields) |
| Transition | `--transition-fast` .15s, `-normal` .2s, `-slow` .3s (all `ease`) |

**Breakpoints are documented constants, not tokens** (`UX-03`, closed by ADR-021): `480px`, `768px`, `1024px`,
declared as a comment block in `variables.css` and used literally in media queries. A custom property cannot
appear in a media query's condition — `@media (max-width: var(--bp-md))` is invalid CSS, because custom
properties are not resolved at that point in the cascade.

`light.css` / `dark.css` (`[data-theme="light"]` / `[data-theme="dark"]`) — **identical key sets**, so every
colour token resolves in both themes. This is the editorial paper/ink palette (spec 009 §8.2, ADR-021), not the
old generic blue-on-white one:

| Group | Tokens |
| --- | --- |
| Surface | `--paper`, `--paper-2` |
| Text | `--ink`, `--ink-2`, `--ink-3` |
| Structure | `--rule` (decorative hairline only — 1.29:1 light / 1.36:1 dark, never a control boundary, `UX-06`), `--field-border` (3:1+, the WCAG 1.4.11 boundary for inputs and controls) |
| Brand | `--accent`, `--accent-text` (the text colour that is always safe on `--accent` — white fails 2.54:1 on the dark accent, so this is a token rather than a per-theme guess) |
| Status | `--danger` |

Every ratio in `light.css`/`dark.css` is measured against the surface the token is actually used on and recorded
in a comment beside it. Three values deviate from the canonical design file for a measured contrast reason —
see ADR-021 for the ratios.

Rules:

- [ ] **Never hard-code a colour, spacing value, radius, or transition duration.** Use the token.
- [ ] A new colour token must be added to **both** `light.css` and `dark.css` — an asymmetric token silently
      resolves to nothing in one theme.
- [ ] `--rule` is decorative only. Anything a user must find and click — an input, a button outline, a card
      boundary — takes `--field-border` instead (`UX-06` in [../known-issues.md](../known-issues.md)).
- [ ] A colour value that will be read at more than one size or against more than one surface needs its ratio
      measured against each — `--accent`, `--danger`, and `--field-border` all needed a value ADR-021's canonical
      design did not use, because a value that passes on `--paper` can fail on `--paper-2` or the other theme.

### Typography — the six-role type scale

`styles/typography.module.css` exposes one CSS Modules class per type role (`display`, `title`, `recipeTitle`,
`body`, `ui`, `label`), each applying the matching `--type-*` token, plus a `numeric` utility class
(`font-variant-numeric: tabular-nums`) for quantities and durations so digits do not jitter between rows. A
component consumes a role through CSS Modules' `composes:`, not a global class:

```css
.pageTitle { composes: title from '@/styles/typography.module.css'; }
```

`composes:` keeps CSS Modules' scoping — the class still resolves to a module-local, hashed name — while sharing
the declaration, which is why it was chosen over a global utility class (ADR-021's rejected alternative: a
second, unscoped styling system beside CSS Modules). `globals.css` no longer sets a heading size of its own
(`UX-02`, closed): headings inherit nothing and every screen composes the role it needs.

### Theming mechanism

The preference the user chose and the theme actually rendered are two separate, deliberately typed values
(`recipe-manager-frontend/src/types/theme.ts`):

```ts
export type ThemePreference = 'light' | 'dark' | 'system'; // what the user chose, persisted to localStorage
export type Theme = 'light' | 'dark';                      // what is rendered, on <html data-theme>
```

`ThemeProvider` stores only the **preference**; `theme` is *derived*, never stored, so the two cannot drift
apart the moment the OS changes: `preference === 'system' ? (matchMedia('(prefers-color-scheme: dark)').matches
? 'dark' : 'light') : preference`. It subscribes to that media query's `change` event, so a preference of
`system` follows the OS live while the app is open, not only at load. `data-theme` is still set inside a
`useLayoutEffect`, so there is no flash of the wrong theme once React has mounted — the window before that
first commit is not covered: `index.html` sets no `data-theme` and neither theme file has a
`prefers-color-scheme` fallback, so a dark-OS visitor can briefly see a browser-default frame. An inline script
in `index.html` would close it; none is added. A visitor with nothing in
`localStorage` now defaults to `system` rather than `light`; an existing stored `light` or `dark` is treated as
an explicit choice and is not migrated. An unrecognised stored value falls back to `system` rather than
throwing. This settles `DEC-06` in [../known-issues.md](../known-issues.md) and is ADR-021 (PR 1 of `R-16`).

- [ ] Never read or set `data-theme` directly from a component — go through `useTheme()`.
- [ ] The three-option control is a **radio group, not three buttons**: arrow keys move between options
      natively and the selected one is announced as selected. `role="switch"` no longer fits — a switch is
      binary and this has three states.
- [ ] **The control lives in Settings (`ProfilePage`), not the `Footer`.** `R-16` PR 3 shipped
      it as `ThemeControl`; since `R-18` PR 2 it is the generic `components/ui/SegmentedControl` (three native
      radios in a `fieldset role="radiogroup"`), fed `preference` and `setPreference` by `ProfilePage` — there is no separate binary toggle and no `toggleTheme` to reach
      for. `Footer` now only links to `/profile`; the switch it used to render is gone.

### Styling approach

CSS Modules per component (`Foo.module.css`), plus `globals.css` for resets and base typography. There is **no
component library** (ADR-014), so every visible element takes its colours from these tokens. Icons are plain SVG
in `components/ui/Icon/` with `fill: currentColor`, so they follow the surrounding text colour in both themes.
Introducing a UI library is an `01-architect` decision.

---

## Screen specification checklist

Before `03-senior-react` writes a screen, specify:

- [ ] **Route** and where it is reachable from. (`/recipes/new` is linked from `HomePage` but has no route —
      do not repeat that.)
- [ ] **Layout**: does it sit inside `AppLayout` (Header + main + Footer)? Every current route does.
- [ ] **All four states**: loading, error, empty, populated. `RecipeList` is the reference and additionally
      distinguishes *empty because no recipes exist* from *empty because the search matched nothing* — new
      list screens must do the same.
- [ ] **Error recovery**: what the user can actually do. Prefer a `refetch()` action over a full page reload.
- [ ] **Responsive behaviour**: which reflows, at which of the three documented breakpoints (`480px`, `768px`,
      `1024px` — see the Tokens section above, `UX-03` closed by ADR-021).
- [ ] **Copy**: exact strings, sentence case, English.
- [ ] **Both themes**: reviewed in light and dark.

### Recipe form screens (create/edit) — not built yet

The highest-value pending UX work. Specify before implementation:

- [ ] **Dynamic ingredient and instruction lists.** Both are ordered `string[]`. Needs add / remove / reorder,
      an obvious affordance for each, and a keyboard-accessible reorder mechanism (drag-only is not acceptable).
- [ ] **Instruction steps are ordered and the order is meaningful** — number them visibly so a reorder is
      verifiable by the user.
- [ ] **Limits shown before submission**: 50 ingredients, 50 instructions, title ≤ 200, description ≤ 1000,
      times 0–1439 minutes, servings 1–999.
- [ ] **The "not both times zero" rule is a cross-field error.** It cannot be shown next to a single input —
      specify where it appears (the server returns `field: "preparationTime,cookingTime"`).
- [ ] **Time input in minutes.** Decide whether the user types minutes or hours+minutes; `RecipeCard` displays
      `1h 30min`, so an input that only accepts raw minutes will feel inconsistent.
- [ ] **Server-error mapping**: 422 errors arrive as `ProblemDetails` with a camelCase `field` and an `errors[]`
      extension; 400 errors arrive as `ValidationProblemDetails` with a different shape. Both must render
      against the right inputs.
- [ ] **Destructive actions** (remove an ingredient, delete a recipe) need confirmation and an accessible name
      that includes what is being removed.

---

## Accessibility bar — already met, must not regress

The codebase does all of this today; treat it as the minimum, not the goal.

- [ ] Semantic landmarks: `header`, `nav`, `main` (`AppLayout` sets `role="main"`), `footer`, `section`,
      `article`, `aside`.
- [ ] Icon-only and ambiguous controls have `aria-label` (e.g. `"Search recipes"`,
      `"View {title} recipe"`).
- [ ] A control with more than two states that are mutually exclusive is a native radio group
      (`fieldset role="radiogroup"` with a `<legend>`), not a custom widget — `SegmentedControl` is the reference (Theme and Units both use it).
      `role="switch"` is for a genuinely binary toggle only; none exists in the app today.
- [ ] Active navigation sets `aria-current="page"` (`NavLink`).
- [ ] Decorative icons inside a labelled control are `aria-hidden="true"`.
- [ ] Durations are marked up as `<time dateTime="PT1H30M">` (`RecipeCard.getISODuration`).
- [ ] Images have descriptive `alt`, plus `loading="lazy"` and `decoding="async"`.
- [ ] A row that navigates is a `<Link>` (`RecipeCard`); a `<button type="button">` is for an action on the
      current page. Never `<div onClick>` (ADR-024).
- [ ] Hand-written ARIA widgets follow the WAI-ARIA Authoring Practices pattern exactly and are tested by
      keyboard — `RecipeDetailTabs` is the reference.
- [ ] `nav` elements have an `aria-label` when more than one exists on a page (`"Primary navigation"`,
      `"Main recipe management actions"`).

Additional requirements for new work:

- [ ] Visible focus indicator on every interactive element, in **both** themes.
- [ ] Text contrast ≥ WCAG AA (4.5:1 body, 3:1 large text) against its actual surface — check `--ink-2` and
      `--ink-3`, which are the likeliest failures.
- [ ] Keyboard-only path through the whole screen, including any reorder or delete affordance.
- [ ] Nothing conveyed by colour alone.

## Inputs it needs

- The spec from `00-leader` and the contract delta from `08-api-contract` (which fields actually exist).
- [../domain-model.md](../domain-model.md) — field meanings, units, and validation limits to surface in the UI.
- [../conventions.md](../conventions.md#frontend-react--typescript).
- The current token files under `recipe-manager-frontend/src/styles/themes/`.

## Expected outputs

1. A screen specification: layout, all four states, copy, responsive behaviour, a11y requirements.
2. Token decisions — which existing tokens to use, or a justified new token added to both theme files.
3. A review of the implemented screen in light **and** dark, with `file:line` findings.
4. Entries in [../known-issues.md](../known-issues.md) for anything needing a product decision — never a
   inline TODO marker left in a stylesheet or a doc.
5. **An explanation of the design and accessibility reasoning** ([../learning-mode.md](../learning-mode.md)):
   - **Name who an accessibility rule protects**, concretely. Not "add `aria-current` for a11y", but "a screen
     reader announces every nav link identically without it, so a blind user cannot tell which page they are
     on". Abstract compliance is forgettable; a person is not.
   - **Explain why the semantic element beats the ARIA patch.** `<button>` gets keyboard activation, focus
     order, and the correct role for free; `<div onClick role="button" tabIndex={0}>` reimplements all three by
     hand and usually gets one wrong.
   - **Explain the token, not just the value.** Why a decision belongs in `variables.css` versus a component's
     module, and why a colour must be added to both theme files or it silently resolves to nothing.
   - **Explain the states.** Why "no recipes exist" and "no recipes match your search" need different copy, and
     what the user concludes wrongly if they share one message.
   - **Explain contrast as a measurement**, with the ratio and the threshold — not as an opinion about whether
     something "looks readable".

## Handoff

- → `03-senior-react` with the screen spec and token list.
- → `01-architect` when a UI dependency (form library, drag-and-drop, component or icon library) is required.
- → `04-code-reviewer` with the a11y findings from the implemented screen.
- → `00-leader` when the design reveals a missing product decision (e.g. what "edit recipe" means without users).
