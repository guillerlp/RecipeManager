# Spec: Cooking mode

| | |
| --- | --- |
| **ID** | `017` |
| **Status** | draft |
| **Author** | `00-leader` + `07-ux-ui`, with the user (2026-10-23) |
| **Created** | `2026-10-23` |
| **Branch** | `feat/cooking-mode` (PR 1), `feat/cooking-mode-timer` (PR 2) |

---

## 1. Context

The owner cooks with a phone propped on the counter and messy hands, and the screen keeps turning off mid-recipe.
The detail screen (`R-18`) is built for reading, not cooking: every step and every ingredient at once, at body
size, with nothing keeping the screen awake. The editorial design's frame **3f** is the answer — one step at a
time, read at arm's length, only that step's ingredients — and every piece of data it needs already exists:
structured steps with `durationMinutes` and `ingredientIds` (`R-17`, ADR-023) and a cook log to record the
finish (`R-22`, ADR-028). This is `R-23` in [../roadmap.md](../roadmap.md).

## 2. Goal

A recipe can be cooked one step at a time on a phone across the kitchen — screen kept awake, quantities as
scaled on Detail, a step timer that gets attention when it ends — and finishing can log the cook.

## 3. In scope

**PR 1 — the screen** (`feat/cooking-mode`)

- [ ] Route `/recipes/:id/cook`, full-screen, outside `AppLayout` but inside the providers (§8.1).
- [ ] Cooking palette as a scoped token override (§8.3), contrast measured on both cooking surfaces.
- [ ] One step at a time: "Step N of M", step text in large serif, Back / Next step, `?step=` in the URL (§8.4).
- [ ] Ingredient lists per §8.5: "For this step" / "Already used" / "Also needed", or the full "Ingredients" list
      when no step references any ingredient.
- [ ] Quantities scaled by `?servings=` and converted by the Units preference — the same scale → convert →
      format pipeline as `IngredientRail`, **extracted and shared, not copied** (§8.6).
- [ ] Finish on the last step opens an inline "Log this cook?" confirm; Log cook calls `useLogCook` (§8.7).
- [ ] Close (×) leaves without logging and without asking.
- [ ] Left / Right arrow keys move between steps.
- [ ] "Start cooking" link on Detail, carrying the current servings (§8.2).
- [ ] ADR-029 in [../architecture.md](../architecture.md); `R-23` removed from the roadmap when PR 2 lands.

**PR 2 — timer and wake lock** (`feat/cooking-mode-timer`)

- [ ] Step timer for steps with `durationMinutes`: started by a tap, survives Next/Back, one at a time (§8.8).
- [ ] Alarm: generated tone (Web Audio) + vibration (where supported) + a pulsing on-screen banner, until
      dismissed.
- [ ] Screen Wake Lock while cooking mode is open, re-acquired when the tab becomes visible again (§8.9).
- [ ] "Keep screen awake while cooking" preference in Settings, **on by default**, a `role="switch"` toggle.
- [ ] Status chip that tells the truth: "Screen stays on" only while the lock is actually held.
- [ ] The "Next step runs …" hint from the design, when the next step has a duration.
- [ ] Spec 009's "Keep-screen-awake preference … arrives with its consumer" note resolved; this spec set to
      shipped; test counts updated in `CLAUDE.md`.

## 4. Out of scope

- **Backend changes.** None. `POST /api/recipes/{id}/cooks` with `{}` already exists (ADR-028).
- **More than one timer at a time.** The design shows one chip; parallel timers are a different design.
- **Undo inside cooking mode.** Logging returns to Detail; a cook logged here cannot be removed afterwards from
  the UI — that is the existing `UX-20`, not widened here.
- **Timer alarms while the tab is hidden or the phone is locked.** A web page cannot reliably run code then;
  doing it properly needs a service worker and Notifications permission. With the wake lock held the screen
  does not lock, which covers the stated use. Filed as a follow-up (§15).
- **Persisting timer or progress across sessions** beyond what the URL carries (`?step=`, `?servings=`). A running
  timer is lost on reload.
- **Voice control, swipe gestures, step-by-step read-aloud.** Not asked for.
- **A "Default serving size" preference.** Spec 009 parked it with this one; nothing in cooking mode needs it,
  since servings come from Detail.
- **`01-architect` ADR beyond ADR-029, `08-api-contract`, `05-security-reviewer`.** No contract change, no new
  input stored, no new dependency (§9, §10).

## 6. Domain impact

