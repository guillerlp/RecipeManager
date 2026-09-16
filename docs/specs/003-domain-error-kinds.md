# Spec: Domain error kinds (R-05)

| | |
| --- | --- |
| **ID** | `003` |
| **Status** | shipped |
| **Author** | `00-leader` |
| **Created** | 2026-09-16 |
| **Branch** | `refactor/domain-error-kinds` |

---

## 1. Context

*(Written before implementation; the present tense describes the state this item started from.)*

`RecipeErrors` attaches HTTP status codes to domain errors (`.WithCode(422)` → `Metadata["ErrorCode"]`), so
`RecipeManager.Domain` — the project that references nothing precisely so that it depends on nothing — encodes
HTTP semantics. It is the one live layering violation (ADR-009). It also causes a real defect:
`ResultExtensions.CreateProblemDetails` takes the status from `errors.First()`, so a `Result` carrying both a
404 and a 422 returns whichever error happens to be first.

That defect is **latent** today: no handler currently returns mixed kinds (`RecipeNotFound` and the validation
errors are never combined). It becomes live the first time one does, and nothing would report it.

## 2. Goal

Domain errors carry a semantic kind, and the API layer alone maps kinds to HTTP statuses, choosing the most
severe error rather than the first.

## 3. In scope

- [ ] `ErrorKind` enum and `DomainError` class in `RecipeManager.Domain/Errors/`.
- [ ] All 11 `RecipeErrors` factories build a `DomainError`; the private `WithCode` helper is deleted.
- [ ] `ResultExtensions` maps kind → status and selects the primary error by severity.
- [ ] `RecipeManager.UnitTests` references `RecipeManager.Api`; new `ResultExtensionsTests`.
- [ ] The two unit tests asserting `Metadata["ErrorCode"]` assert the kind instead.
- [ ] Docs updated (section 16), `R-05` deleted from the roadmap, ADR-009 marked implemented, decisions-log entry.

## 4. Out of scope

- **A `Conflict` kind.** No code produces one. It is added with the first feature that needs it (optimistic
  concurrency via `xmin`, or a uniqueness rule), together with its status and severity rank.
- **Changing the status of kind-less errors.** `DeleteRecipeHandler`'s `catch` returns an untyped error that
  maps to 400 today; an unexpected failure arguably deserves 500. That is a behaviour change forbidden by this
  item's acceptance criterion, and it belongs with `SEC-06`, which concerns the same `catch` block.
- **The response body shape.** `ProblemDetails`, `field`, and `errors[]` (including `errors[].code` as the
  per-error HTTP status) are unchanged.
- Frontend, contract sync, migrations — none are affected (no agent rows for them in section 13).

## 5. Domain impact

- **New types:** `ErrorKind` (`Validation`, `NotFound`) and
  `sealed class DomainError(string message, ErrorKind kind) : Error(message)` with a read-only `Kind`.
- **Changed:** every `RecipeErrors` factory. Return types stay `Error`, so no caller changes. `RecipeNotFound`
  → `NotFound`; the other ten → `Validation`. `field` and `ServingsOutOfRange`'s `min` metadata are unchanged.
- **New/changed properties on `Recipe`:** none.
- **New/changed invariants:** none.
- **New/changed shape validation:** none.
- **Migration required:** no.
- **Known limitations touched:** none.

`.Field(...)` stays valid because FluentResults' `WithMetadata` mutates and returns the same instance, so the
runtime type remains `DomainError`.

## 6. API impact

- **New/changed endpoints:** none. Every externally visible status code is unchanged:

  | Verb | Route | Failures (before = after) |
  | --- | --- | --- |
  | GET | `/api/recipes/{id}` | 404 |
  | POST | `/api/recipes` | 400 shape, 422 invariants |
  | PUT | `/api/recipes/{id:guid}` | 400 shape, 404, 422 |
  | DELETE | `/api/recipes/{id:guid}` | 404; 400 on an unexpected persistence failure |

