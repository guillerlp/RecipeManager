# Learning mode

**This project exists partly so its owner learns while building it.** Shipping working code is necessary but
not sufficient: an agent that produces a correct change and no explanation has done half the job.

Every agent must make its reasoning visible — what it decided, why, what it rejected, and what the choice costs.

---

## The contract

1. **Explain the reasoning, not just the outcome.** "Added a `Result.Fail` here" is a changelog. "Returned a
   failed `Result` instead of throwing, because this is an expected outcome the caller must handle, and
   exceptions in this codebase are reserved for genuinely exceptional failures caught by the middleware" is an
   explanation.
2. **Name the alternatives you rejected and why.** A decision with no visible alternatives teaches nothing —
   it looks like the only option, which it rarely is.
3. **State the cost.** Every choice makes something harder. If you cannot name what this makes harder, you have
   not finished thinking about it.
4. **Name the pattern.** If a change applies a named pattern (decorator, aggregate root, factory method, port
   and adapter, CQRS, unit of work, value object), **say the name** so it can be looked up independently.
5. **Do not fabricate a rationale.** If something is done a certain way because that is how the surrounding
   code does it, say exactly that. "Consistency with the existing code" is a legitimate reason; inventing a
   principled-sounding justification after the fact is not.
6. **Point at the code, not just the concept.** This repo already implements many of these patterns — the best
   explanation usually ends with "read `X.cs` for the version already in the codebase".

---

## Calibration — what deserves an explanation

The rule is not "narrate everything". Over-explaining buries the parts that matter.

**Always explain**

- Any decision that had a real alternative — the choice between two designs, two libraries, two layers.
- The first application of a pattern, or a new use of an existing one.
- Anything touching the layer boundaries, the domain invariants, or the error-handling channels.
- Any trade-off between correctness, simplicity, and performance.
- Why a bug happened, not just what fixed it — the root cause is the lesson.
- Why a review finding is blocking, in terms of what it would actually cause.
- Anything where the repo's convention differs from what a reader might expect from general .NET or React
  advice found online.

**Explain briefly (one line)**

- Routine applications of an established convention in this repo.
- Naming, file placement, test structure.

**Do not explain**

- Mechanical edits with no decision content (renaming a variable, updating an import).
- General programming basics unless they were the actual cause of a problem.
- The same thing twice in one response.

---

## Format

For a decision with real weight, use this compact block. It is deliberately short — five lines, not five
paragraphs.

```md
**Decision:** what was chosen
**Why:** the reason, in terms of this codebase specifically
**Rejected:** the alternative(s) — and what made them worse *here*
**Cost:** what this now makes harder or forecloses
**Pattern / read more:** the pattern's name, and the file in this repo that already demonstrates it
```

For smaller choices, a single sentence carrying the same content is enough: *"Used `AsNoTracking()` because
this read never feeds an update — tracking would cost memory and change-detection time for nothing."*

### Language

Explanations delivered **in conversation** may be in Spanish, matching whatever language the user is using.
Everything written into a **file** — code comments, ADRs, spec documents, PR descriptions — stays in English,
per the global language rule. An explanation is not an exception to that rule; it just usually lives in chat.

### Where explanations end up

Most live in the conversation and are gone by next week. The ones worth keeping go to
[decisions-log.md](decisions-log.md) — decisions that would be hard to justify in six months, lessons a bug
taught, and reversals of earlier calls. Each entry ends with a **Takeaway**: the generalisable lesson, which is
the part that transfers to the next project.

Append there when a decision meets that bar; do not copy every explanation into it, or it stops being worth
re-reading.

---

## What each agent should teach

| Agent | The kind of understanding it is responsible for |
| --- | --- |
| `00-leader` | Why the work decomposes into these pieces, in this order, and what would break if done in another order |
| `01-architect` | The pattern being applied, its canonical name, what it buys, what it costs, and why the alternatives lose *in this codebase* |
| `02-senior-csharp` | .NET and EF Core idioms — why this construct over that one, how EF translates the code, what the runtime actually does |
| `03-senior-react` | React and TypeScript reasoning — render behaviour, state ownership, why a hook belongs where it is, what causes re-renders |
| `04-code-reviewer` | Why a finding matters: the concrete failure it would cause, not the rule it violates |
| `05-security-reviewer` | The attack, concretely — who does what, and what they get. Never just an OWASP category name |
| `06-qa-tester` | Why a test belongs at this level, and precisely what that level **cannot** catch |
| `07-ux-ui` | Why an accessibility or design rule exists, and who it fails when it is ignored |
| `08-api-contract` | How contract drift happens silently and what makes it detectable |

