# Callable phrases: reuse without a language

Status: discussion (to be taken up separately)
Date: 2026-09-26
Split from: [phrase-scopes-and-composition.md](../phrases/phrase-scopes-and-composition.md), section G

Callable phrases let a tester give a sequence of steps a name, and use it with parameters. Where
they are declared and who can see them follows the scope model in the scopes doc (page, file,
module, global).

## The need

Testers repeat themselves. Every Projects scenario starts "create a project, add a work order, log
8 hours". Today there are only two ways to avoid that:

- `## Background`: per file, no parameters, only at the start.
- a C# phrase: needs a developer.

Neither lets a **tester** say "this sequence has a name; use it here with these values".

## The shape

A named, parameterised block of steps, written in scenario language:

````markdown
## Phrase: a project {name} with {hours} unbilled hours

```gherkin
Given I am on the Projects page
When I create a project <name>
And I add a work order to <name>
And I log <hours> hours on the work order
Then I should be on the Project Detail page
```
````

Used like any phrase:

```gherkin
Given a project "Riverside" with 8 unbilled hours
```

It binds like a phrase, and **its inner steps are bound at bind time too**, with the placeholders
substituted, so a typo inside a callable phrase is caught before anything runs, just like
everywhere else.

Where it lives follows the scope model:

| Declared in | Visible to |
| --- | --- |
| the `.uat.md` file, under `## Phrase:` | that file |
| a `Scenarios/Projects/*.phrases.md` file | the Projects module |
| `Scenarios/*.phrases.md` at the root | everything |

## The danger, and where to draw the line

This is where test languages go wrong. Robot Framework started with keywords (exactly this) and
grew `IF`, `FOR`, `WHILE`, `TRY` and variables, until test suites became programs that nobody
could read and that nobody tested. FitNesse and Cucumber's step-calling-steps went the same way.
Once the scenario language has control flow, it needs a debugger, and testers become
programmers in a worse language than C#.

So the rule has to be **structural, not advisory:**

| Allowed in a callable phrase | Not allowed, ever |
| --- | --- |
| a fixed sequence of steps | `If`, `Else`, `While`, `For`, `Try` |
| parameters substituted into step text | variables assigned by steps, or return values |
| calling other phrases, including callable ones | recursion (a cycle is a bind-time error) |
| tables passed through as parameters | branching on what the screen shows |

What covers the cases people reach for control flow for:

- **"Do this for each of these values"**: that's a `Scenario Outline` with `### Examples`, which
  is data, not a loop.
- **"If the dialog appears, close it"**: that means the test doesn't know the app's state, which
  is a test-data problem (the scenario's `TestData`). Or it's a C# phrase that owns the
  uncertainty and names it: `I dismiss the dialog if it is shown`.
- **"Retry until it works"**: never in scenarios. Waiting is the controls' job (the AGENTS.md
  synchronisation rules).

The escape hatch is always **C#**: anything that needs a decision becomes a phrase class method
with a name that says what it decides. The markdown stays sequences.

Two more guardrails:

- **Depth limit** (say, 3). A callable that calls a callable that calls a callable is already
  hard to follow in a failure report.
- **Expanded in results.** The step trace must show the inner steps, indented under the call, so
  a failure points at the real step, not at "a project Riverside with 8 unbilled hours failed".

## Where it sits among the phrase layers

| Layer | Written by | Written in |
| --- | --- | --- |
| Control verbs, page phrases, subject phrases, module phrases | developers | C#: can contain logic |
| Callable phrases | **testers** | markdown: can't contain logic |

That split is the protection against a scenario language turning into a programming language.

## Implementation outline

File-local first, then module and global phrase files. Bind-time expansion, depth limit, cycle
check, expanded trace.

## Open questions

| Question | Notes |
| --- | --- |
| Should callable phrases be allowed to end on a different page than they start? | Yes, but they must declare it (the last step is an `I should be on`) so static page tracking stays possible. |
| Parameter types? | Strings only at first. Tables passed through unchanged. Typed parameters only when a real case needs them. |
| What happens to `## Background`? | It stays. It's a callable phrase with no name and no parameters that runs first. It could be re-implemented as one later. |
