# 01 — The Gherkin language, formalised

The parser in [UatMarkdownParser.cs](../../../srcnew/Brinell.Uat/UatMarkdownParser.cs)
already accepts a specific Gherkin-in-Markdown dialect. This document writes that
dialect down as a grammar so it is a *contract*, and separates the three layers
that are easy to confuse:

1. **The document grammar** — the shape of a `.uat.md` file (feature, scenarios,
   steps, tables). Owned by the parser.
2. **The phrase mini-language** — the `{placeholder}` templates a step line binds
   to. Owned by the catalog.
3. **The lexicon** — the page and control *names* that fill the placeholders.
   Owned by name resolution (`[UatName]` / inference).

Keeping them separate is the whole point: layer 1 is fixed, layer 2 is what this
proposal makes attribute-driven, and layer 3 is already attribute-driven.

---

## 1. Document grammar (EBNF)

This is the current shape, expressed as EBNF. `NEWLINE`, `WS`, and Markdown
table rows are lexical tokens.

```ebnf
document        = feature-heading ,
                  [ metadata-section ] ,
                  [ background-section ] ,
                  { data-section } ,
                  scenario+ ;

feature-heading = "# UAT:" , text , NEWLINE ;

metadata-section = "## Metadata" , NEWLINE , kv-table ;
kv-table         = header-row("Field","Value") , { kv-row } ;
kv-row           = "|" , text , "|" , text , "|" , NEWLINE ;

background-section = "## Background" , NEWLINE , step+ ;

data-section     = "## Data:" , name , NEWLINE , markdown-table ;

scenario         = { tag-line } ,
                   ( "## Scenario:" , name , NEWLINE , step+
                   | "## Scenario Outline:" , name , NEWLINE , step+ ,
                     "### Examples" , NEWLINE , markdown-table ) ;

tag-line         = { "@" , tag } , NEWLINE ;   (* must abut the Scenario line *)

step             = keyword , WS , step-text , [ NEWLINE , markdown-table ] , NEWLINE ;
keyword          = "Given" | "When" | "Then" | "And" | "But" ;
step-text        = text - NEWLINE ;

markdown-table   = header-row , delimiter-row , data-row+ ;
```

Rules the parser enforces that EBNF alone does not show:

- Exactly **one** `feature-heading` per file.
- `And` / `But` inherit the **effective keyword** of the previous step. So the
  effective keyword alphabet is only `{Given, When, Then}` — this is
  `UatEffectiveStepKeyword` in [UatModels.cs](../../../srcnew/Brinell.Uat/UatModels.cs),
  distinct from the surface `UatStepKeyword` (`{Given, When, Then, And, But}`).
- Tag lines must **immediately precede** a `## Scenario:` / `## Scenario
  Outline:` line.
- A `## Scenario Outline:` requires an `### Examples` table; each `<column>`
  placeholder in a step is expanded per row.
- **Code-fence delimiters are transparent.** A line that is a ``` or `~~~` fence,
  with or without an info string, is dropped during tokenisation
  ([UatMarkdownParser.cs](../../../srcnew/Brinell.Uat/UatMarkdownParser.cs),
  `SplitLines`), so a scenario whose steps are fenced for rendering parses exactly
  like a bare one. Line numbers are assigned before the drop, so diagnostics still
  point at the line the author sees. A list marker (`-`) is still not allowed:
  only the fence is transparent, not the step's own prefix.

### Worked example, annotated

```markdown
# UAT: A new todo needs a title      ← feature-heading (exactly one)

## Metadata                          ← metadata-section
| Field | Value |
| --- | --- |
| Target | MAUI |

@smoke @todo                         ← tag-line (abuts the Scenario)
## Scenario: Saving with a blank title is refused
```gherkin                           ← optional; fences are dropped before parsing
Given I am on the Todo List page     ← step, effective keyword Given
When I tap Add                       ← step, effective When
And I clear Title                    ← And inherits When
Then Title Error should contain "Title is required"   ← step, effective Then
```
```

---

## 2. The phrase mini-language

A **phrase** is the template a step line matches. It is not free Gherkin — it is
a small pattern language compiled to a regex by
[UatCommandCatalog.cs](../../../srcnew/Brinell.Uat/UatCommandCatalog.cs).

```ebnf
phrase      = segment , { WS , segment } ;
segment     = literal | placeholder ;
placeholder = "{" , identifier , "}" ;
identifier  = letter , { letter | digit | "_" | "-" } ;
```

Matching semantics (already implemented):

- The compiled regex is **anchored** to the whole step text.
- Phrase literals are **case-sensitive**.
- Literal whitespace matches **one or more** whitespace characters.
- Captured placeholder values are **trimmed**, and surrounding double quotes are
  stripped: `"Alice"` → `Alice`.

Two reserved placeholder names carry meaning for the built-in verbs:

| Placeholder | Meaning |
| --- | --- |
| `{page}` | resolves against the page lexicon (opens / asserts a page) |
| `{control}` | resolves against the current page's control lexicon |

Any other placeholder (e.g. `{value}`, `{text}`) is a plain captured argument
passed to the bound method.

### Binding and precedence

`UatBinder` must resolve a step to **exactly one** phrase. The catalog ranks
candidates:

1. Only phrases whose keyword equals the step's **effective keyword** are
   considered.
2. **Exact** phrases (no placeholders) beat parameterised ones.
3. Among parameterised phrases, the one with **more literal non-whitespace
   characters** (higher *specificity*) wins.
4. A remaining tie is an **ambiguity error** (`UATB002`); no match is
   `UATB001` (unknown).

This precedence is why `{control} should be visible` and `{control} should not
be visible` coexist safely: the second has more literal text, so `not be
visible` never accidentally binds to the first.

---

## 3. The lexicon (names)

Placeholders are filled from *names*, and names come from the runtime model, not
from the Markdown. This is layer 3, and it is **already attribute-driven** — this
proposal only documents it as part of the language and reuses the same discovery
pass.

A name is resolved for a page or control member as:

```
name = [UatName("...")].Name          (explicit wins)
     | UatNameInference.FromMember()   (fallback)
```

`UatNameInference` strips a known suffix (`Page`, `Button`, `Entry`, `Picker`,
...) and splits PascalCase / `_` / `-` into words:

| Code member | Lexicon name |
| --- | --- |
| `TodoListPage` | `Todo List` |
| `AddButton` | `Add` |
| `TitleError` (a `Label`) | `Title Error` |
| `FirstNameEntry` | `First Name` |

So `When I tap Add` fills `{control}` with `Add`, which resolves to
`TodoListPage.AddButton`. The scenario author never writes an AutomationId; the
lexicon is the bridge from readable name → page member → AutomationId.

---

## 4. How the three layers meet for one step

```text
"Then Title Error should contain \"Title is required\""
  │
  ├─ layer 1  parser → UatStep{ EffectiveKeyword = Then, Text = "Title Error should contain \"Title is required\"" }
  │
  ├─ layer 2  catalog → phrase "{control} should contain {value}"
  │            captures { control = "Title Error", value = "Title is required" }
  │
  └─ layer 3  lexicon → "Title Error" == TodoEditPage.TitleError  (AutomationId TodoEdit_TitleError)
              bound call → TitleError.AssertTextContains("Title is required")
```

Today, the *phrase* in layer 2 is one of the 18 hardcoded rows. The next
document replaces that row with an attribute sitting on the `AssertTextContains`
method itself.
