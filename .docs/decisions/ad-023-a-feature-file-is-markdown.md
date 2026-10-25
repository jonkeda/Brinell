# AD-023: A Feature File Is Markdown; Gherkin Is Fenced

A feature or step file is a Markdown document first. The engine reads a few parts of it, and the
rest is prose:

````markdown
# Feature: Todo list

Why this feature matters, in any Markdown: paragraphs, lists, quotes, links, images.

## Metadata

| Field | Value |
| --- | --- |
| TestData | todos |

@smoke
## Scenario: Add a todo

A note on the scenario.

```gherkin
Given I am on the Todo List page
When I add the todo "Buy milk"
```

Prose between two blocks of one scenario is allowed.

```gherkin
Then the list shows "Buy milk"
```

## Notes

A section the engine does not know is prose, up to the next level-2 heading.
````

- **Gherkin only in a ` ```gherkin ` block.** A step, its table, its doc string and a `#` comment
  are read only inside a fenced block whose info string is `gherkin` (any case, ` ``` ` or `~~~`).
  A block holds Gherkin only: a step, a `#` comment or a blank line. A section may have several
  blocks; they are read in order as one list, and `And` continues across them.
- **Everything else is prose, in any form.** Paragraphs, lists, quotes, images, links, HTML, rules,
  code blocks in other languages, a level-2 heading the engine does not know (a prose section up
  to the next level-2 heading), and any lower heading but an outline's `### Examples`. Text that
  reads like a step outside a gherkin block is prose too, and is not checked. A block nested in a
  list or a quote is prose.
- **The parts the engine reads**, at the top level of the document: the title (`# Feature:` or
  `# Steps:`, the only level-1 heading); `## Metadata`, `## Data:` and `### Examples` with the
  first table of their section; `## Background`, `## Scenario:`, `## Scenario Outline:`,
  `## Step:` and `## Step for each row:` with their gherkin blocks (a `## Step:` may have a
  `Parameters` table before its first block); tag lines directly above a scenario heading.
- **One Markdown reading.** The engine finds these parts in Markdig's block tree, the same library
  the preview renders with, so the engine, the preview and the language server split a file the
  same way. Line numbers are the file's.
- **An outline's Examples stay a Markdown table** under `### Examples`: data, like the Metadata and
  Data tables, not steps.
- **A file in the old form loses its steps, and says so.** A section with no gherkin block has no
  steps (`UAT004`, `UAT007`, `UAT014`, `UAT026`); when it holds text that reads like steps, the
  error says to put them in a gherkin block, and `bruinell data validate --fix` (or the editor's
  quick fix) wraps them in one. A heading that looks like a section but is not one (`## scenario:`,
  `### Scenario:`, `## Scenario x`) is a warning, `UAT031`, so a typo is not silently prose.

**Why:** feature files are read on GitHub, in the preview and in review, by people who are not the
engine. They need room to explain a scenario, and a block that marks exactly what is Gherkin. The
line parser before this dropped every fence of any language, so fences were decoration, the
content of a ` ```json ` block was read as steps, a `#` comment ended a scenario, and any prose was
an error; the preview and the language server, which already read gherkin blocks only, disagreed
with it. Markdig handles the edge cases of Markdown (a `Given` line in a list, a fence in a quote)
that a hand-written parser would have to grow into.

**Ruled out:** bare steps beside fenced ones (the engine would guess which lines are Gherkin);
prose inside a gherkin block other than a `#` comment (not now; prose goes between blocks);
Gherkin's own `Examples:` inside the block; a step-like paragraph as an error or a warning when the
section has a block (prose may start with "Given that"); a hand-written Markdown reader.

**Broken when:** a line outside a ` ```gherkin ` block is read as a step; a Markdown form outside
the parts above is an error; the engine and the preview disagree on which lines are a scenario's
steps; a fence of another language is read as Gherkin.