- **Mapping, owned by `ResultExtensions`:**

  | Kind | Status | Severity rank |
  | --- | --- | --- |
  | *(none — not a `DomainError`)* | 400 | 3 (highest) |
  | `NotFound` | 404 | 2 |
  | `Validation` | 422 | 1 |

  - The **primary error** is the first error with the highest rank. It supplies `Status`, `Title`, `Detail`,
    and `field`.
  - A kind-less error ranks highest because it signals a failure the domain did not model; a 422 must not
    mask it by suggesting the client can fix the request.
  - `NotFound` outranks `Validation`: validating something that does not exist is moot.
  - The kind → status and kind → rank mappings are `switch` expressions whose default arm **throws**
    `ArgumentOutOfRangeException`. Adding an `ErrorKind` without mapping it then fails the unit tests
    immediately instead of answering 400 silently; at runtime it would surface as a 500 through
    `ErrorHandlerMiddleware`, never as a plausible-looking wrong status.
  - `errors[]` still lists every error in original order, each with its own mapped `code`.
- **`RecipeDto` / `UpdateRecipeDto` changes:** none.
- **Breaking for the client?** no.
- **Cache impact:** none.

## 7. Frontend impact

None. The SPA does not read `errors[]` or `code`.

## 8. Architecture impact

- **ADR required:** no new ADR. ADR-009 (accepted 2026-07-26) is this decision; it is marked **implemented**
  with the two refinements below, following the ADR-008 precedent of a status update on implementation.
  - Refinement 1: the kind is carried by a typed `DomainError` subclass, not by metadata.
  - Refinement 2: `Conflict` is deferred until something produces it.
- **New dependency:** none.
- **Layer/dependency changes:** Domain loses its HTTP knowledge. `RecipeManager.UnitTests` gains a project
  reference to `RecipeManager.Api` (the project-reference table in `architecture.md` changes). No production
  reference changes.
- **New DI registrations:** none.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Enum stored in metadata (`WithMetadata("ErrorKind", kind)`) | Smallest diff; mirrors today's pattern | A misspelled string key silently falls through to the default status — the class of silent failure ADR-008/ADR-011 removed elsewhere |
| One subclass per kind (`ValidationError`, `NotFoundError`) | FluentResults' most common idiom; no enum | No single enumerable set of kinds, so an exhaustive mapping cannot be asserted; every kind is a new class |
| Keep `errors.First()`, require handlers to order errors | No API-layer logic | Correctness depends on every handler's discipline and nothing enforces it — the current convention, and the current bug |
| Test `ResultExtensions` in `IntegrationTests` | No new project reference | Pure in-memory tests misfiled next to `WebApplicationFactory` tests, and invisible to `run-coverage.ps1` |

- **Pattern applied:** anti-corruption / translation at the boundary — the domain states *what* failed, the
  transport layer decides *how to say it*. Also the port-and-adapter split already used by
  `IRecipeRepository` (Domain) vs. `RecipeRepository` (Infrastructure).
- **What this makes harder:** every new kind needs three coordinated edits (enum value, status, rank) — mitigated
  by the throwing default arm. `UnitTests` now compiles ASP.NET Core. An error built as a plain `Error` rather
  than a `DomainError` is always treated as the most severe kind, so domain code must use `DomainError`.

## 9. Security impact

- **New user-controlled input:** none.
- **User content rendered in the SPA:** no.
- **File upload:** no.
- **Auth/ownership implications:** none; the app remains unauthenticated (`SEC-01`).
- **Config/secrets touched:** none.
- **Standing gaps affected:** none. `SEC-06` (the delete `catch` echoing `ex.Message`) is untouched and remains
  open; it gains a note that its fix should also decide the status of kind-less errors.

## 10. Acceptance criteria

- [ ] `RecipeManager.Domain` contains no `WithCode`, no `"ErrorCode"` key, and no HTTP status literal.
- [ ] Given a `Result` with a `Validation` and a `NotFound` error (in that order), when converted, then the
      response is 404 and `Detail`/`field` come from the `NotFound` error.
