# Spec: Contract drift — hand fix, then generated TypeScript types (BUG-01–05, R-09)

| | |
| --- | --- |
| **ID** | `007` |
| **Status** | in progress — implemented, awaiting a green CI run on PR B |
| **Author** | `00-leader`, owned by `08-api-contract` with `01-architect` |
| **Created** | 2026-09-19 |
| **Branch** | PR A `fix/recipe-contract-drift` · PR B `feat/openapi-generated-types` |

---

## 1. Context

*(Written before implementation. The present tense describes the state this item started from.)*

The TypeScript `Recipe` in `recipe-manager-frontend/src/types/recipe.ts` has drifted from `RecipeDto` in five
verifiable ways (`BUG-01`–`BUG-05`). It declares `id: number` against a `Guid`, it is missing `servings` and
`instructions`, it declares an `image?` the API has never had, and `updateRecipe` expects a body from a 204. None
of this has failed, because the only screen passes `recipe.id` to a React `key`, which stringifies anything.
Nothing detects this kind of drift. `tsc` proves the TS code agrees with the TS types, never that the TS types
agree with the API.

`DEC-05` settled that the fix is generated types, *after* the defects are fixed by hand. This spec covers both,
as two PRs.

## 2. Goal

The TS contract matches `RecipeDto` today (PR A). Any future divergence between the C# DTOs and the TS types
fails CI (PR B).

## 3. In scope

### PR A — hand fix (`bugfix-workflow`)

