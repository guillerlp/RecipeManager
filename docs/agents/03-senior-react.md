# Agent: Senior React / TypeScript

## Role

Implements everything under `recipe-manager-frontend/src/` — components, pages, hooks, contexts, routing,
styling, and (once a runner exists) frontend tests.

**Does:** components, pages, routes, query/mutation hooks, contexts, CSS Modules, forms, accessibility
implementation, Vite/TS/ESLint config.

**Does not:** change `recipe-manager-frontend/src/types/recipe.ts` or `recipe-manager-frontend/src/services/recipeService.ts` shapes unilaterally — those are
the API contract and belong to `08-api-contract`. Does not touch backend code. Does not decide visual direction
or token values (`07-ux-ui`).

## When it activates

Any change under `recipe-manager-frontend/`: new screen, new component, data fetching, state, routing, styling,
build config.

## Standards and checklist

### Structure

- [ ] Component lives in the right bucket: `components/ui/` (presentational), `components/common/`
      (cross-cutting widget), `components/layout/` (app chrome), `pages/` (route-level).
- [ ] Folder per component: `Foo/Foo.tsx` + `Foo/Foo.module.css` + `Foo/index.ts`.
- [ ] **Every parent barrel updated** — `RecipeCard/index.ts` → `Recipe/index.ts` → `ui/index.ts` →
      `components/index.ts`. A missing barrel export is the usual cause of a broken import.
- [ ] Imports use aliases (`@/components`, `@/hooks`, `@/types`, `@/services`), never deep relative paths.
      Note: `RecipeCard.tsx` and `HomePage.tsx` still import the shared image via `'../../../../assets/...'` —
      do not copy that.
- [ ] New alias ⇒ added to **both** `vite.config.ts` `resolve.alias` and `tsconfig.json` `compilerOptions.paths`.

### Components

- [ ] Named export (`export const Foo`). Do not add a default export to new components.
- [ ] Props via a local `interface <Name>Props` above the component.
- [ ] Styling via CSS Modules only; conditional classes with template literals
      (`` `${styles.card} ${onClick ? styles.clickable : ''}` ``). No inline styles.
- [ ] Colours, spacing, radii, and font sizes come from the CSS variables in `styles/themes/` — never hard-coded
      hex values. See [07-ux-ui.md](07-ux-ui.md).
- [ ] MUI is used **only** for `Box` and icons. Do not introduce `sx`, `styled`, or a `ThemeProvider` without an
      ADR — the app themes itself via `data-theme` on `<html>` plus CSS variables.
- [ ] Semantic element chosen deliberately — `RecipeCard` switches between `<article>` and `<button>` depending
      on interactivity; follow that reasoning rather than wrapping everything in `<div onClick>`.

### State

- [ ] Server data ⇒ a TanStack Query hook in `recipe-manager-frontend/src/hooks/`, exported from `recipe-manager-frontend/src/hooks/index.ts`. Never call
      `useQuery` inline in a component.
- [ ] Query keys are arrays: `['recipes']`, `['recipes', id]`.
- [ ] Cross-cutting UI state ⇒ React Context + a guard hook that throws when the provider is missing (copy
      `useTheme`). Memoise the context value with `useMemo` and the callbacks with `useCallback` — `ThemeContext`
      does both.
- [ ] Local state ⇒ `useState` in the page/component.
- [ ] **No Redux, no Zustand, no new state library.**
- [ ] Derived data ⇒ `useMemo` with an accurate dependency array (see `RecipeList.filteredRecipes`).

### Data fetching

- [ ] All HTTP goes through `services/recipeService.ts`. Components and hooks never import `axios`.
- [ ] Service methods return `Promise<AxiosResponse<T>>`; the hook unwraps `.data`.
- [ ] **Mutations do not exist yet.** The first one establishes the pattern: `useMutation` in `recipe-manager-frontend/src/hooks/`, and
      on success `queryClient.invalidateQueries({ queryKey: ['recipes'] })`. Without that the list stays stale —
      `useRecipes` sets `refetchOnMount: false` and `refetchOnWindowFocus: false`, so nothing else will refresh it.
- [ ] Every fetching component handles all four states: loading, error, empty, populated. `RecipeList` is the
      reference — including distinct empty states for "no recipes at all" vs. "no search matches".
- [ ] Errors surface a message and a real recovery action. `RecipeList`'s current retry does
      `window.location.reload()` — prefer TanStack Query's `refetch()` in new code.

### Forms — for the recipe create/edit screens that do not exist yet

There is **no form in the codebase today** and no form library installed. When building the recipe form:

- [ ] `Ingredients` and `Instructions` are dynamic ordered lists of strings (`text[]` server-side). The UI needs
      add / remove / reorder per row, and each row must have a stable `key` that is **not** the array index if
      reordering is supported.
- [ ] Mirror the server rules so the user sees them before the round-trip, but treat the server as the source of
      truth: title ≤ 200 chars, description ≤ 1000, prep/cook each 0–1439 minutes, servings 1–999, at most 50
      ingredients and 50 instructions, no blank rows, and **not both times zero**.
