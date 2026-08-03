# Decisions log

A running record of decisions taken on this project **and what was learned from them** — written to be
re-readable months later, when the reasoning has been forgotten but the code is still here.

## How this differs from the other documents

| Document | Answers |
| --- | --- |
| [architecture.md](architecture.md) ADRs | *What* was decided structurally, in the terse formal record |
| [learning-mode.md](learning-mode.md) | *How* agents must explain decisions |
| **This file** | *Why* a decision was made, what was rejected, and **what it taught** — including decisions too small for an ADR |

An ADR is a record for the codebase. This is a record for the person building it.

## What goes in

- Any decision you would struggle to justify in six months.
- Anything where you learned something you did not know before, including from a bug.
- Decisions that were **reversed** — those teach more than the ones that held.

## What stays out

- Routine applications of an existing convention.
- Anything already fully captured by an ADR with nothing to add.

Newest first. Read bottom-up to follow the project chronologically. Entries are append-only: when a decision is
superseded, add a new entry and link back rather than editing history.

---

## Concept index

Jump to every entry touching a topic.

| Concept | Entries |
| --- | --- |
| CQRS / dispatching | [2026-07-26 Scrutor](#2026-07-26--auto-register-handlers-instead-of-listing-them), [2025-08-28 CQRS without MediatR](#2025-08-28--hand-rolled-cqrs-instead-of-mediatr) |
| Layering / dependency direction | [2026-07-26 Error kinds](#2026-07-26--http-status-codes-do-not-belong-in-the-domain) |
| Error handling | [2026-07-26 Error kinds](#2026-07-26--http-status-codes-do-not-belong-in-the-domain), [2025-09-18 FluentResults](#2025-09-18--expected-failures-are-values-not-exceptions) |
| Caching | [2025-08-30 Decorator](#2025-08-30--caching-as-a-decorator-not-as-handler-code) |
| Domain modelling | [2026-07-26 Structured ingredients](#2026-07-26--free-text-ingredients-are-a-shortcut-with-an-expiry-date) |
| Testing | [2026-07-26 Testcontainers](#2026-07-26--ef-inmemory-is-not-a-database), [2025-10-08 Integration tests](#2025-10-08--integration-tests-need-an-escape-hatch-and-escape-hatches-need-guards) |
| Project direction | [2026-07-26 Project stance](#2026-07-26--practice-project-with-deployment-intent) |
| Tooling / infrastructure | [2026-07-25 .NET 10 + PostgreSQL](#2026-07-25--net-10-and-postgresql) |

---

## Entries

### 2026-07-26 — Free-text ingredients are a shortcut with an expiry date

**Context.** `Recipe.Ingredients` is `IReadOnlyList<string>`, stored as PostgreSQL `text[]`. It works, and the
app functions — which is exactly what makes a shortcut dangerous, because nothing forces a reckoning.

**Decision.** Move to structured ingredients (`Quantity`, `Unit`, `Name`). Recorded as `R-10`. Explicitly
labelled a temporary shortcut in [domain-model.md](domain-model.md) so future work does not entrench it.

**Rejected.** Keeping free text permanently. It is genuinely simpler, and for a recipe *viewer* it would be
enough — but it makes serving scaling, shopping lists, unit conversion, and ingredient search impossible, and
those are the features a recipe manager eventually grows.

**Cost.** The migration is the hard part: `"200g flour"` cannot be parsed into structured data reliably, so
existing rows need a strategy decided in advance. Delaying makes this strictly worse as data accumulates.

**Takeaway.** *The cost of a data-model shortcut is not paid when you take it — it is paid at migration time,
and it grows with every row.* Recognising which shortcuts have an expiry date, and writing that down while the
reasoning is fresh, is the difference between a deliberate trade-off and accidental debt. Writing "this is
temporary" in the docs is what keeps it from silently becoming permanent.

---

### 2026-07-26 — EF InMemory is not a database

**Context.** The 14 integration tests run against `Microsoft.EntityFrameworkCore.InMemory`. They pass. They also
cannot detect anything provider-specific: `text[]` behaviour, PostgreSQL identifier folding, real constraint
violations, or concurrency.

**Decision.** Move to Testcontainers with real PostgreSQL (`R-06`), deferred until CI exists since it needs
Docker in both places.

**Rejected.** Staying on EF InMemory. It is faster and needs no Docker — but the EF Core team itself recommends
against it for integration testing, precisely because passing tests imply guarantees the provider does not
give.

**Cost.** Slower tests and a Docker dependency for every developer and every CI run.

**Takeaway.** *A test suite's value is capped by how closely it resembles production.* A green suite that
cannot fail for the reasons production fails is worse than a smaller honest one, because it converts unknown
risk into false confidence. Worth asking of any test setup: what class of bug is this **structurally incapable**
of catching?

---

### 2026-07-26 — HTTP status codes do not belong in the domain

**Context.** `RecipeErrors.TitleRequired()` attaches `.WithCode(422)` — an HTTP status — to a domain error. So
`RecipeManager.Domain`, the project that references nothing precisely so it depends on nothing, encodes
knowledge of HTTP.

**Decision.** Domain errors will carry a semantic kind (`Validation`, `NotFound`, `Conflict`); the API layer
owns the kind → status mapping. ADR-009, `R-05`.

**Rejected.** Leaving it. It works today and keeps controllers thin, which is why it was done — a reasonable
call under delivery pressure, not a careless one.

**Cost.** Touching every `RecipeErrors` factory plus `ResultExtensions`, with the integration tests as the
regression net.

**What made it worth fixing.** The layering violation was abstract, but it produced a concrete bug:
`ResultExtensions.CreateProblemDetails` derives the response status from `errors.First()`, so a `Result`
carrying both a 404 and a 422 returns whichever happens to be first. That is not a coincidence.

**Takeaway.** *Layering violations announce themselves as bugs before they announce themselves as design
problems.* "The domain shouldn't know about HTTP" sounds like purity until it produces a wrong status code.
When a rule feels academic, the useful question is not "is this principled?" but "what does breaking it
actually cost me?" — and if you cannot answer, the rule may genuinely not apply.

---

### 2026-07-26 — Auto-register handlers instead of listing them

**Context.** Every CQRS handler had to be added by hand to `ServiceInitializer.RegisterCqrsHandlers()`. A
forgotten line compiled cleanly and threw `InvalidOperationException` at runtime, on the first request that
dispatched that command.

**Decision.** Discover handlers by assembly scanning with Scrutor. ADR-008. **Implemented 2026-08-03** —
`RegisterCqrsHandlers` deleted, the scan added to `RegisterCqrsDispatchers`, and
`CqrsHandlerRegistrationTests` added as the resolution net.

**Rejected.** *(a)* Keeping manual registration and relying on the review checklist — but a rule enforced only
by attention fails eventually, and this one fails in production. *(b)* Adopting MediatR, which solves this and
adds pipeline behaviours — but it is commercially licensed from v12, and the hand-rolled dispatcher was a
deliberate choice worth preserving.

**Cost.** Registration becomes implicit and therefore invisible. Mitigated by a container-resolution test that
asserts every handler interface resolves — otherwise the failure just moves somewhere less obvious.

**The detail that decided it.** Scrutor was **already a dependency**, used only for `Decorate`. The fix cost
nothing new.

**Takeaway.** *Prefer failures the compiler or a test can catch over failures that require discipline.* And
before adding a library to solve a problem, check what the existing dependencies already do — Scrutor's
scanning is its main feature, and this project had been using only its smallest one.

---

### 2026-07-26 — Practice project with deployment intent

**Context.** Whether the missing authentication, CI, and versioning are critical defects or acceptable
simplifications depends entirely on what this project is for — and that had never been written down.

**Decision.** Practice project with deployment intent: the production-grade bar applies, and security gaps are
**sequenced behind a [deploy gate](roadmap.md#deploy-gate) rather than waived**.

**Rejected.** *(a)* "Local tool only" — would have justified permanently dropping auth and rate limiting, but
forecloses deployment and removes the reason to practise those patterns at all. *(b)* "Production product now"
— would make `SEC-01`/`SEC-02` block every feature, which is wrong for something still being learned on.

**Cost.** More items stay open than a purely local tool would carry, and the deploy gate must be honoured
rather than quietly eroded when deployment starts to look appealing.

**Takeaway.** *"What is this project for?" is a technical question, not a philosophical one.* It determines
severity ratings, what counts as done, and which corners are legitimate. It is worth answering explicitly and
early — every later prioritisation call inherits from it.

---

### 2026-07-25 — .NET 10 and PostgreSQL

**Context.** The project ran on .NET 8 with SQL Server (`Integrated Security=True`, Windows-only auth).

**Decision.** Upgrade all six projects to `net10.0`, replace `Microsoft.EntityFrameworkCore.SqlServer` with
Npgsql, pin the SDK in `global.json`. ADR-007. Commit `d2d490d`.

**Cost, and the parts that were not obvious.** The connection string had to become a **password-less
template** — SQL Server integrated auth needed no password, PostgreSQL does, so the secret moved to
user-secrets. `nvarchar(max)` became `text`, `uniqueidentifier` became `uuid`, and `Ingredients`/`Instructions`
went from a JSON string to a native `text[]` — a *better* representation, since PostgreSQL can query arrays.
PostgreSQL also folds unquoted identifiers to lowercase while EF creates `"Recipes"`, so raw SQL needs quoting.

The package upgrades that came along with it introduced **7 build warnings** in the test project
(`BUILD-01`, `BUILD-02`) — NSubstitute 6 annotated `Arg.Is<T>` as nullable, and xUnit's analyzer started
flagging `[InlineData(null)]` on non-nullable parameters.

**Takeaway.** *A database swap is never only a connection-string change.* Types, identifier casing, secret
handling, and the capabilities available to you all shift. And a coordinated dependency upgrade will surface
new analyser warnings in code that did not change — budget for that instead of treating it as noise.

---

### 2025-10-08 — Integration tests need an escape hatch, and escape hatches need guards

**Context.** Integration tests need to substitute the real database, but `Program.cs` registers the DbContext
and applies migrations at startup, so `WebApplicationFactory` alone could not intervene.

**Decision.** `Program.Main` skips DbContext registration and migrations when
`EnvironmentName == "IntegrationTest"` — and **throws in RELEASE builds** if that environment name is used.
ADR-005. Commits `b15bb11`, then `898c9ce`.

**Why the second commit exists.** The first version added the escape hatch. The follow-up added the `#if DEBUG`
guard, because an environment variable that disables database configuration is a production hazard: set
`ASPNETCORE_ENVIRONMENT=IntegrationTest` on a real deployment and the app starts in an undefined state.

**Takeaway.** *Any hook added for testing is also an attack surface and an operational footgun.* The right
reflex is to add the hatch and the guard together — ask immediately "what happens if someone sets this in
production?" Notice that the fix here was a compile-time guard, not documentation or a naming convention: the
hatch cannot exist in a release binary at all.

---

### 2025-09-18 — Expected failures are values, not exceptions

**Context.** Validation failures and not-found conditions needed to reach the client as proper HTTP responses.

**Decision.** Adopt FluentResults. Domain and application failures return `Result`/`Result<T>`; only genuinely
unexpected failures throw and are caught by `ErrorHandlerMiddleware`. ADR-002. Commits `1a1f6de`, `2f3fb86`.

**Rejected.** Custom exception types per failure (`RecipeNotFoundException`) mapped in middleware. Common in
.NET, and it keeps handlers terse — but it uses exceptions for control flow, which is expensive, hides the
failure path from the method signature, and makes "this can fail" invisible at the call site.

**Cost.** Every caller must check `IsFailed`; nothing forces them to. The compiler will not catch an ignored
`Result` the way an uncaught exception announces itself.

**Takeaway.** *A return type that includes failure makes the failure path visible; an exception makes it
invisible.* `Task<Result<RecipeDto>>` tells you this can fail before you read the body. The trade is that
exceptions are impossible to ignore silently and `Result` is not — which is the actual reason this pattern
needs discipline, and the honest counter-argument to it.

---

### 2025-08-30 — Caching as a decorator, not as handler code

**Context.** Recipe reads were repetitive and hit the database every time. The obvious implementation is a
cache lookup at the top of each query handler.

**Decision.** `CachedRecipeRepository` implements `IRecipeRepository` and wraps the real one, wired with
Scrutor's `services.Decorate(...)`. Handlers are unaware caching exists. ADR-003. Commit `1f7e8c0`.

**Rejected.** Cache lookups inside the handlers — fewer files and a more obvious control flow, but it puts an
infrastructure concern in the application layer, repeats itself in every handler, and makes handler unit tests
require a cache mock.

**Cost.** Caching becomes invisible at the call site: a developer reading `GetAllRecipesHandler` sees no hint
that results may be stale. And every new `IRecipeRepository` method must be implemented **twice**, with an
invalidation decision each time.

**Takeaway.** *The decorator pattern's value is that the decorated code does not change and does not know.*
That is also its cost — behaviour becomes invisible where it is used. It is the right trade when the added
behaviour is genuinely orthogonal (caching, logging, retries) and the wrong one when it is part of what the
operation means. Worth noticing that the DI container is what makes this practical: `Decorate` swaps the
implementation without touching a single call site.

---

### 2025-08-28 — Hand-rolled CQRS instead of MediatR

**Context.** Commands and queries needed dispatching to handlers. MediatR is the default answer in .NET.

**Decision.** Custom `ICommand<T>`/`IQuery<T>` markers with `CommandDispatcher`/`QueryDispatcher` resolving
handlers from `IServiceProvider`. ADR-001. Commit `05656ed`.

**Rejected.** MediatR — mature, and its pipeline behaviours give validation, logging, and transactions for
free. Rejected to avoid a third-party dependency for roughly 40 lines of code, and because writing the
dispatcher makes the mechanism legible rather than magic. (MediatR moved to a commercial licence at v12, so
this aged well.)

**Cost.** No pipeline, so cross-cutting concerns need handler decorators; and handlers must be registered
manually — the problem [ADR-008](#2026-07-26--auto-register-handlers-instead-of-listing-them) later fixed.

**Takeaway.** *"Build it yourself" and "use the library" is a trade between control and unpaid maintenance —
and for a learning project the calculus differs from a commercial one.* Writing the dispatcher is ~40 lines and
teaches how mediator dispatch actually works; you then understand MediatR properly if you adopt it later.
Reaching for the library first would have made this the one part of the architecture that stayed opaque.

---

### Template for new entries

```md
### YYYY-MM-DD — <what was decided, as a statement>

**Context.** What forced the decision. Reference real files.

**Decision.** What was chosen. Link the ADR or roadmap ID if one exists.

**Rejected.** The alternatives, stated fairly — what would someone competent choosing them be optimising for?

**Cost.** What this makes harder. If nothing, you have not finished thinking.

**Takeaway.** *The generalisable lesson*, in one or two sentences — the part that transfers to other projects.
This is the line you will actually re-read.
```

Then add it to the [concept index](#concept-index).