- [ ] Given a `Result` with a kind-less error and a `Validation` error, when converted, then the response is 400.
- [ ] Given two `Validation` errors, when converted, then the response is 422 with the first error's
      `Detail`/`field`, and `errors[]` lists both with `code` 422.
- [ ] Every `ErrorKind` value maps to a status without throwing.
- [ ] Every `RecipeErrors` factory returns a `DomainError` with the kind listed in section 5.
- [ ] All 14 integration tests pass unchanged — the regression net for the endpoint status codes in section 6.
- [ ] `dotnet build` reports 0 warnings; `dotnet test` passes with 84 + the new tests.

## 11. Test plan

- **Domain unit tests:** `RecipeTests.Create_WithInvalidTitle_ShouldIncludeErrorCodeInMetadata` becomes a kind
  assertion (`BeOfType<DomainError>()`, `Kind == Validation`). A `[Theory]` over all `RecipeErrors` factories
  asserts each kind.
- **Handler unit tests:** `GetRecipeByIdHandlerTests.Handle_WhenRecipeNotFound_ShouldIncludeErrorMetadata`
  asserts `Kind == NotFound` and keeps its `field` assertion.
- **New `RecipeManager.UnitTests/Api/Extensions/ResultExtensionsTests.cs`:** each kind → status; the three
  mixed-kind cases above; tie-break by order; `errors[]` contents; `Enum.GetValues<ErrorKind>()` all mapped;
  success paths (`Ok`, `NoContent`, `CreatedAtAction`) unchanged.
- **Integration tests:** existing 14, unchanged.
- **Not covered, and why:** a mixed-kind result through a real endpoint — no handler produces one, so it is
  only reachable at unit level.
- **Manual verification:** none needed beyond the integration tests; no endpoint behaviour changes.

## 12. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-009 status update, constraints (this spec, section 8) |
| 3 | `02-senior-csharp` | domain + api changes + backend tests |
| 4 | `06-qa-tester` | `ResultExtensionsTests` + coverage statement |
| 5 | `04-code-reviewer` | review |

`08-api-contract`, `07-ux-ui`, `03-senior-react`: no contract or UI change. `05-security-reviewer`: error
output shape is unchanged, so not triggered.

## 13. Decisions taken with the user (2026-09-16)

- Kind carried by a typed `DomainError` subclass, not metadata or one class per kind.
- `ErrorKind` starts with `Validation` and `NotFound` only.
- Severity: kind-less > `NotFound` > `Validation`; kind-less stays 400.
- `ResultExtensions` tests live in `UnitTests`, which gains an `Api` reference.

## 14. Assumptions made

- ADR-009 is updated in place (status + refinements) rather than superseded by a new ADR, since it was accepted
  but never implemented and its direction is unchanged.
- `errors[].code` remains the per-error HTTP status rather than the kind name, since the acceptance criterion
  forbids externally visible changes and no client reads it.

## 15. Follow-ups

- Note on `SEC-06` in [../known-issues.md](../known-issues.md): when that `catch` is fixed, decide whether an
  unexpected persistence failure should be 500 rather than today's 400.

## 16. Known issues and roadmap items touched

- Fixes: `R-05` (delete from [../roadmap.md](../roadmap.md)); ADR-009 implemented.
- Docs to update in the same PR, each currently describing `WithCode`/`ErrorCode`/`errors.First()`:
  `architecture.md` (Domain layer, error-channel table and paragraph, project-reference table, ADR-002 note,
  ADR-009 status), `conventions.md` (error handling), `domain-model.md` (invariants table column),
  `tech-stack.md` (FluentResults row), `agents/01-architect.md`, `agents/02-senior-csharp.md`,
  `agents/04-code-reviewer.md`, `agents/06-qa-tester.md`, `workflows/feature-workflow.md` (steps 3 and 6),
  `workflows/bugfix-workflow.md`, `CLAUDE.md` (unit-test count), `decisions-log.md` (append; the 2026-07-26
  entry stays as history).
- Depends on: nothing.
- On the [deploy gate](../roadmap.md#deploy-gate)? no.
