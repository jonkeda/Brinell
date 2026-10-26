# Frainet: the first slice

## Outcome

One journey of the Todo sample, TOD.02 "Create a todo", written as a Frainet spec, with two checks
that run without a model: the spec is complete, and the work covers it. When that holds, Todo's
other journeys follow, then Tomorrow.

## What Todo already has

| Layer (specs/01) | In Todo now | Gap |
| --- | --- | --- |
| Business intent | A line in the README | Not written per journey |
| Product spec | `journeys.md`: subtasks with ids | Fine |
| Rules and invariants | Inside subtasks ("at most 100 characters") | Not separate, no ids of their own |
| Non-functional | Scattered (TOD.10) | Not per journey |
| Acceptance | `TOD.02-create-a-todo.feature.md`, unit and UI tests with `Journey` traits | Fine |
| Traceability | `JourneyCoverage`: subtask to test, per tier | Rules not traced |

So Frainet's first job is the layers above the tests, and tracing rules, not only subtasks.

## Steps

| # | Step | Done when |
| --- | --- | --- |
| 1 | `Frainet/specs/todo/TOD.02.spec.md`: intent, rules (`TOD.02.R1`…), examples per rule, decision boundaries (what an agent may decide), links to the subtasks in `journeys.md` | The owner reads it and says it is what Todo means |
| 2 | The format written down: `Frainet/docs/spec-format.md`, only what step 1 used | One page |
| 3 | Spec check, no model: every rule has an id, an example and a subtask; no vague words from a short list ("fast", "should", "appropriate", "etc.") | A console or test project fails on a broken copy of the spec |
| 4 | Work check: every rule id reached by a test (a `Rule` trait or a scenario tag), every rule trait names a rule; reuse `JourneyCoverage`'s reading of traits and scenarios | Fails when a rule loses its test |
| 5 | Try it as the factory would: an agent gets only the spec, writes a rule's missing test; the checks judge it | One rule added dark, end to end |

Then the rest of Todo's journeys, one at a time, and only then a first module of Tomorrow.

## Open questions

- Where does the spec live, beside the sample or in Frainet? Proposal: in `Frainet/specs/todo/`
  for now, so Frainet can leave the repo whole; the sample keeps `journeys.md` as it is.
- Do rules replace subtasks? Proposal: no, a subtask is a behaviour a user sees, a rule is what
  must hold; a subtask lists its rules.
- A model-based check of the spec (ambiguity, missing edge cases)? Proposal: after step 5, once
  the exact checks show what they cannot catch.
- Code: a .NET tool in Bruinet's solution or its own? Proposal: its own `Frainet.slnx` under
  `Frainet/`, referencing nothing of Bruinet yet, so the move to its own repo stays a copy.
