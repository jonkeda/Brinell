# AD-017: Callable Phrases Are Sequences, Not Programs

A tester names a sequence of steps and calls it like a phrase, in scenario language:

```markdown
## Phrase: the synced list has todos

Given I am on the Todo List page
And the server also has todos
  <table>
When I tap Sync
Then I should be on the Todo List page
```

It lives in a scenario file (seen by that file) or in a `*.phrases.md` file in `Scenarios/` or a
folder under it (seen by the scenarios in that folder and below, AD-019). `## Phrase for each
row:` runs the body once per row of the call's table.

- **A sequence, never a program.** No `If`, `Else`, loop, `Try`, variable or return value in a
  callable, and none will be added. Placeholders are substituted; the call's table or doc string
  is passed on whole; a per-row callable repeats over rows the scenario writes, with no index,
  skip or break. Nesting stops at three, and a cycle is an error. Anything that decides is a C#
  phrase whose name says what it decides.
- **Expanded where it is called.** A call is expanded while the binder walks the scenario's
  steps, so its steps run in the caller's app and page, and every call is checked before
  anything runs. The phrases they can use are those seen where the callable is written
  (AD-019). A problem inside a call is reported at the call's line, or its row, with the body
  steps it came through.
- **Typed by where the values go.** A parameter's type is the type of the C# parameter its
  placeholder fills; an optional `Parameters` table declares one inference cannot see, and must
  agree with use. Values are read by `UatValues` as in AD-016.
- **No shadowing.** A callable and a C# phrase with the same text are ambiguous, as are two
  callables. A broken callable is marked and fails only its calls (AD-016's rule for phrases).

**Why:** testers repeat step sequences, and the only reuse was `## Background` (no parameters,
only first) or a developer writing C#. Robot Framework, FitNesse and Cucumber's steps-calling-steps
show where reuse in a test language goes once it gains control flow: suites become programs in a
worse language than C#, with no debugger and no tests. Keeping the markdown to sequences and the
decisions in C# keeps scenarios readable by the people who write them.

**Ruled out:** control flow and variables in scenario language; picking rows or cells from a
passed table; repeating one step per row without a callable (`<Name>` already means an Examples
column, and the step line would not say it repeats); inferring types from what calls write (a
callable's type would change when someone adds a call); innermost-scope-wins shadowing.

**Broken when:** a scenario file gains a construct that branches, loops or assigns; a callable's
call runs without its inner steps having been bound; a problem inside a call is reported only in
the phrase file; a callable silently overrides a C# phrase or another callable.
