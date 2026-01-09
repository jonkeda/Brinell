# SpxLlm Configuration — Brinell Framework

- **version**: 1.0
- **output**: slm.md
- **parser**: spx-to-slm
- **encoding**: utf-8

This configuration defines which SPX content is extracted for LLM consumption in the Brinell UI Test Automation Framework.

---

## Include

These SPX blocks are extracted to `.slm.md` files:

| Block Type | Section | Relevance | Notes |
|------------|---------|-----------|-------|
| specification | interface | full | Complete C# interface definitions |
| specification | boundary | full | Edge cases, null handling, timeouts |
| specification | method-signatures | full | Quick reference tables |
| behavior | rules | summary | Numbered rules (1. 2. 3.) only |
| acceptance | scenarios | summary | Gherkin Given/When/Then |
| pattern | structure | full | Pattern code blocks |
| pattern | usage | summary | Implementation guidance |
| decision | decision | summary | ADR decision statement |
| decision | consequences | summary | Positive/negative impacts |

---

## Exclude

These SPX blocks are NOT extracted:

| Block Type | Section | Reason |
|------------|---------|--------|
| behavior | detailed | Verbose prose explanations |
| behavior | overview | Restates interface, redundant |
| examples | all | Human-focused code samples |
| assumption | all | Background context only |
| exclusion | all | Negative scope definitions |
| overview | prose | Marketing-style descriptions |
| overview | context | Project background |
| related | all | Cross-reference links |
| layer | all | Architecture structure |
| module | all | Code organization |
| goal | all | High-level business intent |
| quality | all | Non-functional requirements |
| history | all | Version history |
| revision | all | Change tracking |

---

## Overrides

File-specific configuration overrides.

### 250_001_IControlObject.spx.md

| Action | Section | Reason |
|--------|---------|--------|
| include | assumption | Foundation interface, context critical |
| include | overview.purpose | Core interface needs intro |

### 250_002_IClickableControl.spx.md

| Action | Section | Reason |
|--------|---------|--------|
| include | behavior.click-variants | Click types important for LLM |

### 250_003_ITextControl.spx.md

| Action | Section | Reason |
|--------|---------|--------|
| include | behavior.text-operations | Enter/Clear/GetText patterns |

### 250_004_IToggleControl.spx.md

| Action | Section | Reason |
|--------|---------|--------|
| include | behavior.state-management | Checked/unchecked/indeterminate |

### 250_005_ISelectorControl.spx.md

| Action | Section | Reason |
|--------|---------|--------|
| include | behavior.selection-modes | Single vs multi-select critical |

### 231_001_ControlObjectPattern.spx.md

| Action | Section | Reason |
|--------|---------|--------|
| include | examples.implementation | Pattern examples aid understanding |

### 202_002_InterfaceFirst.spx.md

| Action | Section | Reason |
|--------|---------|--------|
| include | examples.workflow | Interface-first workflow critical |

---

## Output Format

Generated `.slm.md` files follow this structure:

1. **Header**
   - Auto-generated warning comment
   - Source file reference
   - Generation timestamp

2. **Interface** (if specification)
   - Complete C# interface code block
   - All method signatures
   - XML documentation (condensed)

3. **Rules** (if present)
   - Numbered list format
   - One rule per line
   - No explanatory prose

4. **Boundaries** (if present)
   - Table format: Scenario | Behavior
   - Focus on edge cases
   - Include null/timeout handling

5. **Dependencies** (if present)
   - Bulleted list of required types
   - Namespace references
   - Related interfaces

6. **Patterns** (if present)
   - Code structure templates
   - Implementation guidance
   - Usage examples (condensed)

---

## Section Markers

The parser identifies sections by these patterns:

| Pattern | Matches |
|---------|---------|
| `## Interface` | Interface code blocks |
| `## Behavior` | Behavior descriptions |
| `### Rules` or `**Rules:**` | Numbered rule lists |
| `## Boundary` or `## Edge Cases` | Boundary tables |
| `## Dependencies` | Dependency lists |
| `## Acceptance` | Gherkin scenarios |
| ` ```csharp` after `## Interface` | Interface code |

---

## Priority Configuration

For large files, limit extraction by priority:

| Priority | Content Type | Include When |
|----------|--------------|--------------|
| 1 (Critical) | Interface code | Always |
| 2 (High) | Rules list | Always |
| 3 (High) | Boundary table | Always |
| 4 (Medium) | Acceptance scenarios | Token budget allows |
| 5 (Medium) | Dependencies | Token budget allows |
| 6 (Low) | Pattern examples | Explicitly requested |

**Token budget:** Default 2000 tokens per `.slm.md` file.

---

## Notes

### Why Markdown Format?

This configuration uses Markdown tables instead of YAML because:

1. **Consistency** — Matches SPX documentation format
2. **Readability** — Tables are clear and scannable
3. **Tooling** — Renders in GitHub, VS Code, Azure DevOps
4. **Versioning** — Diffs are meaningful
5. **Human-friendly** — Easy to edit without syntax errors

### Validation

The parser validates:

- [ ] All listed block types exist in SPX schema
- [ ] All override files exist
- [ ] No conflicting include/exclude rules
- [ ] Output format sections are recognized

### Iteration

This configuration should be refined based on:

1. LLM task performance (do `.slm.md` files help?)
2. Token budget constraints (too much? too little?)
3. New specification files (add overrides as needed)
4. Team feedback (what's missing? what's noise?)

---

## Related Documents

- [E10_SpxLlm.md](../../SPX/Docs/V7/blocks2/E00_syntax/E10_SpxLlm.md) — Format reference
- [IDEAS-003](../ideas/IDEAS-003-SPX-LLM-Integration-QA.md) — LLM integration analysis
- [.github/copilot-instructions.md](../.github/copilot-instructions.md) — Copilot configuration

---

**Version:** 1.0  
**Status:** Proposal  
**Created:** January 9, 2026  
**Last Review:** January 9, 2026
