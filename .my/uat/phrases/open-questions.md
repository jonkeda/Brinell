# Phrases: open questions

Status: **open**, none blocking. Date: 2026-10-03.
Left over from the phrases plan, whose phases 1-8 are built; its decisions are
[AD-019](../../../.docs/decisions/ad-019-phrases-have-scopes-and-namespaces.md) and the guide's
[Where Phrases Live](../../../docs/guides/uat-step-definitions.md#where-phrases-live). The plan is at
[027b0e77](https://github.com/exact-mobile-apps/test-tooling/blob/027b0e77/.my/uat/phrases/plan.md).
Decide each when a suite needs it; Construction's first UAT suite is the likely trigger.

| Question | Notes |
| --- | --- |
| Can a namespace phrase be used on any page, or only its own pages? | Namespace and page scope are independent axes. Built as "any page". |
| Callables on a page scope | Could a callable belong to a page, like a page phrase? Possible since static page tracking; Todo has not needed it. |
| Deprecated aliases | If a namespace renames a phrase, its users break. Should a phrase keep its old text as a deprecated alias for a while? |
| A naming rule per subject | Every backend phrase starts "the server", every stored-data phrase "the database", so the class can be read off the text. |
| A call with many parameters | Five `{x}` in a heading is hard to read; a `Field / Value` table would clash with a step's one table. |
| `## Background` as a callable | It keeps its own code. |
| Tooling | Not confirmed built: step completion grouped by namespace in the Presenter and the IDEs, and a "who uses this namespace?" check listing the files with `Using \| <name>`. The bind report naming the scope, and the `Using` errors and warnings (`UATB011`, `UATB012`), are built. |
| Which package owns the shared subjects? | [subjects-plan.md](subjects-plan.md) S3. |