- **New/changed entities:** none.
- **New/changed properties on `Recipe`:** none.
- **New/changed invariants:** none.
- **New/changed shape validation:** none.
- **Migration required:** no.
- **Known limitations touched:** none. Step references (`ingredientIds`) are read, not changed; they are optional,
  which is why §8.5 has a fallback.

## 7. API impact

- **New/changed endpoints:** none. Uses `GET /api/recipes/{id}` (via `useRecipe`) and
  `POST /api/recipes/{id}/cooks` with an empty body (via `useLogCook`).
- **`RecipeDto` / `UpdateRecipeDto` changes:** none.
- **Breaking for the client?** no.
- **Cache impact:** none server-side. Client-side, `useLogCook` already invalidates `['recipes']`, which refreshes
  Detail's cook stat, Home's rail, and Profile's stats.

## 8. Frontend impact

### 8.1 Route and layout

- `/recipes/:id/cook` → `CookingModePage` in `pages/CookingMode/`.
- It needs `ThemeProvider` and `UnitsProvider` but **not** `AppLayout` (no header, footer, or bottom nav). Today
  `Shell` wraps all three, so the route table becomes a providers route containing (a) a layout route with
  `AppLayout` for every existing page and (b) the cooking route on its own. How `Shell` is split is the
  builder's call; every existing route must keep its current chrome, and `useBlocker` must keep working
  (the data router stays — ADR-027).
- Invalid id (`isRecipeId` false) or 404 → a small not-found state of the page's own, in the cooking palette:
  "We couldn't find that recipe." with a link to `/recipes`. Detail's `RecipeNotFound` is private to
  `RecipeDetailPage.tsx` and leans on the app chrome around it, so it is not reused (decided after the builder
  check). Loading → a minimal centered
  "Loading recipe…" in the cooking palette. Fetch error → message plus a `refetch()` retry, same copy pattern
  as Detail.
- A recipe with **zero steps** cannot be cooked: if reached by URL, show "This recipe has no steps yet." with a
  link back to its Detail page. (Detail hides the entry point in that case, §8.2.)

### 8.2 Entry point on Detail

- A "Start cooking" **link** (navigates — ADR-024, so `<Link>`, not a button) on `RecipeDetailPage`, shown only
  when `recipe.instructions.length > 0`. Drafts included — spec 016 decided drafts can be cooked.
- Target: `/recipes/{id}/cook?servings={servings}`, using the page's current servings state; omit the parameter
  when servings is `null` (a draft with no servings).
- Placement: desktop beside Edit in the top bar; mobile a full-width sticky bottom bar, as spec 012 anticipated
  ("No sticky bottom bar (it would hold Start cooking, `R-23`)"). Exact placement and styling is `07-ux-ui`'s
  call within those constraints; the design's label is "Start cooking" with a `skillet` icon.

### 8.3 Cooking palette — scoped token override

The palette is **not a theme**: it applies in both light and dark. It is implemented by re-declaring the existing
colour tokens under `[data-palette="cooking"]` in a new `styles/themes/cooking.css`, imported in `main.tsx` beside
`light.css` / `dark.css`, and set on the cooking page's root element. Every element inside picks it up through
the tokens it already uses. **Same token names, no new names.** `styles/themes/tokenParity.test.ts` is extended
so `cooking.css` must declare every colour token the themes declare — which turns ADR-029's named cost (a future
theme token silently falling back inside cooking mode) into a failing test.

Values from spec 009 §8.2; the design never gave cooking values for the last three tokens, so those are
chosen here; every ratio measured (WCAG relative luminance)
against both cooking surfaces and recorded in a comment beside the value:

| Token | Value | on `--paper #12110f` | on `--paper-2 #1c1a17` | Note |
| --- | --- | --- | --- | --- |
| `--paper` | `#12110f` | — | — | |
| `--paper-2` | `#1c1a17` | — | — | |
| `--ink` | `#f7f4ee` | 17.19 | 15.82 | |
| `--ink-2` | `#a9a29a` | 7.48 | 6.88 | |
| `--ink-3` | `#8f887e` | 5.38 | 4.95 | passes 4.5 on both |
| `--rule` | `#2b2822` | 1.28 | 1.18 | decorative only (`UX-06`) |
| `--accent` | `#f6c453` | 11.63 | 10.70 | |
| `--accent-text` | `#12110f` | 11.63 on `--accent` | | value chosen here — dark text on amber |
| `--field-border` | `#746d63` | 3.69 | 3.40 | value chosen here — WCAG 1.4.11 control boundary |
| `--danger` | `#f28b82` | 7.90 | 7.27 | value chosen here — the log-error message |

