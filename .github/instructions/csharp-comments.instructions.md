---
applyTo: "**/*.cs"
---

# C# comment policy

Write down what the code cannot show; delete what it can. A comment restating what the
next line does costs maintenance and drifts from the code. Reasoning: [the documentation plan](https://github.com/exact-mobile-apps/test-tooling/blob/fe5d2b66/.my/documentation/plan.md).

## Keep

- `<summary>`, one or two lines, on **public** and **protected** members. This is the help
  file: IntelliSense for a test author who never opens the file.
- `<param>`, `<returns>`, `<exception>` where the name does not already say it.
- `<inheritdoc />` on overrides and interface implementations - free, and it cannot drift.
- A one-line `// why` with a pointer where a decision is genuinely surprising, e.g.
  `// R0: an action is never repeated; see AD-009`.
- `<remarks>` **only** when it records something that cannot be re-derived from the code:
  a control's measured per-platform route/evidence, or an app-side requirement (the
  `maui-control` convention). Not a retelling of the method body.

## Delete

- `<remarks>` that restates what the code does, on any member.
- Any XML doc on `private` and `internal` members - nothing consumes it.
- History and rationale in prose. That belongs in `.docs/` (decisions/contracts), where it
  is reviewed and corrected in one place.
- A pointer into `.my/`. Plans end and are deleted; a `// why` names an AD or a contract, or
  says its reason itself in one line.

## Where durable prose goes

- A decision (rules something out): `.docs/decisions/`. Record it **once**.
- A contract or invariant: `.docs/contracts/`, `.docs/invariants.md`.
- Work in flight (plans, analyses): `.my/`. Code does not link to it.
