# Spec: Editorial design system and shell (`R-16`)

| | |
| --- | --- |
| **ID** | `009` |
| **Status** | draft — awaiting user approval |
| **Author** | `00-leader` + `07-ux-ui` + `01-architect`, with the user |
| **Created** | 2026-09-19 |
| **Branch** | `feat/editorial-tokens` → `feat/editorial-shell` → `feat/editorial-screens` (three PRs, §3) |

---

## 1. Context

The canonical visual reference for the whole app is the **editorial design**
([agents/07-ux-ui.md](../agents/07-ux-ui.md#canonical-design-reference)), and the SPA looks nothing like it: a
generic blue-on-white palette, `h1 { font-size: 4.2em }` bypassing the type scale (`UX-02`), ad-hoc media queries
per module (`UX-03`), dark-theme status colours that were never contrast-checked (`UX-01`), and a theme toggle in
the footer with no `prefers-color-scheme` support (`DEC-06`). `R-16` is the only part of the design with no
data-model dependency, so it goes first and every later screen is built on its tokens rather than against them.

The design's own values were extracted from an export of `Recipe Manager - Editorial App.dc.html` supplied by the
user on 2026-09-19, not from the rendered frames, so the hex values and type sizes in §8 are the file's own.

## 2. Goal

Replace the palette, typography, and shell with the editorial design's, on the data the API can already answer,
and move the theme control to Settings with a working **System** option.

## 3. In scope

Three PRs, each independently reviewable and shippable.

### PR 1 — `feat/editorial-tokens` (ADR-021)

- [ ] `styles/themes/variables.css`: font-family tokens, the six-role type scale (§8.1), documented breakpoints,
      spacing/radius/transition tokens kept as they are.
- [ ] `styles/themes/light.css` + `dark.css`: the paper/ink palette (§8.2), **identical key sets**.
- [ ] `styles/typography.module.css`: six composable classes (`display`, `title`, `recipeTitle`, `body`, `ui`,
      `label`) consumed through CSS Modules' `composes:`.
- [ ] `styles/globals.css`: delete the three absolute heading rules (`UX-02`); headings inherit from the scale.
- [ ] Newsreader self-hosted via `@fontsource-variable/newsreader`, imported once in `main.tsx`.
- [ ] `ThemePreference` / `Theme` split, `matchMedia` subscription, `localStorage` migration (§8.4).
- [ ] Every existing `*.module.css` migrated from `--color-*` to the new token names.
- [ ] `src/test/setup.ts`: a `window.matchMedia` stub (jsdom does not implement it).
- [ ] Vitest coverage for the resolved-theme logic (§12).

### PR 2 — `feat/editorial-shell`

- [ ] `Header`: blender mark + "Recipe Manager" in Newsreader, pill nav links, an inked "New recipe" pill.
- [ ] `Footer`: hairline rule, `© <year> Recipe Manager` left, a "Settings" link right. The theme toggle is
      **deleted here**, so PR 2 and PR 3 must land together or PR 2 keeps it until PR 3 (§14).
- [ ] `BottomNav` (new, `components/layout/`): shown below 768px, four destinations, 44px minimum touch target.
- [ ] `Icon`: add `add`, `arrow_forward`, `home`, `menu_book`, `add_circle`, `person` as inline SVG.
- [ ] `NotFoundPage` + a `*` route, so the existing "Add recipe" link stops rendering a blank page (`BUG-06`).

### PR 3 — `feat/editorial-screens`

- [ ] `HomePage`: masthead line with the live recipe count, Newsreader display heading, subtitle, two pill
      buttons. No Last-cooked rail, no Jump-to chips (§4).
- [ ] `RecipePage` + `RecipeList` + `RecipeCard`: editorial list rows — hatch placeholder, Newsreader title,
      description, tabular total time — with hairline separators instead of bordered cards.
- [ ] `SearchBar`: pill field, inline search icon, `--field-border` boundary.
- [ ] `ProfilePage` (replacing the `<div>Profile</div>` stub in `App.tsx`): header block, a Preferences section
      containing **only** the Light/Dark/System segmented control, and the disabled Account block from the design.
- [ ] `src/assets/mainPhoto.png` deleted, closing `BUILD-05`.

## 4. Out of scope

- **Home's "Last cooked" rail and the cooks/stats numbers** — need `R-22`. Left out rather than stubbed.
- **Home's three "Jump to" saved views** — `under 30 minutes` and `feeds a table` need `R-11`'s query contract,
  `never cooked yet` needs `R-22`. Building them client-side now means building them twice.
- **Tag chips on list rows** — need `R-20`.
- **Units (metric/imperial) preference** — needs `R-10`'s conversion policy. Not rendered, not even disabled.
- **Default serving size and Keep-screen-awake preferences** — nothing reads them until `R-21` and `R-23`. A
  control that silently changes nothing is worse than an absent one; they arrive with their consumers.
- **Export / import / print rows in Settings** — `R-24`.
- **Recipe detail and Add/Edit screens** — `R-18`, `R-21`. The list rows link nowhere until `R-18`.
- **Cooking-mode palette** — recorded in §8.2 for completeness, implemented in `R-23`.
- **A frontend formatter** (`QUAL-05`) — a separate dependency decision.

## 5. Decisions taken with the user (2026-09-19)

| # | Question | Decision |
| --- | --- | --- |
| 1 | How is R-16 split? | Three PRs, as in §3. |
| 2 | Which Settings preferences ship now? | Theme only. |
| 3 | How is Newsreader self-hosted? | `@fontsource-variable/newsreader`. |
| 4 | What does Home show? | Hero plus the live recipe count; no rail, no chips. |
| 5 | What happens to `mainPhoto.png`? | Deleted; list rows use the design's CSS hatch. Closes `BUILD-05`. |

## 6. Domain impact

None. No entity, invariant, validator, migration, or known limitation is touched.

## 7. API impact

None. No endpoint, no `RecipeDto` change, no cache key change. The only data consumed is the existing
`GET /api/recipes` through `useRecipes`, for Home's recipe count and the Recipes list.

## 8. Frontend impact

### 8.1 Type scale

Newsreader 300 and 500 only — no italic, no other weight appears anywhere in the design file. Sizes converted
from the design's `px` to `rem` at the 16px root, so a user who raises their browser's default font size gets
larger text; `px` would ignore that setting.

| Token | Role | Design value | Spec value |
| --- | --- | --- | --- |
| `--type-display` | Home h1 | `300 62px/1.02` Newsreader | `300 3.875rem/1.02`, `letter-spacing: -.02em` |
| `--type-title` | page h1 | `300 38px/1.1` Newsreader | `300 2.375rem/1.1` |
| `--type-recipe-title` | list row, card | `500 20px/1.25` Newsreader | `500 1.25rem/1.25` |
| `--type-body` | descriptions, prose | `400 15.5px/1.5` system-ui | `400 0.969rem/1.5` |
| `--type-ui` | nav, buttons, controls | `500–600 14px/1` system-ui | `500 0.875rem/1` |
| `--type-label` | small-caps labels | `11px/1` monospace, `.12em`, uppercase | `0.688rem/1`, `.12em`, uppercase |

Families: `--font-serif: 'Newsreader Variable', Georgia, serif`, `--font-ui: system-ui, sans-serif`,
`--font-mono: ui-monospace, Menlo, monospace`. Quantities and durations set `font-variant-numeric: tabular-nums`
so digits do not jitter between rows.

Mobile: `--type-display` and `--type-title` drop to the design's mobile sizes (`2rem/1.05` for the title) below
768px.

### 8.2 Palette

Light and dark are the design's own values. Every ratio below was measured against the surface the token is
actually used on; **AA body** is ≥ 4.5:1, **AA large/UI** is ≥ 3:1.

| Token | Light | on `--paper` | on `--paper-2` | Dark | on `--paper` | on `--paper-2` |
| --- | --- | --- | --- | --- | --- | --- |
| `--paper` | `#faf8f4` | — | — | `#16151a` | — | — |
| `--paper-2` | `#f2eee7` | — | — | `#1e1d23` | — | — |
| `--ink` | `#1c1b19` | 16.23 | 14.88 | `#f4f2ee` | 16.24 | 14.96 |
| `--ink-2` | `#57534c` | 7.21 | 6.61 | `#a8a29a` | 7.18 | 6.61 |
| `--ink-3` | `#6b655c` | 5.44 | 4.99 | `#938c83` | 5.47 | 5.03 |
| `--rule` | `#e2dcd2` | 1.29 | — | `#302e36` | 1.36 | — |
| `--accent` | `#1d4ed8` † | 6.32 | 5.80 | `#60a5fa` | 7.14 | 6.58 |
| `--accent-text` | `#ffffff` | 6.70 on accent | — | `#0b1220` | 7.5 on accent | — |
| `--danger` | `#b42318` | 6.20 | — | `#f87171` ‡ | 6.57 | 6.05 |
| `--field-border` | `#8a8275` ‡ | 3.58 | 3.28 | `#726e7d` ‡ | 3.67 | 3.38 |

Three values deviate from the design file, each for a measured reason:

- **† `--accent` in light is `#1d4ed8`, not the design's `#2563eb`.** The design's blue measures **4.47:1 on
  `--paper-2`** — under the 4.5 body threshold, and `--paper-2` is exactly where the design puts small accent
  links. `#1d4ed8` is the same hue one step darker and passes on both surfaces.
- **‡ `--danger` in dark is new.** The design defines `--danger` only in its light Add-recipe frame. That value on
  dark paper is **2.76:1 — a fail**. This is `UX-01`'s mistake (a status colour shared across themes and only ever
  checked against light) caught before it ships.
- **‡ `--field-border` is new.** WCAG 1.4.11 requires 3:1 for the visual boundary that identifies a control. The
  design's search field is bounded only by `--rule` at **1.29:1**. Hairlines stay `--rule` because they are
  decorative; anything a user must find and click gets `--field-border`.

`--accent-text` exists because the design hard-codes `#fff` on accent in light and `#0b1220` in dark. White on the
dark accent is **2.54:1**; a token makes that pairing unrepresentable rather than relying on memory.

Cooking mode (`R-23`, recorded only): `--paper #12110f`, `--paper-2 #1c1a17`, `--ink #f7f4ee`, `--ink-2 #a9a29a`,
`--ink-3 #8f887e`, `--rule #2b2822`, `--accent #f6c453` (11.63:1 on its paper).

### 8.3 Breakpoints (`UX-03`)

`480px`, `768px`, `1024px`, declared once as a comment block in `variables.css` and used literally.

**A custom property cannot be used in a media query** — `@media (max-width: var(--bp-md))` is invalid CSS, because
custom properties are not resolved at that point in the cascade. The alternatives are PostCSS `@custom-media` (a
build dependency and a syntax extension for a cosmetic gain) or container queries (a different feature solving a
different problem). Documented constants are the honest answer, and `UX-03` is closed on that basis with the
limitation recorded.

### 8.4 Theme model

```ts
export type ThemePreference = 'light' | 'dark' | 'system'; // what the user chose → localStorage 'theme'
export type Theme = 'light' | 'dark';                      // what is rendered    → <html data-theme>
```

`ThemeProvider` stores the **preference** and derives the **theme**:
`preference === 'system' ? (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light') : preference`.
It subscribes to that media query so the app follows the OS live, not only at load. The resolved theme is still
written to `<html data-theme>` inside the `useState` initialiser, preserving the existing no-flash first paint.

- The context exposes `{ preference, theme, setPreference }`. `toggleTheme` survives PR 1 only to keep the footer
  switch working, and is deleted in PR 3 with the switch itself.
- Unknown `localStorage` values fall back to the default, as the current code already does for `'dark'`.
- **Default for a first-time visitor becomes `system`** — the point of `DEC-06`. An existing stored `'light'` or
  `'dark'` is an explicit choice and is respected, not migrated.
- The control is a segmented three-option group. It is a radio group, not three buttons: arrow keys move between
  options natively, and the chosen one is announced as selected. `role="switch"` no longer fits — a switch is
  binary, and this has three states.

### 8.5 Routes, components, hooks

- **New routes:** `/profile` gains a real page (it renders `<div>Profile</div>` today); `*` renders `NotFoundPage`.
  `/recipes/new` and `/recipes/:id` stay absent — `R-21` and `R-18`.
- **New components:** `layout/BottomNav`, `pages/Profile/ProfilePage`, `pages/NotFound/NotFoundPage`,
  `ui/ThemeControl` (the segmented control).
- **Changed:** `Header`, `Footer`, `AppLayout`, `SearchBar`, `RecipeCard`, `RecipeList`, `HomePage`, `Icon`.
- **New hooks:** none. Home's count comes from the existing `useRecipes`.
- **States.** Home: loading → the masthead count is omitted while the query is pending, the rest of the hero
  renders immediately (it needs no data); error → the hero renders without the count, since a failed count must
  not hide a working page. Recipes: unchanged four-state behaviour, restyled, keeping `RecipeList`'s distinction
  between *no recipes exist* and *no search matches*. Settings: no async state.

## 9. Architecture impact

- **ADR required:** yes → **ADR-021 — Self-hosted Newsreader and the editorial token set** in
  [../architecture.md](../architecture.md).
- **New dependency:** `@fontsource-variable/newsreader` (MIT packaging; the font is OFL). One import in
  `main.tsx`; Vite bundles and content-hashes the `woff2`, so it is served from our own origin.
- **Layer/dependency changes:** none. **New DI registrations:** none.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Google Fonts `<link>`, as the design does | Zero setup, shared CDN cache | Sends every visitor's IP to a third party, forces the future CSP (`SEC-10`) to allow an external origin, and breaks an app meant to be self-hosted when offline. A prototype's implementation choices are not decisions (ADR-014's reasoning). |
| Hand-committed `woff2` + `@font-face` | No dependency at all | We would own subsetting and updates by hand for no gain; the package is a build-time asset that disappears into the bundle. |
| `@fontsource/newsreader` static weights | Slightly smaller for exactly two weights | Two imports instead of one, and a third weight later means another import rather than nothing. |
| Keep `--color-*` names, swap only values | Smallest diff; no module churn | The names would lie — `--color-surface` describing `--paper-2` teaches the wrong model to every later screen, and `R-17`–`R-24` would all be written against stale vocabulary. |
| Global utility classes for typography | Terse markup | A second, unscoped styling system beside CSS Modules; ADR-014 already rejected a competing style engine. |
| Repeat type declarations per module | No new file | Four declarations per label across ~15 modules is how a token system rots into copy-paste. |

- **Pattern applied:** design tokens with a derived (not stored) resolved value; CSS Modules `composes:` for
  shared typography. Existing examples in this repo: `styles/themes/*.css` with `data-theme`, and
  `RecipeList.filteredRecipes` for derived state.
- **What this makes harder:** every `*.module.css` changes at once, so `git blame` on styling is muddied for a
  release, and any branch open across PR 1 conflicts in CSS. Renaming the tokens also means every snippet in the
  docs referring to `--color-*` is stale until updated in the same PR.

## 10. Security impact

- **New user-controlled input:** none. **User content rendered:** unchanged — React escapes it as before.
- **File upload:** no. **Auth/ownership:** unchanged; the Account block is inert and says so in its own copy.
- **Config/secrets:** none.
- **Standing gaps affected:** **improves `SEC-10`** — self-hosting the font keeps the future CSP `self`-only,
  where the design's Google Fonts link would have required `fonts.googleapis.com` and `fonts.gstatic.com`.

## 11. Acceptance criteria

- [ ] Given a browser with no stored preference and an OS set to dark, when the app loads, then `<html>` carries
      `data-theme="dark"` on first paint and Settings shows **System** selected.
- [ ] Given the preference is `system`, when the OS colour scheme changes while the app is open, then the rendered
      theme follows without a reload.
- [ ] Given a stored preference of `light`, when the OS is dark, then the app renders light.
- [ ] Given `localStorage.theme` holds an unrecognised value, when the app loads, then it falls back to the
      default rather than throwing.
- [ ] Given Settings, when a preference is chosen with the keyboard alone, then arrow keys move between the three
      options and the choice persists across a reload.
- [ ] Every token in `light.css` has a counterpart in `dark.css` — asserted by a test, not by review.
- [ ] Every interactive element shows a visible focus ring in both themes.
- [ ] No `--color-*` reference remains under `src/`.
- [ ] Below 768px the bottom navigation is visible, every target is ≥ 44px, and the header nav is hidden.
- [ ] `npm run build`, `npm run lint` (0 problems), and `npm test` all pass; the production bundle no longer
      contains a 2.1 MB PNG.

## 12. Test plan

- **Domain / handler / integration tests:** none — no backend code changes.
- **Vitest (extending the 40 existing tests):**
  - `ThemeProvider`: default with no stored value; `system` resolving each way; live `matchMedia` change;
    explicit preference overriding the OS; an unrecognised stored value; persistence to `localStorage`.
  - `ThemeControl`: renders three options, marks the current one selected, calls `setPreference`.
  - Token parity: parse `light.css` and `dark.css`, assert identical custom-property key sets. This is the
    automated version of a checklist item in [agents/07-ux-ui.md](../agents/07-ux-ui.md) that has so far been
    enforced by eye.
  - `BottomNav`: `aria-current` on the active destination.
- **Not covered, and why:** contrast ratios are computed once here (§8.2) rather than asserted at runtime — a
  test would need a colour-science dependency to re-derive constants that only change when a token changes.
  Visual regression is not covered at all; there is no snapshot tooling and adding it is its own decision.
- **Manual verification:** both themes at 1280px and 390px, with the OS preference flipped at the system level
  while the app is open; keyboard-only pass through Header → Home → Recipes → Settings.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-021 (font hosting + token set) |
| 3 | `07-ux-ui` | the token, type, and contrast decisions in §8; review of both themes |
| 4 | `03-senior-react` | components, theme model, routes, CSS Modules |
| 5 | `06-qa-tester` | the Vitest additions in §12 |
| 6 | `04-code-reviewer` | review of each PR |

`02-senior-csharp`, `08-api-contract`, and `05-security-reviewer` have nothing to do here: no backend code, no
contract change, and no new input or rendered user content (§10).

## 14. Assumptions made

- The default for a visitor with nothing stored becomes `system`, and existing stored values are honoured as
  explicit choices.
- PR 2 keeps the footer theme switch working until PR 3 moves the control into Settings, so no PR in the sequence
  leaves the app without a way to change theme.
- The design's blender glyph stays the brand mark; `Logo` is restyled, not replaced.
- Home's masthead reads `<n> recipes · yours alone`, dropping the design's `78 cooks` until `R-22`.
- Keeping `RecipeCard`'s existing `formatDuration` / `getISODuration` and `<time dateTime>` markup — the design
  shows a bare total, but dropping the machine-readable duration would regress the accessibility bar.

## 15. Follow-ups

- `UX-06` (new): the design's hairline `--rule` is 1.29:1 against paper. Decorative separators are exempt, but any
  future control bounded only by a hairline must use `--field-border` instead. Recorded so the next screen does
  not reintroduce it.
- `UX-07` (new): no visual-regression tooling, so a token change that breaks one screen is caught only by eye.
- `QUAL-05` (existing): this spec touches every CSS module and cannot fix the 2-vs-4-space split without a
  formatter, which is a separate dependency decision.
- `BUG-10`: list rows still link nowhere until `R-18`. They render as non-interactive rows in PR 3 rather than as
  buttons that do nothing.

## 16. Known issues and roadmap items touched

- **Fixes:** `UX-01` (dark status colours measured, `--danger` given a dark value), `UX-02` (heading sizes on the
  scale), `UX-03` (documented breakpoints), `DEC-06` (System option), `BUILD-05` (2.1 MB PNG deleted),
  `BUG-06` (a `*` route, so the link no longer renders blank).
- **Opens:** `UX-06`, `UX-07`.
- **Implements:** `R-16`, which is deleted from [../roadmap.md](../roadmap.md) when PR 3 merges.
- **Depends on:** nothing. `R-16` is the only Phase 3 item with no data-model dependency.
- **On the [deploy gate](../roadmap.md#deploy-gate)?** No — but it improves `SEC-10` (§10).
