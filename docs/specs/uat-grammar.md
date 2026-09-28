# UAT Scenario Grammar

Status: **Implemented**. The parser is
[BddMarkdownParser.cs](../../srcnew/Brinell.Uat/BddMarkdownParser.cs); this page writes down the
format it accepts, so the format is a contract and not whatever the parser happens to do.

Three layers, each owned by one piece of code:

1. **The document grammar**: the shape of a `.feature.md` or `.steps.md` file. Owned by the
   parser, and fixed.
2. **The phrase language**: the `{placeholder}` templates a step line binds to. Owned by the
   catalog ([BddCommandCatalog.cs](../../srcnew/Brinell.Uat/BddCommandCatalog.cs)); phrases are
   declared by attributes (AD-011) or as callable phrases (AD-017).
3. **The lexicon**: the page, control, app and user names that fill the placeholders. Owned by
   name resolution (`[UatName]`, `UatNameInference`, the `Apps` table).

How to write scenarios is in the [UAT Template Guide](../guides/uat-template-guide.md); what the
phrases are and how they bind is in [UAT Phrases And Flows](../guides/uat-step-definitions.md).

## 1. Scenario files (`*.feature.md`)

`NEWLINE` and `WS` are lexical; `text` is the rest of the line, trimmed.

```ebnf
document         = feature-heading ,
                   { metadata-section | background-section | data-section
                   | phrase-section | scenario } ;          (* at least one scenario *)

feature-heading  = "# Feature:" , text , NEWLINE ;             (* exactly one, first *)

metadata-section = "## Metadata" , NEWLINE , kv-table ;
kv-table         = table-with-columns("Field", "Value") ;

background-section = "## Background" , NEWLINE , step+ ;

data-section     = "## Data:" , name , NEWLINE , table ;

phrase-section   = ( "## Step:" | "## Step for each row:" ) , phrase , NEWLINE ,
                   [ parameters-table ] , phrase-step+ ;
parameters-table = table-with-columns("Parameter", "Type" [, "Example"]) ;

scenario         = [ tag-line ] ,
                   ( "## Scenario:" , name , NEWLINE , step+
                   | "## Scenario Outline:" , name , NEWLINE , step+ ,
                     "### Examples" , NEWLINE , table ) ;

tag-line         = "@" , tag , { WS , "@" , tag } , NEWLINE ;

step             = keyword , WS , step-text , NEWLINE , [ table ] , [ doc-string ] ;
phrase-step      = step | keyword , WS , step-text , NEWLINE , passed-block ;
passed-block     = "<table>" | "<doc string>" ;
keyword          = "Given" | "When" | "Then" | "And" | "But" ;

doc-string       = '"""' , NEWLINE , { line } , '"""' , NEWLINE ;

table            = header-row , separator-row , data-row+ ;
separator-row    = "|" , ( "---" , { "-" | ":" } , "|" )+ ;
```

Rules the EBNF does not show:

- **Keywords.** `And` and `But` take the effective keyword of the step before them, so a step
  binds as `Given`, `When` or `Then` only. An `And` with nothing before it is `UAT016`.
- **Tags** must be on the line directly above a `## Scenario:` or `## Scenario Outline:` heading.
- **Outlines.** Each `<column>` in an outline step is replaced from one `### Examples` row, and
  every placeholder needs a column. Row *n* runs as `<name> [n]`.
- **Tables.** Every row has as many cells as the header, and a table has at least one data row.
  Cells are trimmed. Where a table sits under a step, the step gets the table.
- **Doc strings.** A `"""` block under a step (after its table, if it has one) is passed to the
  step as text. Lines lose the indentation of the opening `"""`.
