# Spec: Adopt an `.editorconfig` so `EnforceCodeStyleInBuild` enforces something

| | |
| --- | --- |
| **ID** | `008` |
| **Status** | in progress, approved by the user 2026-09-19 |
| **Author** | `00-leader` + `01-architect`, with the user |
| **Created** | 2026-09-19 |
| **Branch** | `chore/editorconfig` |

---

## 1. Context

`RecipeManager/Directory.Build.props` sets `EnforceCodeStyleInBuild` (ADR-010), but no `.editorconfig` exists, so
every IDE rule sits at its default `suggestion` severity and the build enforces no style at all. The property looks
like a gate and is not one (`R-15`). With `TreatWarningsAsErrors` on, the first rule raised to `warning` becomes a
build error in every violating file at once, so adoption has to be one deliberate pass.

## 2. Goal

A root `.editorconfig` whose chosen rules fail the build, with the codebase brought into compliance in one
isolated, blame-ignored reformat commit.

## 3. In scope

- [ ] `/.editorconfig` at the git root, `root = true`:
  - `[*]`: `indent_style = space`, `insert_final_newline = true`, `trim_trailing_whitespace = true`.
  - `[*.md]`: `trim_trailing_whitespace = false` (two trailing spaces are a Markdown line break).
  - `[*.{ts,tsx,js,json,css,html}]`: `indent_size = 2` — guidance for editors only, see §4.
  - `[*.{csproj,props}]`: `indent_size = 2`, as they are today. `[*.cs]`: `indent_size = 4`.
  - `[*.cs]`, **enforced (`warning`)**:
    - `IDE0055` formatting, with `csharp_*` formatting options matching the existing code (Allman braces, the
      .NET defaults) so the reformat is minimal.
    - `IDE0005` unused `using` directives.
    - `IDE0161` via `csharp_style_namespace_declarations = file_scoped:warning`.
  - `[*.cs]`, **documented but not enforced (`suggestion`)**: `var` preference
    (`csharp_style_var_* = true:suggestion`), which the code already follows (218 `var` against ~33 explicit types).
  - `[**/Migrations/*.cs]`: `generated_code = true`. EF emits block-scoped namespaces; without this, every future
    `dotnet ef migrations add` would fail the build on `IDE0161`.
- [ ] `RecipeManager/Directory.Build.props`: `GenerateDocumentationFile = true` and `NoWarn` `CS1591`, each with a
      comment saying why. The compiler only reports `IDE0005` during a build when documentation generation is on,
      and turning it on raises `CS1591` ("missing XML comment") on every public member, which
      `TreatWarningsAsErrors` would make fatal.
- [ ] One-off reformat with `dotnet format`: 22 whitespace fixes in 7 files, 21 block-scoped files converted to
      file-scoped, and whatever unused usings `IDE0005` finds. **No other change in that commit.**
- [ ] `/.git-blame-ignore-revs` listing the reformat commit's SHA, plus the one-time
      `git config blame.ignoreRevsFile .git-blame-ignore-revs` documented in `README.md`. GitHub's blame view reads
      the file automatically.
- [ ] A deliberate negative test, run by hand and recorded in the PR: one mis-indented line, one unused `using`, and
      one block-scoped namespace each fail `dotnet build`, then get reverted.
- [ ] Docs: ADR-020 in `docs/architecture.md`, a `docs/decisions-log.md` entry, `docs/conventions.md` (the
      namespace rule becomes enforced and "convert only when touching" is superseded; style enforcement is noted),
      `docs/agents/04-code-reviewer.md` (the block-scoped-namespace line becomes compiler-enforced),
      `docs/tech-stack.md` if it lists build tooling, `CLAUDE.md` where it describes `Directory.Build.props`, and
      `R-15` deleted from `docs/roadmap.md`.

## 4. Out of scope

- **Enforcing frontend formatting.** `.editorconfig` only instructs editors; nothing enforces it for TS or CSS, and
  indentation there is mixed today (24 files use 2 spaces, 14 use 4). Recorded as `QUAL-05` rather than
  reformatting 14 files as a side effect.
- **`end_of_line`.** Git owns line endings (`.gitattributes` + `core.autocrlf`); a second authority would conflict.
- **`charset`.** 44 of 64 `.cs` files have a BOM and 20 do not. Pinning either way rewrites files for no benefit.
- **The modern-C# suggestions** (`IDE0028`/`0300`/`0305` collection expressions, `IDE0290` primary constructors,
  137 hits). They are .NET defaults, not this repo's style, and stay at their default `suggestion`, which the
  build ignores. `IDE0290` in particular conflicts with the `_camelCase` injected-field convention.
- **`CA` analyzers / `AnalysisLevel`.** A separate decision. Noted as a follow-up, since `CA2254` would enforce
  `QUAL-01` automatically.
- **A `dotnet format --verify-no-changes` CI step.** Redundant: `EnforceCodeStyleInBuild` makes the existing CI
  `dotnet build` fail on the same rules.

## 6. Domain impact

