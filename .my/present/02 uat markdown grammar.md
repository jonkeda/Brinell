# UAT Markdown Grammar

Status: Pointer — the grammar is specified in [01-grammar.md](../uat/attribute-catalog/01-grammar.md)
Date: 2026-09-21
Area: `srcnew/Brinell.Uat/UatMarkdownParser.cs`
Related:

- [The Gherkin language, formalised](../uat/attribute-catalog/01-grammar.md) — **the specification**
- [Development roadmap](04%20development%20roadmap.md) §Phase 2 — the phase that implements it
- [Runner code binding](03%20runner%20code%20binding.md) — what happens to a step after it parses

---

## Why this document is a pointer

This slot was cited by [04](04%20development%20roadmap.md) Phase 2 from the
beginning, but the document was never written — the parser was built first and
the grammar stayed implicit in it. It was written down afterwards, from the
shipped parser, as
[01-grammar.md](../uat/attribute-catalog/01-grammar.md) in the attribute-catalog
folder.

Rather than copy an EBNF that would immediately start drifting, this file records
where the specification lives and what it covers. **Read `01-grammar.md` for the
grammar itself.**

---

## What is specified there

The key move in that document is separating three things that are easy to
conflate, because each is owned by different code and each changes for different
reasons:

| Layer | What it governs | Owned by | Stability |
| --- | --- | --- | --- |
| 1. Document grammar | The shape of a `.uat.md` file — feature heading, metadata, background, data, scenarios, outlines, tables | `UatMarkdownParser` | Fixed |
| 2. Phrase mini-language | The `{placeholder}` templates a step line binds to, and how one is picked | `UatCommandCatalog` / `UatBinder` | Grows with the vocabulary |
| 3. Lexicon | The page and control *names* that fill the placeholders | `[UatName]` / `UatNameInference` | Per application |

`01-grammar.md` §1 gives layer 1 as EBNF plus the rules EBNF cannot express, §2
gives the phrase language and the binding precedence, §3 gives name resolution,
and §4 walks a single step through all three.

---

## The three things worth knowing without opening it

**`And` and `But` are not separate keywords to a phrase.** The surface alphabet
is `{Given, When, Then, And, But}` (`UatStepKeyword`), but a phrase binds against
the *effective* keyword, which is only `{Given, When, Then}`
(`UatEffectiveStepKeyword`) — `And` and `But` inherit the previous step's. This is
why `[UatStep]` and `[UatPhrase]` take a `UatEffectiveStepKeyword` and why there
is no way to declare an `And` phrase.

**A step line may be fenced, but not bulleted.** `Given …` at the start of the
line, with no list marker. A bulleted step does not parse. A **fenced** one does:
fence delimiters are dropped during tokenisation, so steps wrapped in
```gherkin for rendering execute exactly like bare ones, and diagnostics still
report the authored line number. This is why the Todo sample's scenarios are
fenced — unfenced consecutive step lines collapse into one paragraph in a Markdown
preview.

**Parse errors are line-numbered and coded.** `UAT000`–`UAT022` come from the
parser — exactly one `# UAT:` heading (`UAT001`), scenarios must have a name
(`UAT006`) and at least one step (`UAT007`), tag lines must abut a scenario
heading (`UAT020`), and so on. Binding and discovery add their own:
`UATB001`–`UATB004` and `UATD001`–`UATD003`, which are tabulated in
[03](03%20runner%20code%20binding.md) §Validation Rules. The parser codes are in
`UatMarkdownParser.cs`.

---

## A file, annotated

```markdown
# UAT: A new todo needs a title      ← feature heading (exactly one, first)

## Metadata                          ← optional
| Field | Value |
| --- | --- |
| Target | MAUI |

@smoke @todo                         ← tags must abut the Scenario line
## Scenario: Saving with a blank title is refused
Given I am on the Todo List page     ← effective keyword Given
When I tap Add                       ← effective When
And I clear Title                    ← And inherits When
Then Title Error should contain "Title is required"
```

`## Scenario Outline:` plus an `### Examples` table expands one scenario per row,
substituting `<column>` placeholders into step text and tables.