The cooking root fills the viewport (`min-height: 100dvh`) and paints its own background, so the theme's
`body` background never shows around it.

### 8.4 Step navigation

- Step index lives in the URL as `?step=N` (1-based), updated with `replace` so Back in the browser leaves cooking
  mode rather than stepping through every step. Missing, non-integer, or out-of-range → clamp to 1..M.
- Heading: "Step N of M" (label role), then the step text in large serif — the design's 34px on desktop; on
  mobile as large as fits without the controls leaving the viewport (`07-ux-ui`'s call, never below the
  `--type-recipe-title` size).
- **Back** / **Next step**: `<button type="button">`. On step 1, Back is `aria-disabled="true"` (not `disabled`,
  so focus is never dropped — the `R-21` reorder rule). On the last step, Next step is replaced by **Finish**.
- Mobile: Back and Next/Finish are pinned to the bottom, full-width, at least 56px tall — reachable with a
  knuckle. Desktop: as in the design.
- Moving steps moves focus to the step heading and announces it (the heading is the focus target with
  `tabIndex={-1}`), so a screen-reader user hears the new step.
- Keyboard: `ArrowLeft` / `ArrowRight` on the page move steps, ignored while focus is in a form control or the
  confirm panel is open.

### 8.5 Ingredient lists

Decided per recipe:

- **No step in the recipe has any `ingredientIds`** → beside every step, the full list under the heading
  **"Ingredients"**, nothing struck through.
- **At least one step has `ingredientIds`** → three lists, each hidden when empty:
  - **"For this step"** — the current step's referenced ingredients, in ingredient order.
  - **"Already used"** — ingredients referenced by any earlier step and not by the current step, struck
    through, each carrying visually hidden text ", used" so the meaning is not conveyed by styling alone.
  - **"Also needed"** — ingredients no step references at all, on every step.
- An ingredient referenced by several steps appears under "For this step" on each of them, and under "Already
  used" only on later steps that do not reference it.
- An `ingredientId` that matches no ingredient (should not happen — ADR-023 validates it) is ignored.
- Layout per the design: desktop a left rail beside the step; mobile the lists below the step text.

### 8.6 Quantities

- `servings` from `?servings=`; integer 1–999, otherwise the recipe's written servings. If the recipe has no
  written servings, quantities are shown as written.
- The scale → convert → format pipeline in `IngredientRail` is moved into a shared function (and the amount's
  visible/spoken markup into a shared component, if the builder judges it worth it) used by both `IngredientRail`
  and cooking mode. **No second copy of the pipeline.** `IngredientRail`'s behaviour and tests stay unchanged.
- Units from `useUnits()`; no control for it in cooking mode.
- No servings stepper in cooking mode — scaling is decided on Detail.

### 8.7 Finish and log

- **Finish** replaces Next step on the last step. It opens an inline panel (not a modal) in place of the
  controls: heading "Log this cook?", buttons **Log cook** (primary, accent) and **Not now**.
- Focus moves to the panel heading when it opens; Escape closes it back to the step.
- **Log cook** → `useLogCook().mutate({ recipeId })` (no `cookedAt`; the server stamps it). Button disabled while
  pending. On success → navigate to `/recipes/{id}` (the Detail page's cook stat shows the new count after the
  invalidation). On error → the panel stays, with `role="alert"` "Couldn't log this cook. Try again." (Detail's
  copy) and Log cook enabled again.
- **Not now** → navigate to `/recipes/{id}` without logging.
- **Close (×)** in the top bar: a `<Link>` to `/recipes/{id}`, accessible name "Leave cooking mode", always
  available, never confirms, never logs.
- **Every exit (Close, Not now, Log cook success) navigates with `replace`**, so the browser's Back from Detail
  does not reopen cooking mode — the same reasoning as `?step=` in §8.4. History is left as
  Detail → Detail, a harmless duplicate entry; `navigate(-1)` was rejected because cooking mode can be opened
  directly by URL with nothing behind it.
- **Detail's servings reset on return.** They are local state (spec 012) and cooking mode does not hand them
  back. Accepted: the scaling mattered while cooking; making Detail read `?servings=` would change `R-18`'s
  "servings describe this visit, never persisted" decision for no stated need.

### 8.8 Timer (PR 2)

- Shown only on a step with `durationMinutes > 0`: a **Start timer** button labelled with the duration
  ("Start 1h 25min timer", `formatDuration`).