- [x] `recipe-manager-frontend/src/types/recipe.ts`: the correct shape from
      [../agents/08-api-contract.md](../agents/08-api-contract.md#correct-shape). That means `id: string`, plus
      `servings` and `instructions`, with `image?` removed. It also adds `CreateRecipeRequest` and
      `UpdateRecipeRequest`, both `Omit<Recipe, 'id'>`.
- [x] `recipe-manager-frontend/src/services/recipeService.ts`: every id becomes `string`.
      `createRecipe(recipe: CreateRecipeRequest)`. `updateRecipe(id: string, recipe: UpdateRecipeRequest)`
      returns `Promise<AxiosResponse<void>>`. The current `Partial<…>` is also wrong, because `PUT` binds an
      `UpdateRecipeDto` whose seven members are all required. That is folded into `BUG-05`, since it is the same
      method on the same seam.
- [x] `RecipeCard.tsx`: always renders the placeholder (`image` no longer exists).
- [x] `RecipeList.test.tsx`: fixtures use string ids and the new fields.
- [x] Docs: see section 16.

### PR B — generation and the drift gate (`feature-workflow`, ADR-019)

- [x] **Swashbuckle config** (`ServiceInitializer.RegisterSwagger`):
  - Mark non-nullable members as `required`, so the generated TS has no spurious `?`. This covers reference
    types via `SupportNonNullableReferenceTypes()` and `NonNullableReferenceTypesAsRequired()`, and value types
    too (`Guid`, `int`). If Swashbuckle 10 does not mark value types required, add one small schema filter that
    marks every non-nullable property required, instead of annotating each DTO.
  - Remove `CustomSchemaIds(type => type.ToString())`, so schemas are named `RecipeDto` rather than
    `RecipeManager.Application.DTO.Recipes.RecipeDto`.
- [x] **Snapshot test** `RecipeManager.IntegrationTests/OpenApiContractTests.cs`:
  - Boots a bare `WebApplicationFactory<Program>` in the `IntegrationTest` environment. It does not use
    `IntegrationTestBase` and has no container.
  - Resolves `ISwaggerProvider` from DI, serialises document `v1` to JSON, and compares it with the committed
    `RecipeManager/contracts/openapi.json`, with line endings normalised.
  - With `UPDATE_OPENAPI_SNAPSHOT=1` set, it rewrites the file instead of asserting.
  - It is a plain `[Fact]`, not `[SkippableFact]`. It needs no Docker, so it must never skip.
- [x] **Generator package** `RecipeManager/contracts/`:
  - A private `package.json` and its own `package-lock.json`, pinning `openapi-typescript` 7.13.0 and
    `typescript` 5.9.3.
  - One script, `gen`, which writes `../recipe-manager-frontend/src/types/generated/api.ts`.
- [x] **Frontend**:
  - Add the script `"gen:api": "npm --prefix ../contracts run gen"`.
  - Commit the generated `src/types/generated/api.ts`.
  - Add `src/types/generated` to Oxlint's `ignorePatterns`. It stays inside `tsc`'s scope on purpose, so TS 7
    still type-checks the generated file.
  - `recipe.ts` becomes aliases: `export type Recipe = components['schemas']['RecipeDto']`, with
    `UpdateRecipeRequest` and `CreateRecipeRequest` aliasing their own schemas.
- [x] **`.gitattributes`** at the repo root: `eol=lf` for `RecipeManager/contracts/openapi.json` and
      `…/src/types/generated/api.ts` only. `core.autocrlf=true` on Windows would otherwise check them out as
      CRLF, and they would permanently differ from the generator's LF output.
- [x] **CI** (`.github/workflows/ci.yml`, frontend job, after `npm ci`):
  - `npm ci --prefix ../contracts`
  - `npm run gen:api`
  - `git diff --exit-code -- src/types/generated`
  - `npm audit --audit-level=high --prefix ../contracts`

  The backend job is unchanged, because the snapshot test runs inside the existing `dotnet test`.
- [x] **Dependabot**: a second `npm` entry for `/RecipeManager/contracts`, grouping `openapi-typescript` with
      `typescript` and ignoring `typescript` majors (see section 9).
- [x] **Proof the gate works**, reported in the PR:
  - A throwaway commit adds a property to `RecipeDto` without regenerating anything. The snapshot test must go
    red in the backend job.
  - A second throwaway commit regenerates `openapi.json` only. The diff check must go red in the frontend job.
- [x] Docs: see section 16.

## 4. Out of scope

- **Typing routes, verbs, and status codes.** `recipeService` keeps axios, and its paths and response
  wrappers stay hand-typed. A renamed route is still not caught. This is recorded as `QUAL-04`.
- **`openapi-fetch` or any generated runtime client.** It was considered and rejected in section 9.
- **Replacing Swashbuckle with `Microsoft.AspNetCore.OpenApi`.** That is a separate decision with its own ADR if
  it ever happens.
- **An image field (`BUG-04` resolved the other way).** Images are designed in `R-12`.
- **`BUG-07`** (`{id}` without `:guid` on `GET`). It changes the OpenAPI document (the parameter gains
  `format: uuid`), so it is worth doing soon after PR B, when the diff will show the effect. It is not part of
  this item.
- **Error-shape types** (`ProblemDetails` / `ValidationProblemDetails`). The generator will emit whatever the
  document declares, and no client code consumes them yet.

## 5. Open questions

None open. The user answered all of these on 2026-09-19:

| # | Question | Answer |
| --- | --- | --- |
| 1 | Sequence the hand fix and the generation? | **Two PRs**: A fixes `BUG-01`–`05`, B adds generation. |
| 2 | `BUG-04`: add an image field or drop `image?`? | **Drop it from TS.** Images belong to `R-12`. |
| 3 | How is `openapi.json` produced and checked? | **Snapshot test** through `WebApplicationFactory`. |
| 4 | How does the SPA consume the types? | **Types only**, and axios stays. |
| 5 | `openapi-typescript` needs the TS 5 compiler API but the repo runs TS 7. What now? | **Give the generator a private TS 5.** See section 9 for how the chosen mechanism changed after a spike. |

## 6. Domain impact

None. No entity, invariant, or migration changes.

## 7. API impact

- **Endpoints:** none changed in behaviour.
- **OpenAPI document:** it changes in PR B, and deliberately so. Members become `required`, and schema ids
  shorten to their type name. Swagger UI shows `RecipeDto` instead of the fully qualified name.
- **`RecipeDto` / `UpdateRecipeDto`:** unchanged.
- **Breaking for the client?** PR A is breaking for `recipeService` signatures (`number` → `string` ids). The
  only caller today is `useRecipes`, which uses `getAllRecipes` and is unaffected.
- **Cache impact:** none.

### Contract delta (PR A)

```md
Added:    servings: number, instructions: string[]
Removed:  image
Retyped:  id: number → string
          updateRecipe: Promise<AxiosResponse<Recipe>> → Promise<AxiosResponse<void>>
          updateRecipe body: Partial<Omit<Recipe,'id'>> → UpdateRecipeRequest (all fields required)
Routes:   unchanged
Breaks:   RecipeCard (image fallback), RecipeList.test.tsx fixtures
```

## 8. Frontend impact

- **New routes:** none.
- **Changed components:** `RecipeCard` renders the placeholder unconditionally.
- **New/changed hooks:** none.
- **States to design:** none.
- **Design tokens:** none.

## 9. Architecture impact

- **ADR required:** yes. **ADR-019**, generated TypeScript types from a committed OpenAPI snapshot, goes in
  [../architecture.md](../architecture.md).
- **New dependencies:** `openapi-typescript` 7.13.0 and `typescript` 5.9.3, both dev-only, isolated in
  `RecipeManager/contracts/`. No runtime dependency is added.
- **Layer/dependency changes:** none. `RecipeManager.IntegrationTests` already references the Api project and
  `Microsoft.AspNetCore.Mvc.Testing`.
- **New DI registrations:** none. At most one Swashbuckle schema filter, registered inside `RegisterSwagger`.

### The drift gate, as two links

```
C# DTOs ──(snapshot test, backend job)──▶ contracts/openapi.json ──(openapi-typescript, frontend job)──▶ src/types/generated/api.ts
```

Committing the intermediate JSON lets each CI job check only its own link, with no artifacts passed between
jobs. It also makes every contract change visible in review as a readable JSON diff.

### Why the generator lives in its own package

ADR-016 moved the frontend to TypeScript 7, the native Go compiler. TS 7 ships **no JavaScript compiler API**:
`node_modules/typescript/lib` contains only a `tsc.js` launcher. Every mainstream OpenAPI → TS generator uses
that API as a code printer (`ts.factory`), not to type-check anything. This was verified on 2026-09-19:

| Tool | TS requirement |
| --- | --- |
| `openapi-typescript` 7.13.0 | peer `typescript ^5.x` |
| `@hey-api/openapi-ts` 0.99.0 | peer `>=5.5.3 \|\| >=6.0.0` |
| `swagger-typescript-api` 13.13.0 | dependency `typescript ^6.0.3` |

**Spike 1, rejected: an `npm overrides` entry giving `openapi-typescript` a nested TS 5.** The result was
`ERESOLVE`. npm satisfies a **peer** dependency from the parent's tree, not the dependent's. An override
rewrites the peer *range* to `5.9.3`, but npm still has to satisfy that range with the root `typescript@7.0.2`,
so nothing is nested. The only way around it is `--legacy-peer-deps`, which disables peer resolution for the
whole project and would hide real conflicts such as the `vite` / `@vitejs/plugin-react` one Dependabot already
hit.

**Spike 2, chosen: an isolated package.** `RecipeManager/contracts/package.json` with `openapi-typescript`
7.13.0 and `typescript` 5.9.3 installs cleanly, audits to 0 vulnerabilities, and produces the expected output:
`id: string` with `/** Format: uuid */`, and required members without `?`. It also confirmed why the `required`
fix matters: a member not listed in `required` came out as `notRequired?: string | null`.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Build-time generation (`Microsoft.Extensions.ApiDescription.Server`) | The documented, conventional .NET route. The JSON regenerates on every build with no manual step. | It boots the host during `dotnet build`. `Program.Main` throws without a connection string and migrates before `Run()`, so `Program` would need a special case for the generator. It would also slow every build. |
| Swashbuckle CLI (`swagger tofile`) as a local tool | An explicit, scriptable step | Same host-boot problem, plus a `.config/dotnet-tools.json` outside Central Package Management that has to be version-matched to Swashbuckle by hand. |
| Replace Swashbuckle with `Microsoft.AspNetCore.OpenApi` | The .NET 10 default, OpenAPI 3.1, native build-time documents | It is a separate decision: Swashbuckle removal, a Swagger UI replacement, and its own ADR. Folding it into R-09 would double the review. |
| `openapi-fetch` (a generated, typed runtime client) | Catching route, verb, and status-code drift as well as shapes | It rewrites `recipeService`, `useRecipes`, and the test mocks, adds a runtime dependency, and drops axios. That is far beyond the roadmap item. The residual is recorded as `QUAL-04`. |
| NSwag on the .NET side | No Node or TS coupling at all | Frontend types would be produced by a backend tool, with a tool manifest outside CPM and NSwag's own TS conventions. It remains the fallback if the isolated package ever stops working. |
| **Chosen:** a snapshot test for C# → JSON, and an isolated `openapi-typescript` package for JSON → TS, each checked by its own CI job | Reusing the existing test and CI infrastructure, with the tool the roadmap named | See the cost below. |

- **Pattern applied:** *golden master / snapshot testing* for the document, and *generated code checked for
  staleness in CI* ("commit generated output and diff it") for the types. The closest existing example is the
  `--locked-mode` restore in `ci.yml`, which likewise fails when a committed artifact disagrees with what the
  tool would produce.
- **What this makes harder:**
  - Changing a DTO becomes a three-step ritual: edit the C#, run
    `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --filter OpenApiContractTests`, then run `npm run gen:api`. It is
    documented in the README, and CI names the failing step, but it is friction.
  - The repo now has two TypeScript versions and a second npm lockfile to keep patched.
  - `contracts/` must stay on TS 5 until `openapi-typescript` supports TS 7. Dependabot's `ignore` on
    `typescript` majors there is load-bearing. Without it, a PR bumping it to 7 would break generation.
  - A test that can also write a file is an unusual idiom and needs a clear comment.

## 10. Security impact

- **New user-controlled input:** none.
- **User content rendered in the SPA:** no change.
- **File upload:** no.
- **Auth/ownership implications:** none.
- **Config/secrets touched:** none. `UPDATE_OPENAPI_SNAPSHOT` is a local developer switch and is never set in
  CI. If it were, the test would pass by rewriting its own expectation, so the CI step must not set it.
- **Supply chain:** there are two new dev-only packages, pinned exactly and covered by their own lockfile,
  `npm audit`, and Dependabot. Neither ships to the browser.
- **Standing gaps affected:** none.

## 11. Acceptance criteria

PR A:

- [x] `recipe.ts` matches `RecipeDto` field for field, with no `image` and no optional markers.
- [x] `recipeService.updateRecipe` returns `Promise<AxiosResponse<void>>` and requires every body field.
- [x] `npm run typecheck`, `npm run lint`, `npm test` (40 tests), and `npm run build` are all green.

PR B:

- [x] The generated `RecipeDto`, `UpdateRecipeDto`, and `CreateRecipeCommand` schemas contain **no** optional
      (`?`) properties, and `id` is `string`.
- [x] Given a property added to `RecipeDto` without regenerating, when `dotnet test` runs, then
      `OpenApiContractTests` fails with a message that names the regeneration command.
- [x] Given a regenerated `openapi.json` but a stale `api.ts`, when the frontend CI job runs, then the diff step
      fails.
- [ ] Given no contract change, both checks pass on Windows (`core.autocrlf=true`) and on the Linux runner.
      **Not fully evidenced — noted, not ticked.** What was actually observed: the frontend check
      (`Contract types are current`) passes locally on the author's Windows machine, and `.gitattributes`'
      `eol=lf` is what keeps that comparison meaningful across a `core.autocrlf=true` checkout. The backend
      check (`OpenApiContractTests`) does not run on that machine at all — Smart App Control (`INFRA-06`) fails
      it with `FileLoadException` before it can compare anything, which is not the same as passing. Both checks
      are evidenced together only on the Linux runner, via CI run 35438379053 (85 unit + 22 integration passed,
      0 skipped).
- [x] The snapshot test passes on a machine **without** Docker. It does not skip. **Evidenced by design, not by
      a local no-Docker run:** `OpenApiContractTests` never uses `IntegrationTestBase` or the PostgreSQL
      container fixture (confirmed by reading `RecipeManager.IntegrationTests/Contracts/OpenApiContractTests.cs`),
      so nothing in it can depend on Docker being present. CI run 35438379053 shows 0 skipped, consistent with
      that — not with having actually been run on a Docker-less machine.
- [x] `recipe.ts` contains only aliases over the generated types, and no hand-written fields.

## 12. Test plan

- **`OpenApiContractTests`** (4 tests, corrected from the 1 originally planned — see "Deviations during
  implementation" below): a 3-case `[Theory]`, `RecipeSchemas_ShouldMarkEveryPropertyRequired`, over
  `RecipeDto`/`UpdateRecipeDto`/`CreateRecipeCommand`, plus the 1 snapshot `[Fact]`,
  `OpenApiDocument_ShouldMatchCommittedSnapshot`. The backend count becomes **107** (85 unit + 22 integration),
  not the 104 this spec originally planned. On a machine without Docker, the 18 Testcontainers-backed tests
  skip and all 4 `OpenApiContractTests` still run, because none of them touches PostgreSQL. On a machine where
  Smart App Control blocks `WebApplicationFactory` (`INFRA-06`), those 4 **fail** rather than skip — see the
  received-file mechanism in ADR-019 and the companion decisions-log entry.
- **Frontend:** the existing 40 Vitest tests, with fixtures updated in PR A. No new frontend tests, because
  types are verified by `tsc`, not by Vitest.
- **Gate proof:** the two throwaway commits in section 3, run on a throwaway draft PR (#54, closed and its
  branch deleted after the runs below were captured), not on PR B itself.

  | Milestone | Change | Backend (`OpenApiContractTests`) | Frontend (`Contract types are current`) | CI run |
  | --- | --- | --- | --- | --- |
  | M1 | `RecipeDto` gains a defaulted `Rating` property; nothing regenerated | **failed** — snapshot mismatch | **passed** — `api.ts` still matches the old snapshot it was generated from | 35438913390 |
  | M2 | `openapi.json` regenerated from M1's uploaded `openapi-received` artifact; `api.ts` left stale | **passed** — snapshot now matches the document | **failed** — `git status --porcelain` on `src/types/generated` showed `+ rating: number;` | 35438988522 |

  Each half of the gate was shown to fail on its own defect and pass once that defect alone was fixed, proving
  the two links are independent: a DTO change with no regeneration is caught by the backend, and a
  half-regenerated pair (snapshot updated, generated types not) is caught by the frontend.
- **Not covered, and why:**
  - Route, verb, and status-code drift (`QUAL-04`).
  - Whether the *runtime* JSON matches the document. Swashbuckle describes what `System.Text.Json` will emit,
    and a custom converter could make them disagree. None exists today apart from `JsonStringEnumConverter`,
    which Swashbuckle understands.
- **Manual verification:** `dotnet run --project RecipeManager.Api --launch-profile https`, then check that
  `/swagger` shows `RecipeDto` with required members. Then `npm run dev`, and check that the recipe list still
  renders.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `08-api-contract` | PR A types and service, contract delta; PR B aliases |
| 3 | `03-senior-react` | PR A `RecipeCard` and test fixtures |
| 4 | `01-architect` | ADR-019, generator isolation, CI and Dependabot changes |
| 5 | `02-senior-csharp` | Swashbuckle config, snapshot test |
| 6 | `06-qa-tester` | gate proof (the two throwaway commits) |
| 7 | `04-code-reviewer` | review of each PR |

`05-security-reviewer` is not triggered: no user input, config secret, CORS, or auth surface changes. The
supply-chain note is in section 10. `07-ux-ui` is not involved: there is no new screen.

## 14. Assumptions made

- `WebApplicationFactory<Program>` in the `IntegrationTest` environment builds without a `DbContext`.
  `Program.Main` skips `RegisterDbContext` and `MigrateDatabase` there, and document generation reads
  `ApiExplorer` metadata without constructing a controller, so the repository is never resolved. Scope
  validation on build is on only in `Development`. **To be verified first in PR B.** **Held** — the only
  evidence for this is CI run 35438379053 (Linux runner): `OpenApiContractTests` ran and passed with no
  database configured, and the full suite reported 85 unit + 22 integration passed, 0 skipped. Not independently
  verified on a Docker-backed local machine.
- `ISwaggerProvider.GetSwagger("v1")` serialised as OpenAPI 3.0 JSON produces the same document the Swagger
  middleware serves in Development.
- Swashbuckle 10's value-type `required` behaviour is unverified. Section 3 states the fallback.
- `openapi-typescript` output is deterministic for identical input. Spike 2 is consistent with that, and the
  gate-proof run will confirm it on Linux.

## Deviations during implementation

Two things changed from what this spec planned, both forced by discoveries made while building PR B, not by a
change of goal.

- **Smart App Control forced a second acceptance route.** `INFRA-06` (Settled) had already resolved Smart App
  Control on the owner's Windows machine by making CI the authority for the integration tests. That resolution
  turned out not to extend to a test whose job is to be *re-run on demand* by whoever changes a DTO —
  `OpenApiContractTests` cannot run at all under Smart App Control (`FileLoadException`, same as the rest of
  `RecipeManager.IntegrationTests`), so the author had no route to a local snapshot update. The fix, not
  originally planned: `OpenApiDocument_ShouldMatchCommittedSnapshot` writes `contracts/openapi.received.json`
  on any mismatch (the "received file" pattern) before failing, and the backend CI job uploads it as the
  `openapi-received` artifact on failure — giving a second acceptance route that needs no local test run at
  all. Recorded in ADR-019 and its own decisions-log entry (2026-09-19, "A regeneration workflow that only
  works where the tests run is not a workflow").
- **The required-members check became a 3-case theory, not folded into the snapshot fact.** Section 3 planned
  one snapshot test; verifying "every non-nullable member is required" turned out to need its own assertion
  per schema (`RecipeSchemas_ShouldMarkEveryPropertyRequired`, `[Theory]` over the three DTO schemas) so a
  failure names which schema regressed, rather than surfacing only as an opaque JSON diff in the snapshot
  test. This is why section 12's count is 107, not the 104 originally planned.

A real bug was also found and fixed during implementation, not a planned deviation: the first version of
`OpenApiDocument_ShouldMatchCommittedSnapshot` threw `DirectoryNotFoundException` writing the received file,
because `contracts/` does not exist on a bare checkout before the first snapshot is committed. Fixed by
creating the directory up front (commit `7ab09fa`).

## 15. Follow-ups

- `QUAL-04` (new): route, verb, and status-code drift in `recipeService` is still undetected. A typed
  path layer or `openapi-fetch` would close it.
- `BUG-07`: add `:guid` to `GET {id}`. It is cheap after PR B, and the OpenAPI diff will show the effect.
- Remove the `contracts/` TS 5 pin once `openapi-typescript` supports TS 7, and consider folding the package back
  into the frontend at that point.

## 16. Known issues and roadmap items touched

- **PR A fixes:** `BUG-01`, `BUG-02`, `BUG-03`, `BUG-04` (resolved by removal), and `BUG-05`. Delete their
  entries in `known-issues.md`, and update the drift table in `agents/08-api-contract.md` and the frontend
  section of `domain-model.md`.
- **PR B fixes:** `R-09`. Delete it from `roadmap.md`, record ADR-019, and update:
  - the `DEC-05` "Settled" row, marked as shipped;
  - `tech-stack.md` (the two new dev packages and the reason they are isolated);
  - the checklist in `08-api-contract.md` (the regeneration steps and "nothing detects drift" → the gate);
  - the README (the three-step ritual);
  - `CLAUDE.md` (test counts 103 → 107, and `contracts/` in the repository layout);
  - `feature-workflow.md` step 7 (the regeneration step).
- **PR B adds:** `QUAL-04` to `known-issues.md`.
- **Decisions log:** "TS 7 has no compiler API; codegen needs its own TS". Takeaway: a tool can depend on a
  compiler as a *library*, and that coupling is invisible until the compiler changes runtimes. Also "a peer
  dependency cannot be nested with overrides".
- **Depends on:** `R-04` / ADR-013 (CI, shipped), ADR-016 (TS 7, the cause of the isolation).
- **On the [deploy gate](../roadmap.md#deploy-gate)?** No.