None. No entity, invariant, validation, or migration change. The reformat touches `Recipe.cs` and
`IRecipeRepository.cs` for namespaces and whitespace only.

## 7. API impact

None. No route, DTO, or status-code change, and `contracts/openapi.json` must not change (the contract tests
verify this).

## 8. Frontend impact

None beyond editor behaviour for new edits (2-space indentation).

## 9. Architecture impact

- **ADR required:** yes → ADR-020, which completes ADR-010's `EnforceCodeStyleInBuild` rather than superseding it.
- **New dependency:** none. `dotnet format` ships with the SDK.
- **Layer/dependency changes:** none.
- **New DI registrations:** none.

### Alternatives considered

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| Delete `EnforceCodeStyleInBuild`, rely on review | Zero friction, zero churn | The repo's stated lesson (ADR-010, decisions log 2026-08-04) is "turn *should* into *cannot*". A style rule held only by review is one nobody applies consistently: 21 files already drifted on namespaces. |
| Everything at `suggestion`, promote later | Lowest risk | `suggestion` enforces nothing in the build, which is exactly today's state with extra config. The measured cost of `warning` is small (7 + 21 files). |
| `dotnet format --verify-no-changes` as a CI step instead of in-build | Faster local builds | The failure arrives minutes later on a runner instead of in the local build, and local and CI would disagree. The in-build latch already exists. |
| Enforce `var` too | Uniformity | The code already complies, so a `warning` buys almost nothing and adds friction. Kept as a documented `suggestion` (user decision). |
| Pin `end_of_line` / `charset` | Byte-level consistency | Conflicts with git's line-ending handling, and rewrites 20 BOM-less files for no reader-visible gain. |

- **Pattern applied:** configuration as code / policy-as-code, a shared, versioned rule file enforced by the same
  toolchain that builds. Existing example in this repo: `Directory.Build.props` (ADR-010) and
  `Directory.Packages.props` (ADR-011).
- **What this makes harder:** a mis-indented line or a leftover `using` now fails the build mid-refactor, the same
  friction ADR-010 accepted for warnings. Generating XML documentation files adds a small build cost and a
  suppressed `CS1591` that must not be read as "we document public APIs". And `git blame` needs the ignore-revs
  config locally or it attributes 30-odd files to the reformat.

## 10. Security impact

- **New user-controlled input:** none. **Config/secrets touched:** none. **Standing gaps affected:** none.

## 11. Acceptance criteria

- [ ] Given the branch, when `dotnet build RecipeManager.sln` runs, then it succeeds with 0 warnings.
- [ ] Given a `.cs` file with a mis-indented line, when building, then `error IDE0055` fails the build.
- [ ] Given an unused `using`, when building, then `error IDE0005` fails the build.
- [ ] Given a block-scoped namespace in a non-migration file, when building, then `error IDE0161` fails the build.
- [ ] Given a newly generated EF migration (block-scoped), when building, then no style error is raised.
- [ ] `dotnet test RecipeManager.sln`: the same 107 tests pass, and `contracts/openapi.json` is unchanged.
- [ ] The reformat commit contains no non-whitespace/namespace/using change (reviewed with `git diff -w`).
- [ ] `git blame` with the ignore-revs config attributes reformatted lines to their previous commits.

## 12. Test plan

- **Unit/integration tests:** no new tests. The existing 107 are the regression net for "reformat changed no
  behaviour".
- **Enforcement:** the three negative builds in §11, run by hand and pasted into the PR. A gate that has never
  rejected anything is unproven (decisions log, 2026-08-04).
- **Migration exclusion:** generate a throwaway migration, build, then remove it.
- **Not covered, and why:** no automated test asserts the `.editorconfig` itself. The build *is* the test.

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR-020 + decisions-log entry |
| 3 | `02-senior-csharp` | `.editorconfig`, `Directory.Build.props`, reformat, negative tests |
| 4 | `04-code-reviewer` | review, with the reformat commit read under `git diff -w` |

`08-api-contract`, `03-senior-react`, `07-ux-ui`, `06-qa-tester` and `05-security-reviewer` do not apply: no contract,
screen, test-strategy, or security surface changes.

## 14. Assumptions made

- The existing brace/spacing style is the .NET default, so default `csharp_*` formatting options produce only the
  22 measured fixes. Verified with `dotnet format whitespace --verify-no-changes`.
- `IDE0005`'s count is unknown until `GenerateDocumentationFile` is on. If it turns out large, it still lands in the
  same reformat commit.

## 15. Follow-ups

- `QUAL-05` (new): frontend indentation is mixed and nothing enforces formatting for TS/CSS.
- Consider `AnalysisLevel` / selected `CA` rules as their own decision. `CA2254` would turn `QUAL-01` into a build
  error.

## 16. Known issues and roadmap items touched

- Fixes: `R-15` (roadmap, deleted on completion).
- Adds: `QUAL-05`.
- Depends on: ADR-010.
- On the [deploy gate](../roadmap.md#deploy-gate)? no