- One timer at a time. Running state: a chip in the top bar showing the remaining time `mm:ss` (or `h:mm:ss`),
  tabular numerals, `<time>`-free (it is a countdown, not a duration of the recipe). The chip persists across
  Next/Back. Starting a timer on another step while one runs replaces it (no confirm — the old one is shown
  in the chip, so the replacement is visible; the builder may revisit this if it proves annoying).
- Pause/resume and cancel from the chip.
- **Time is derived from a stored end timestamp** (`Date.now()`-based), recomputed each tick — never by counting
  interval ticks, which browsers throttle.
- Screen readers: the chip is **not** a live region (a per-second announcement is noise); the alarm is.
- **Alarm** when it reaches zero, until **Dismiss** is pressed:
  - A repeating short tone generated with the Web Audio API — no audio asset. The `AudioContext` is created or
    resumed inside the Start timer tap, which is what browsers require before audio may play later.
  - `navigator.vibrate(pattern)` repeated, guarded by feature detection. **iOS Safari has no Vibration API**;
    there the tone and banner carry the alarm. Not an error, not mentioned in the UI.
  - A banner "Time's up — Step N" (the step the timer was started on; steps have no title field) with `role="alert"`, pulsing amber, with a large Dismiss button. The
    pulse respects `prefers-reduced-motion` (static high-contrast banner instead).
- The timer stops (no alarm) when cooking mode is left.

### 8.9 Wake lock and its preference (PR 2)

- Preference: `keepAwake: boolean`, default `true`, persisted in `localStorage` under `keepAwake`, read and
  written through a small hook (§9 for why not a context). Unreadable or invalid storage → `true`.
- Settings (`ProfilePage`, Preferences section): "Keep screen awake while cooking", a `<button role="switch"
  aria-checked>` — the first genuinely binary control since the theme switch was replaced (07-ux-ui's rule:
  `role="switch"` for a genuinely binary toggle only).
- In cooking mode, when the preference is on and `'wakeLock' in navigator`: request `navigator.wakeLock.request
  ('screen')` on mount; release on unmount; on `visibilitychange` to visible, request again (the browser releases
  the lock whenever the page is hidden); listen for the sentinel's `release` event.
- Status chip in the top bar, from the lock's real state:
  - held → "Screen stays on" (`visibility` icon, as in the design);
  - unsupported, refused, or released and not yet re-acquired → "Screen may turn off";
  - preference off → no chip.
- A refused request is not an error state: no alert, just the chip.

### 8.10 States

| State | What shows |
| --- | --- |
| Loading | Cooking palette, centered "Loading recipe…" |
| Error | Message + Retry (`refetch()`) + link back to the recipe |
| Not found / invalid id | Detail's not-found handling |
| No steps | "This recipe has no steps yet." + link to Detail |
| Populated | §8.4–§8.9 |

### 8.11 New/changed hooks and components

- `pages/CookingMode/CookingModePage.tsx` (+ module CSS, index, barrel) and whatever subcomponents the builder
  wants under it — page-local, as `RecipeDetail/CookControls.tsx` is.
- PR 2: a wake-lock hook, a timer hook, a `keepAwake` preference hook in `hooks/`, exported from
  `hooks/index.ts`. Shapes are the builder's call.
- No new query or mutation hooks. `useRecipe` and `useLogCook` are reused.
- No new icons beyond what the screen shows: close, timer, visibility, arrow back/forward, skillet, lightbulb —
  added to `components/ui/Icon/` as plain SVG paths, decorative.

## 9. Architecture impact

- **ADR required:** yes → **ADR-029**, short: cooking mode is a route outside the app chrome but inside the
  providers; the cooking palette is a scoped token override, not a third theme; browser capability APIs (Wake
  Lock, Web Audio, Vibration) are used directly behind feature detection, no dependency.
