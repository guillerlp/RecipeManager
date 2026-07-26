# Spec: <Feature name>

> Copy this file to `docs/specs/<NNN>-<kebab-name>.md` and fill it in **before** writing code.
> Delete the guidance blockquotes as you go. Anything you cannot answer becomes a question for the user, not an
> assumption. Owner: `00-leader`.

| | |
| --- | --- |
| **ID** | `<NNN>` |
| **Status** | draft / approved / in progress / shipped / abandoned |
| **Author** | `<agent or user>` |
| **Created** | `<YYYY-MM-DD>` |
| **Branch** | `feat/<kebab-name>` |

---

## 1. Context

> Why this exists. 2–4 sentences. What can the user not do today, and what breaks or is painful because of it.

## 2. Goal

> One sentence. If it needs two, it is probably two features.

## 3. In scope

> Concrete, checkable deliverables.

- [ ]
- [ ]

## 4. Out of scope

> Explicit non-goals. This section prevents scope creep more than any other.

-
-

## 5. Open questions

> Blocking questions for the user, each with a recommended default so work is not stalled.
> Delete this section once everything is answered, moving the answers into the sections below.

| # | Question | Recommended default | Answer |
| --- | --- | --- | --- |
| 1 | | | |

## 6. Domain impact

> See [../domain-model.md](../domain-model.md). Be exact — this section drives the migration.

- **New/changed entities:** none | `<Entity>` — `<what changes>`
- **New/changed properties on `Recipe`:** none | `<name>: <type>` — nullable? default for existing rows?
- **New/changed invariants:** none | `<rule>` → new `RecipeErrors.<Name>()` with code `<422/404/…>` and field `<camelCaseName>`
- **New/changed shape validation:** none | `<field>` — `<rule in RecipeValidationRules>`
- **Migration required:** no | yes — destructive? how are existing rows handled?
- **Known limitations touched:** none | items `<n, n>` from [known limitations](../domain-model.md#known-limitations)

## 7. API impact

- **New/changed endpoints:**

  | Verb | Route | Request body | Success | Failures |
  | --- | --- | --- | --- | --- |
  | | | | | |

- **`RecipeDto` / `UpdateRecipeDto` changes:** none | `<field>: <type>` added/removed/retyped
- **Breaking for the client?** no | yes — `08-api-contract` must ship the TS change in the same PR
- **Cache impact:** none | new key `<name>` | which keys does each write invalidate?

## 8. Frontend impact

- **New routes:** none | `<path>` — added to `App.tsx`?
- **New/changed components:** `<Foo>` in `components/<bucket>/`
- **New/changed hooks:** `<useFoo>` — query or mutation? which `queryKey` does it invalidate?
- **States to design:** loading / error / empty / populated — describe each
- **Design tokens needed:** existing only | new token `<name>` (must be added to both `light.css` and `dark.css`)

## 9. Architecture impact

> `01-architect` fills this in, or writes "no architectural impact".

- **ADR required:** no | yes → ADR-`<NNN>` in [../architecture.md](../architecture.md)
- **New dependency:** none | `<package>@<version>` — why, and what it replaces
- **Layer/dependency changes:** none | `<description>`
- **New DI registrations:** `<handler/service>` in `ServiceInitializer.<method>`

### Alternatives considered

> Required, not optional — see [../learning-mode.md](../learning-mode.md). A spec that records only the chosen
> approach cannot be re-evaluated later and teaches nothing. Strawmen do not count: state what a competent
> engineer choosing the other option would be optimising for.

| Option | Optimises for | Why not chosen here |
| --- | --- | --- |
| | | |

- **Pattern applied:** `<name it, so it can be looked up>` — existing example in this repo: `<file>`
- **What this makes harder:** `<every choice costs something; name it>`

## 10. Security impact

> `05-security-reviewer` reviews this section. See the `SEC-01`–`SEC-12` standing gaps in
> [../agents/05-security-reviewer.md](../agents/05-security-reviewer.md) and
> [../known-issues.md](../known-issues.md).

- **New user-controlled input:** none | `<fields>` — validated where?
- **User content rendered in the SPA:** no | yes — how is it escaped?
- **File upload:** no | yes → **`01-architect` + `05-security-reviewer` sign-off required before coding**
- **Auth/ownership implications:** the app has no authentication or ownership today — state what this feature
  assumes about that
- **Config/secrets touched:** none | `<key>` — sourced from user-secrets / env var
- **Standing gaps affected:** none | worsens/improves `<SEC-nn>`

## 11. Acceptance criteria

> Testable statements. `06-qa-tester` turns each of these into a test. "Works correctly" is not a criterion.

- [ ] Given `<state>`, when `<action>`, then `<observable outcome>`.
- [ ] Given `<state>`, when `<invalid action>`, then HTTP `<code>` with `field: "<name>"`.
- [ ] Edge case: `<from the catalogue in 06-qa-tester.md>`

## 12. Test plan

- **Domain unit tests:** `<invariants to cover>`
- **Handler unit tests:** success / not-found / validation failure / cancellation / repository interaction
- **Integration tests:** `<endpoint>` — status code + database state
- **Not covered, and why:** `<e.g. Npgsql-specific behaviour — EF InMemory cannot reproduce it>`
- **Manual verification:** `<steps, including against a real PostgreSQL if relevant>`

## 13. Agents involved

| Step | Agent | Deliverable |
| --- | --- | --- |
| 1 | `00-leader` | this spec |
| 2 | `01-architect` | ADR or "no impact" |
| 3 | `02-senior-csharp` | domain + application + infrastructure + api + backend tests |
| 4 | `08-api-contract` | TS types + service + contract delta |
| 5 | `07-ux-ui` | screen spec |
| 6 | `03-senior-react` | components + hooks + route |
| 7 | `06-qa-tester` | tests + coverage statement |
| 8 | `04-code-reviewer` | review |
| 9 | `05-security-reviewer` | security review (if triggered) |

> Delete rows that do not apply, and say why in section 4.

## 14. Assumptions made

> Everything decided without the user answering. Each one is a risk if wrong.

-

## 15. Follow-ups

> Deliberately out of scope. Anything that outlives this spec goes into
> [../known-issues.md](../known-issues.md) with its own ID — do not leave `TODO(...)` markers behind.

-

## 16. Known issues and roadmap items touched

> IDs from [../known-issues.md](../known-issues.md) (defects) and [../roadmap.md](../roadmap.md) (planned work)
> that this fixes, worsens, or depends on. Entries that get closed must be **deleted from those files in the
> same PR**.

- Fixes:
- Depends on:
- On the [deploy gate](../roadmap.md#deploy-gate)? yes | no