---

## Concepts this repo already demonstrates

The most effective way to learn here is to read the code that already implements a pattern. When explaining one
of these, point at the file.

### Backend

| Concept | Where it lives | What to notice |
| --- | --- | --- |
| **Aggregate root with enforced invariants** | `RecipeManager.Domain/Entities/Recipe.cs` | Private setters and private constructors mean an invalid `Recipe` cannot be constructed at all. Contrast with a class of public setters validated by the caller. |
| **Static factory method** | `Recipe.Create` | Returns `Result<Recipe>` instead of throwing. A constructor cannot fail gracefully; a factory can. |
| **Result / railway-oriented error handling** | `FluentResults` throughout, `RecipeManager.Domain/Errors/RecipeErrors.cs` | Expected failures are values, not exceptions. Note that only *unexpected* failures reach `ErrorHandlerMiddleware`. |
| **Port and adapter (dependency inversion)** | `IRecipeRepository` in **Domain**, implementation in **Infrastructure** | The interface lives with the code that *needs* it, not with the code that implements it. This is what lets Domain reference nothing. |
| **Decorator pattern** | `CachedRecipeRepository` wrapping `RecipeRepository`, wired by `services.Decorate(...)` | Same interface, added behaviour, handlers unaware. Compare with putting `if (cached)` inside the handler. |
| **CQRS without a mediator library** | `RecipeManager.Application/Common/Interfaces/Messaging/`, `.../Dispatchers/` | Commands mutate, queries read, each with one handler. See ADR-001 for why MediatR was rejected. |
| **Validation in two layers** | `RecipeValidationRules` vs. `Recipe.ValidateProperties` | Shape (null, length, bounds) vs. business rules. `Title = ""` passing one and failing the other is the clearest example. |
| **Entity equality by identity** | `RecipeManager.Domain/Shared/Entity.cs` | Two `Recipe` objects are equal if their ids match, regardless of field values — the opposite of a value object. |
| **EF Core primitive collections** | `Recipe.Ingredients` → `text[]` | EF 9+ maps `IReadOnlyList<string>` to a native PostgreSQL array with no configuration. |
| **Test doubles and interaction testing** | `RecipeManager.UnitTests`, NSubstitute | `Received(1)` asserts an interaction happened; contrast with asserting only the return value. |
| **Integration testing an ASP.NET app in-process** | `IntegrationTestBase`, `WebApplicationFactory<Program>` | The real pipeline runs — routing, model binding, filters, DI — with only the database swapped. |

### Frontend

| Concept | Where it lives | What to notice |
| --- | --- | --- |
| **Server state vs. client state** | `useRecipes` (TanStack Query) vs. `useState` in `RecipePage` | Server data is cached, shared, and invalidated; UI state is local. Conflating them is why people reach for Redux unnecessarily. |
| **Context + guard hook** | `ThemeContext` and `useTheme` | The hook throws when the provider is missing, converting a silent-undefined bug into a loud one. |
| **Memoised context value** | `ThemeContext`'s `useMemo` + `useCallback` | Without them, every provider render gives consumers a new object and re-renders all of them. |
| **Derived state via `useMemo`** | `RecipeList.filteredRecipes` | Filtering is computed from props and state, never stored in its own `useState` — no synchronisation bug is possible. |
| **Semantic element selection** | `RecipeCard` rendering `<article>` or `<button>` by whether `onClick` exists | Accessibility falls out of the element choice instead of being bolted on with ARIA. |
| **Design tokens and theming** | `styles/themes/*.css`, `data-theme` on `<html>` | One attribute swaps the whole palette because no component hard-codes a colour. |

---

## Worked example

The kind of answer this project wants, for a change as small as adding a cache to a new repository method:

> **Decision:** invalidate both `recipes_all` and `recipe_{id}` inside `UpdateAsync`, rather than letting the
> entries expire.
>
> **Why:** `CacheDuration.DefaultExpiration` is 10 minutes, so a stale list would survive an edit for up to ten
> minutes. Writes are rare and reads are frequent here, so paying invalidation cost on write is clearly right.
>
> **Rejected:** shorter TTLs — that degrades every read to fix a write-path problem. Also rejected: invalidating
> only the per-id key, which leaves the list endpoint serving the old title.
>
> **Cost:** every new write method must remember to invalidate, and nothing enforces it. That is exactly why
> `TEST-02` (no cache-invalidation test) is rated High.
>
> **Pattern / read more:** cache-aside with explicit invalidation, implemented as a decorator —
> `RecipeManager.Infrastructure/Repositories/Recipes/CachedRecipeRepository.cs`, ADR-003.