- **Code fences are transparent.** A ```` ``` ```` or `~~~` line, with or without an info
  string, is dropped before parsing, so steps fenced as `gherkin` for rendering parse like bare
  ones. Line numbers are kept, so diagnostics point at the line the author sees. Only the fence
  is transparent: a list marker (`- Given ...`) is still an error.
- **Headings** other than the ones above, and any other text outside a section, are errors.

## 2. Phrase files (`*.steps.md`)

```ebnf
phrases-document = "# Steps:" , text , NEWLINE , phrase-section* ;
```

A phrase file holds only phrase sections. A phrase in a scenario file is seen by that file; a
phrase in a `*.steps.md` file in `Features/` or a folder under it is seen by the scenarios in
that folder and below (AD-017, [Where Phrases Live](../guides/uat-step-definitions.md#where-phrases-live)).

In a phrase section:

- The heading's `{x}` placeholders are the call's arguments. The body uses them as `<x>`. In a
  `## Step for each row:` section, `<Column>` reads a column of the current row of the call's
  table.
- `<table>` or `<doc string>` alone on the line under a body step passes the call's table or doc
  string to that step. It is only allowed in a phrase section.
- The optional `Parameters` table declares a type that the body's use does not show (AD-017).

## 3. The phrase language

A phrase is the template a step line matches:

```ebnf
phrase      = segment , { WS , segment } ;
segment     = literal | placeholder ;
placeholder = "{" , letter , { letter | digit | "_" | "-" } , "}" ;
```

- A phrase matches the **whole** step text, case-sensitively. Whitespace in the phrase matches
  one or more whitespace characters.
- A placeholder's value is trimmed, and surrounding double quotes are removed: `"Alice"` is
  `Alice`. The value is then read as the bound parameter's type (AD-016).
- `{page}` and `{control}` name a page and a control of the current page. `{user}` and `{app}`
  in first place name the step's subject (AD-015). Every other placeholder is an argument.
- Binding keeps the phrases with the step's effective keyword that match, prefers phrases with
  no placeholder, then the phrase with the most literal characters. Two left is ambiguous
  (`UATB002`); none is unknown (`UATB001`). Callable and C# phrases compete under the same rule.

No optional words, alternatives, regexes or types in phrase text (AD-016).

## 4. The lexicon

A page or control name is its `[UatName("...")]`, or, without one, its member name with the
type suffix removed (`Page`, `Button`, `Entry`, ...) and split into words: `TodoListPage` is
`Todo List`, `AddButton` is `Add`, `TitleError` is `Title Error`. Users and apps come from the
`Apps` table. A scenario never names an AutomationId.

## 5. Parser diagnostics

| Code | Meaning |
| --- | --- |
| `UAT000`, `UAT001` | no `# Feature:` heading, or more than one |
| `UAT002`, `UAT003` | the heading has no title, or is not first |
| `UAT004` | `## Background` has no steps |
| `UAT005` | `## Data:` has no name |
| `UAT006`, `UAT007` | a scenario has no name, or no steps |
| `UAT008` | the file has no scenario |
| `UAT009`, `UAT010` | the Metadata table's columns are not `Field`/`Value`, or a field is empty |
| `UAT011` | a line in a step block is not a step |
| `UAT012` - `UAT015` | an outline has no `### Examples`, no name, no steps, or a placeholder with no column |
| `UAT016` | `And`/`But` without a step before it |
| `UAT017` - `UAT019` | a table is missing, has a row of the wrong width, or has no data rows |
| `UAT020` | a tag line is not directly above a scenario |
| `UAT021`, `UAT022` | text outside a section, or an unknown heading |
| `UAT023` | a doc string is not closed |
| `UAT024` | `<table>`/`<doc string>` outside a phrase section, or not under a step |
| `UAT025`, `UAT026` | a phrase heading has no phrase, or the phrase has no steps |
| `UAT027` | a malformed `Parameters` table |
| `UAT028` | a steps file with content other than one `# Steps:` heading and step sections |
| `UAT029` | a heading or file name in the form before feature files (`# UAT:`, `## Phrase:`, `*.uat.md`, `*.phrases.md`); `brinell-data validate --fix` rewrites it |

Binder (`UATB0xx`) and argument (`UATA0xx`) diagnostics are raised in the binder and the value
reader (`BddBinder`, `BddValues`, `BddPhraseDeclarations`).