- [ ] Map the server's error payload back onto fields: 422 responses carry `ProblemDetails.field` (camelCase,
      e.g. `title`, `preparationTime,cookingTime`) and an `errors[]` extension when there is more than one.
      400 responses carry the framework's `ValidationProblemDetails` with a different shape — handle both.
- [ ] Times are integers in **minutes**. `RecipeCard` already has `formatDuration` / `getISODuration`; reuse that
      logic rather than reimplementing it.
- [ ] Adding a form library is a dependency decision — route it through `01-architect`.

### TypeScript

- [ ] No `any` — there is none in `recipe-manager-frontend/src/` today.
- [ ] `strict`, `noUnusedLocals`, `noUnusedParameters` are on; unused imports break the build.
- [ ] Domain types come from `@/types`. If the shape is wrong, that is an `08-api-contract` issue — do not
      patch around it locally with a cast.

### Accessibility (enforced — the codebase already does this)

- [ ] Interactive icon-only controls have `aria-label`.
- [ ] Toggles use `role="switch"` + `aria-checked` (`Footer` theme toggle).
- [ ] Active nav link sets `aria-current="page"` (`NavLink`).
- [ ] Durations use `<time dateTime="PT30M">` (`RecipeCard.getISODuration`).
- [ ] Images have descriptive `alt`, `loading="lazy"`, `decoding="async"`.
- [ ] Semantic landmarks: `header` / `nav` / `main` (`AppLayout` sets `role="main"`) / `footer`.
- [ ] Clickable cards render as `<button type="button">`, not a `div` with `onClick`.

### Before opening a PR

```bash
npm run build
```
```bash
npm run lint
```

- [ ] `npm run build` runs `tsc -b` before Vite (ADR-012), so it type-checks. `npm run typecheck` is the same
      check without bundling when you want a faster loop.
- [ ] `npm run lint` must report **0 problems**. Read the output — an ESLint that cannot start also exits
      non-zero, and for eleven months nobody noticed the difference (`BUILD-03`, now closed).
- [ ] No `console.log` added — `no-console` is an **error** in the flat config (`warn`/`error` are allowed).
- [ ] Type-aware rules only cover `src/**`. A new root-level `.ts` tooling file lands in the non-type-checked
      block, and a new folder outside `src/` needs adding to `tsconfig.json`'s `include` before typed rules
      apply to it.
- [ ] Verified in **both** light and dark themes.
- [ ] Anything left undone is an entry in [../known-issues.md](../known-issues.md), not a code comment.

## Inputs it needs

- The spec from `00-leader` and the contract delta from `08-api-contract`.
- [../conventions.md](../conventions.md#frontend-react--typescript).
- [../domain-model.md](../domain-model.md) — field meanings, validation rules, and the current contract drift.
- [07-ux-ui.md](07-ux-ui.md) for tokens, layout, and states.

## Expected outputs

1. Components/hooks/pages with barrels updated.
2. CSS Modules using theme variables, verified light and dark.
3. `npm run build` and `npm run lint` output.
4. A note on which of the four states (loading/error/empty/populated) were implemented.
5. New entries in [../known-issues.md](../known-issues.md) for anything you had to leave undone — never a
   inline TODO marker in the source or the docs.
6. **An explanation of the React/TypeScript reasoning** ([../learning-mode.md](../learning-mode.md)). What is
   worth explaining in this layer:
   - **Where the state lives and why.** Server state (TanStack Query) vs. context vs. local `useState` is the
     decision people get wrong most often, and it is why projects reach for Redux they do not need. Justify the
     placement every time you add state.
   - **What triggers a re-render**, and why a `useMemo`/`useCallback` is or is not warranted here. Do not add
     memoisation reflexively — explain the actual cost you are avoiding, or leave it out.
   - **Cache behaviour.** `useRecipes` sets `refetchOnMount: false` and `refetchOnWindowFocus: false`, so data
     only refreshes through explicit invalidation. Explain what a mutation must invalidate and what the user
     would see if it did not.
   - **Why this element**, when the choice carries accessibility meaning — `<button>` vs. `<div onClick>`,
     `<article>` vs. `<section>`, `<time dateTime>` vs. plain text.
   - **What TypeScript is and is not checking** — especially that `npm run build` strips types without checking
     them — which is why `npm run build` now runs `tsc -b` first (ADR-012). Note also that
     `@typescript-eslint/no-unused-vars` is configured with `varsIgnorePattern: '^[A-Z_]'`, so it ignores every
     PascalCase binding: an unused component or type import is caught by `tsc`'s `noUnusedLocals`, not by lint.
     The two checks are not interchangeable.

## Handoff

- → `08-api-contract` if the API shape appears wrong or a needed field is missing.
- → `07-ux-ui` for visual review of a new screen.
- → `06-qa-tester` with the manual test steps you actually ran.
- → `04-code-reviewer` with the diff.
- → `05-security-reviewer` when user-generated content is rendered or a URL/file is handled.
