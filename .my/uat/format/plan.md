# Feature files are Markdown: Gherkin in a fence, prose everywhere else

Status: **plan**, Q1-Q4 decided, not started. Date: 2026-09-28.

## 1. Goal

A feature or step file is a Markdown document first. Two rules:

1. **Gherkin is always inside a ` ```gherkin ` block.** Steps, their tables and their doc strings
   are only ever written in a gherkin fence. Text outside one is prose, even when it reads like a
   step (Q3).
2. **Prose may be anything Markdown has, anywhere.** This covers paragraphs, lists, block quotes,
   images, links, emphasis, HTML blocks, thematic breaks, code blocks in other languages, and
   headings the engine doesn't read. It can go before, between and after the parts the engine
   reads, including between two gherkin blocks of one scenario.

The file then reads well on GitHub and in the preview, can explain a scenario where it matters,
and the engine knows exactly which lines are Gherkin, rather than guessing.

## 2. What holds today

| Fact | Where |
| --- | --- |
| Fences are optional and invisible. `SplitLines` drops every line starting with ` ``` ` or `~~~`, **any language**, before parsing, so bare steps parse exactly like fenced ones. Worse, the content of a ` ```json ` or ` ```text ` block is read as steps. | `BddMarkdownParser.SplitLines` |
| Prose is refused almost everywhere: `UAT021` at the top level, `UAT011` ("Expected a step") inside a section, `UAT017` between a Metadata, Data or Examples heading and its table. `UAT022` refuses any heading it doesn't know. | `BddMarkdownParser.Parse`, `ParseStepBlock`, `ParseRequiredTable` |
| A `#` comment inside a fence is read as a level-1 heading, and ends the scenario. | `ParseStepBlock` → `TryParseHeading` |
| Of 32 feature and step files, **16 write their steps bare**, all in the framework's own test suites (Presenter, MAUI, Html, Blazor, WPF, WinForms, Stride). The Todo sample fences everything. No file has prose yet. | counted on `move` at fb07253 |
| About 30 C# test files hold feature text inline: Brinell.Bdd.Tests, Brinell.Uat.Tests, the Preview, LanguageServer, DataValidator and Presenter tests. Many write steps bare. | |
| The docs show the format: the template guide and the step definitions guide, the skills, and `copilot-instructions.md`. | `docs/guides/uat-template-guide.md`, `uat-step-definitions.md`, `.github/skills/*` |
| The preview (Markdig) and the language server's `BddDocumentStructure` **already** treat only gherkin fences as steps and everything else as Markdown. They disagree with the engine today; after this plan they agree. | `BddPreviewWriter`, `BddDocumentStructure` |

## 3. The format

**What the engine reads** (the structure). Everything else is prose, and is ignored.

| Part | Form |
| --- | --- |
| Title | `# Feature: <name>` (a feature file) or `# Steps: <name>` (a step file). Exactly one level-1 heading of this kind. |
| Metadata | `## Metadata`, then its first table (`Field`, `Value`). |
| Background | `## Background`, then one or more gherkin blocks. |
| Scenario | tag lines directly above `## Scenario: <name>`, then one or more gherkin blocks. |
| Outline | tag lines directly above `## Scenario Outline: <name>`, then one or more gherkin blocks, then `### Examples` and its first table (Q2). |
| Data | `## Data: <name>`, then its first table. |
| Callable step | `## Step: <phrase>` or `## Step for each row: <phrase>`, an optional `Parameters` table (the first table before its first gherkin block), then one or more gherkin blocks. |

**A gherkin block** is a fenced code block whose info string is `gherkin`, in any case, with
` ``` ` or `~~~` fences. Inside it, each line is one of:
- a step, which may be followed by its table or doc string;
- a `#` comment;
- a blank line.

Anything else inside is an error, as today (`UAT011`). The blocks of one section are read in
order, as one list of steps; `And` continues across blocks.

**Prose** is everything else, in any Markdown form. That includes a level-2 heading the engine
doesn't know (`## Notes`), which is a prose section running to the next level-2 heading. It also
includes any level-3-or-lower heading except `### Examples` in an outline. Each part's table is the
first table of its section, so a table after it is prose.

**Bare steps are prose (Q3).** A line that reads like a step but is outside a gherkin block is
prose, in every section, and is not checked. A file in the old form therefore loses its steps, and
the existing "no steps" errors say so: `UAT004` (Background), `UAT007` (Scenario), `UAT014`
(Outline) and `UAT026` (Step). When such a section holds step-like prose, the error adds a hint:
"steps go in a ` ```gherkin ` block". The fix wraps that prose in one block:
`brinell-data validate --fix`, and a quick fix on those codes in the editor. It is how the current
files are migrated (phase 3).

**Errors the new form adds:**

| Code | When | Fix |
| --- | --- | --- |
| `UAT031` (warning) | A heading looks like a section but isn't one: its text starts with `Scenario`, `Background`, `Step`, `Examples`, `Data` or `Feature`, but it is misspelled, at the wrong level, or misses its colon. It would otherwise be silently read as prose. | none |