- **New dependency:** none.
- **Layer/dependency changes:** none (frontend only). The route tree gains one nesting level (§8.1).
- **New DI registrations:** none.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Cooking palette as a third `data-theme` value | Reusing the theme mechanism | A theme is a user preference applied app-wide; this palette belongs to one route and must override whichever theme is chosen. A scoped override needs no change to `ThemeProvider` and cannot leak to other pages. |
| New token names (`--cook-paper`, …) | Explicitness | Every reused style (the ingredient amounts, buttons) would need cooking-specific variants. Re-declaring the existing names inside one class makes them follow automatically — the CSS custom-property cascade doing what it is for. |
| Cooking route inside `AppLayout`, chrome hidden with CSS | No route-tree change | Header, nav, and footer still mount, still take focus order and landmarks, and a full-screen page would fight the layout's padding. |
| Servings and step in router `state` | Clean URLs | Lost when a mobile browser discards and reloads a background tab — exactly the cooking scenario. |
| `keepAwake` in a context, like `UnitsProvider` | Consistency with Units | Units is read live by a mounted page while Settings changes it in the same session; Settings and cooking mode are never mounted together, so `localStorage` read on mount is enough. A provider, context, and guard hook for that is code with nothing to do. |
| `nosleep.js`-style hidden looping video | Older iOS (< 16.4) support | A dependency and a hack (autoplay video) for browsers out of support; Wake Lock is supported by current Chrome, Edge, Safari (16.4+), and Firefox (126+). |
| Bundled alarm sound file | A nicer sound | An asset to ship and a fetch to fail; a generated tone is a few lines with no asset. Revisit if the tone is too weak to hear. |

- **Pattern applied:** CSS custom-property scoping (theming by cascade) — existing example: `light.css` /
  `dark.css` keyed on `[data-theme]`. Progressive enhancement / feature detection for Wake Lock and Vibration.
- **What this makes harder:** the cooking palette's values live apart from `light.css` / `dark.css`, so a future
  token added to the themes must also get a cooking value, or it falls back to the active theme's value inside
  cooking mode. The extended `tokenParity.test.ts` (§8.3) makes forgetting it a test failure rather than a silent
  colour bug. ADR-029 records this.

## 10. Security impact

- **New user-controlled input:** `?step=` and `?servings=` from the URL — parsed as integers and clamped, never
  rendered raw, never sent to the server.
- **User content rendered in the SPA:** yes — step text, ingredient names and notes, recipe title, already
  rendered elsewhere. React escapes them; no `dangerouslySetInnerHTML`.
- **File upload:** no.
- **Auth/ownership implications:** none new; no auth exists (`R-14`). Any visitor can log a cook, as on Detail.
- **Config/secrets touched:** none.
- **Standing gaps affected:** none.

## 11. Acceptance criteria

PR 1:

- [ ] Given a recipe with steps, when Detail is opened, then a "Start cooking" link points to
      `/recipes/{id}/cook?servings={current servings}`.
- [ ] Given a recipe with no steps, then Detail shows no "Start cooking" link, and `/recipes/{id}/cook` shows
      "This recipe has no steps yet." with a link back.
- [ ] Given a draft with steps, then "Start cooking" is shown.
- [ ] Given cooking mode on step 1 of 3, then "Step 1 of 3", Back is `aria-disabled`, and Next step shows step 2
      and sets `?step=2`.
- [ ] Given `?step=99` on a 3-step recipe, then step 3 is shown; `?step=abc` → step 1.
- [ ] Given `?servings=6` on a recipe written for 4, then every quantity is ×1.5 and converted per the Units
      preference — matching what `IngredientRail` shows for 6.
- [ ] Given `?servings=0` / `abc` / `5000`, then quantities are as written for the recipe's servings.
- [ ] Given no step references any ingredient, then every step shows the full "Ingredients" list, nothing struck.
- [ ] Given steps 1 and 2 reference A and B, step 3 references C, and D is referenced by none, then on step 3:
      "For this step" = C, "Already used" = A, B (struck, with hidden ", used"), "Also needed" = D.
- [ ] Given an ingredient referenced by steps 1 and 3, then on step 2 it is under "Already used" and on step 3
      under "For this step".
- [ ] Given the last step, when Finish then Log cook, then `POST …/cooks` is sent with `{}` and the app navigates
      to Detail.
- [ ] Given the log request fails, then "Couldn't log this cook. Try again." appears and Log cook is enabled.
- [ ] When Not now, or Close (×), then nothing is posted and the app is on Detail.
- [ ] When ArrowRight / ArrowLeft is pressed (focus not in a control), then the step changes.
- [ ] The cooking root carries `data-palette="cooking"` in both light and dark themes, and `tokenParity.test.ts`
      fails if `cooking.css` omits a colour token the themes declare.
- [ ] When Close, Not now, or Log cook success navigates to Detail, then the cooking entry is replaced in history.
- [ ] Given an invalid id or a 404, then "We couldn't find that recipe." with a link to `/recipes`.
- [ ] The cooking route renders no app header, nav, or footer; every other route still does.

PR 2:

- [ ] Given a step with `durationMinutes: 85`, then a "Start 1h 25min timer" button is shown; a step without a
      duration shows none.
- [ ] When the timer is started and time advances 85 minutes (fake timers), then the alarm banner
      (`role="alert"`) appears, `navigator.vibrate` is called when present, and the tone starts; Dismiss stops all.
- [ ] Given a running timer, when Next step is pressed, then the chip keeps counting.
- [ ] Given `navigator.vibrate` is undefined, then the alarm still shows the banner and does not throw.
- [ ] Given `keepAwake` is on and `navigator.wakeLock` exists, then `request('screen')` is called on mount, the
      chip reads "Screen stays on", and the lock is released on unmount.
- [ ] Given the page becomes visible again after the lock was released, then the lock is requested again.
- [ ] Given `request` rejects, or `wakeLock` is absent, then the chip reads "Screen may turn off" and nothing
      else fails.
- [ ] Given `keepAwake` is off, then no lock is requested and no chip is shown.
- [ ] In Settings, the switch toggles `aria-checked` and persists to `localStorage`; default is on.

## 12. Test plan

- **Domain / handler / integration tests:** none — no backend change. Backend count stays 315.
- **Vitest + RTL:** the acceptance criteria above, at page level under `createMemoryRouter` (as Detail's tests);
  the ingredient-list split and URL parsing as pure functions with table tests; the shared quantity pipeline
  keeps `IngredientRail`'s existing tests green. Timers with `vi.useFakeTimers()` and a stubbed `Date.now`.
  `navigator.wakeLock`, `navigator.vibrate`, and `AudioContext` stubbed (jsdom has none of them).
- **Not covered, and why:** real wake-lock behaviour, real audio, and real vibration — jsdom has no such APIs,
  so tests prove the calls are made, not that a phone stays awake. Contrast is measured, not tested.
- **Manual verification:** on a real phone (Android Chrome, and iOS Safari if available): screen stays on for
  more than the device's auto-lock time; switching apps and back re-acquires the lock; the timer alarm is
  audible and vibrates (Android); Back/Next reachable one-handed; both themes show the same cooking palette.
  Because the dev server is `http://localhost:3000`, a phone on the LAN is not a secure context and **Wake Lock
  will be unavailable there** — test via a tunnel with HTTPS or Chrome's
  `--unsafely-treat-insecure-origin-as-secure` flag on Android, and record which was used.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `07-ux-ui` | placement details left open in §8.2 / §8.4, review in both themes and on a phone |
| 3 | `01-architect` | ADR-029 (short; no dependency, no layer change) |
| 4 | `03-senior-react` | route split, page, shared quantity pipeline, hooks, Settings switch |
| 5 | `06-qa-tester` | Vitest suites per §12, manual phone check |
| 6 | `04-code-reviewer` | review |

`02-senior-csharp`, `08-api-contract`, and `05-security-reviewer` are not involved: no backend, contract, or
stored-input change (§7, §10).

## 14. Assumptions made

Decided with the user on 2026-10-23:

- Phone on the counter is the primary use; the mobile layout is primary.
- Timer end: sound **and** vibration.
- Finish asks before logging, with a confirm button.
- Cooking mode keeps Detail's scaled servings and the Units preference.
- A recipe whose steps reference no ingredients shows the full ingredient list.

Calls made by `00-leader`, accepted by the user in summary:

- Wake-lock preference defaults to on; the chip reflects the real lock state.
- One timer at a time, started manually; an on-screen banner backs the sound (iOS has no vibration).
- Close (×) never confirms and never logs; Not now likewise.
- Servings and step in the URL; invalid values fall back silently.
- Exits replace the history entry; Detail's servings reset on return; not-found is the page's own; the alarm
  names "Step N" (settled after the builder check).
- "Also needed" for ingredients no step references; ", used" hidden text on struck items.
- Arrow keys on desktop; Back `aria-disabled` on step 1.
- Two PRs: screen first, timer + wake lock second.
- No undo in cooking mode (`UX-20` already covers the gap).

## 15. Follow-ups

- **Timer alarm while the page is hidden or the phone locked** — needs a service worker and Notifications;
  file in [../known-issues.md](../known-issues.md) when PR 2 ships if it proves to matter.
- **Remove a cook logged from cooking mode** — `UX-20`, unchanged.

## 16. Known issues and roadmap items touched

- Fixes: `R-23` (roadmap). Resolves spec 009 §4's parked Keep-screen-awake preference.
- Depends on: `R-17` and `R-22`, both shipped.
- On the [deploy gate](../roadmap.md#deploy-gate)? no.