**What changes for existing codes:**
- `UAT021` (content outside a section) goes: prose is allowed.
- `UAT022` (unsupported heading) is kept only for a second level-1 heading.
- `UAT017` stops requiring the table to follow its heading directly.
- `UAT020` (tags directly above a scenario heading) stays.
- `UAT003` means "no section before `# Feature:`"; prose before it is allowed.

## 4. How the engine reads it

**Recommended: Markdig in `Brinell.Bdd`.** The parser walks Markdig's block tree:
- headings pick the sections;
- `FencedCodeBlock` with info `gherkin` gives the step lines, read by today's step reader;
- `Table` gives the part's table;
- every other block is prose, skipped.

Line numbers come from Markdig's source positions, as the preview already does.

- **Why:** CommonMark is subtle. A line starting with `Given` inside a list item, a block quote or
  an indented code block is prose, not a step. A table needs its separator row. Fences nest in
  lists. The preview already reads the file through Markdig, so the engine and the preview would
  split a file the same way, by construction.
- **Cost:** `Brinell.Bdd` gains a package dependency (Markdig, MIT, netstandard2.0, no
  dependencies of its own). See Q1.

The step reader itself (keywords, `And`/`But`, tables and doc strings under a step, `<table>`
passes in a callable) stays; it is fed the gherkin blocks' lines instead of the whole file.

## 5. Phases

Each step is one commit, with its tests green.

| # | Step | Done when |
| --- | --- | --- |
| 1 | Write **AD-023: a feature file is Markdown; Gherkin is fenced**. It covers the format of section 3, and supersedes the parts of the template guide that describe the old form. | The AD is reviewed. |
| 2 | **Parser**: read the block structure (section 4); gherkin blocks only; `#` comments in a block; several blocks per section; prose everywhere, step-like lines outside a block included; `UAT031`; the hint on the "no steps" errors; the code changes above. Line numbers unchanged. | `Brinell.Bdd.Tests` has a case for each prose form (paragraph, ordered and unordered list, quote, image, link, HTML block, thematic break, setext heading, indented code, a ` ```json ` block with a step-like line in it, `## Notes`, `### Why`, prose between two gherkin blocks, prose between a heading and its table). Each parses to the same document as the file without the prose. A scenario of bare steps gives `UAT007` with the hint; a bare step-like paragraph next to a gherkin block is prose. |
| 3 | **Fix and migrate**: `brinell-data validate --fix` wraps each run of bare step lines (with their tables and doc strings) in one gherkin block; the language server offers the same as a quick fix on the "no steps" errors that carry the hint. Run `--fix` over the 16 files, and fence the inline feature text of the C# tests (with a test helper where many share a shape). | Every test suite is green; every UAT suite lists the same number of scenarios as before (the Todo suites 43/35/4/3); a search finds no bare step outside a fence in any `*.feature.md` or `*.steps.md`. |
| 4 | **Tools agree**: the preview, `BddDocumentStructure`, completion, the explorer and the MCP `validate_scenario` show the new diagnostics. Completion offers a gherkin block where a section has none. The preview renders prose as Markdown (it already does). | The Preview and LanguageServer tests, including new ones for prose and the hint; the preview snapshots change only where a file changed. |
| 5 | **Docs**: the template guide and the step definitions guide describe the format of section 3, with a prose example. Also update the skills, `copilot-instructions.md`, the prompt file, and the docs' examples of steps (fenced). Add a CHANGELOG entry under **Changed**, marked breaking: steps outside a gherkin block are prose, so an old-form file loses its steps. | The guides' examples parse with the new parser (a docs test that parses each ` ```gherkin ` example that is a whole feature). |

After each step, from the Brinell root:
- build `srcnew\Brinell.sln`;
- run the CI unit suites, `Brinell.Bdd.Tests`, the Preview, Workspace, LanguageServer,
  DataValidator and Presenter tests;
- list the Todo UAT suites' tests (43/35/4/3);
- run tier 1 of `Brinell.Maui.UITests`.

## 6. Decisions

| # | Question | Decided |
| --- | --- | --- |
| Q1 | Markdig in `Brinell.Bdd`, or extend the hand-written line parser? | **Markdig**: otherwise we end up writing our own. One Markdown reading for the engine and the preview. |
| Q2 | An outline's Examples: a Markdown table under `### Examples`, or Gherkin's `Examples:` inside the block? | **A Markdown table under `### Examples`**, as today. It is data, like the Metadata and Data tables; tags stay above the heading. |
| Q3 | A paragraph that reads like steps, outside a gherkin block? | **Prose**, always. The current files in the old form are fixed to use ` ```gherkin ` blocks (phase 3). |
| Q4 | Prose inside a gherkin block, other than a `#` comment? | **Not now**: a block holds Gherkin only (`UAT011`); prose goes between blocks. Revisit if it is asked for. |
