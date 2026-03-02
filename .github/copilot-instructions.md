# Copilot Instructions for Brinell Framework

**Last Updated:** February 14, 2026

This document provides guidance for GitHub Copilot and other AI assistants working on the Brinell UI test automation framework.

---

## 0. Code Anti-Patterns (Critical)

### ❌ NEVER Use Thread.Sleep or Arbitrary Waits — Anywhere

**NEVER** use `Thread.Sleep()`, `Task.Delay()`, or any arbitrary time-based waits — not in test code, not in framework code, not anywhere. This applies to **ALL** code: tests, controls, drivers, helpers.

```csharp
❌ WRONG - Arbitrary sleep in test:
element.Click();
Thread.Sleep(500);  // NEVER DO THIS
Assert.True(nextElement.IsVisible());

❌ WRONG - Arbitrary sleep in framework/control code:
_plusButton.Click();
Thread.Sleep(50);  // NEVER DO THIS EITHER
var value = GetValue();

✅ CORRECT - Wait for a condition:
element.Click();
nextElement.WaitVisible(true, timeoutMs: 5000);
Assert.True(nextElement.IsVisible());

✅ CORRECT - Poll until state changes:
_plusButton.Click();
WaitForValueChange(previousValue, timeoutMs: 1000);

✅ CORRECT - Use polling:
element.Click();
// Framework polls until condition is met or timeout
nextElement.AssertVisible("Element should appear after click");
```

**Why this matters:**
- Sleeps are flaky — they either wait too long or not long enough
- They make tests slow and unreliable
- They hide real timing bugs instead of solving them
- The Brinell framework has built-in `Wait*` and `Assert*` methods that poll for conditions
- Always wait FOR something specific, never wait arbitrarily

**Rule:** If you need to wait, wait **for a condition** — a value change, an element appearing, a property becoming true. Never wait for time.

### ❌ NEVER Increase Wait Times to Fix Failures

If a fix involves increasing `Thread.Sleep` durations or adding longer arbitrary waits, **the approach is wrong**. There is a deeper root cause.

```csharp
❌ WRONG - Escalating waits:
Thread.Sleep(50);   // didn't work
Thread.Sleep(200);  // still didn't work
Thread.Sleep(500);  // "fixed" it... for now

✅ CORRECT - Find the real problem:
// Why is the value stale? Because we're reading a cached element.
// Fix: re-find the element, or poll for the expected value.
```

**Rule:** If you find yourself raising wait times, stop and find the actual root cause. The real fix is almost never "wait longer."

### ❌ NEVER Use Empty Catch Blocks

**NEVER** swallow exceptions silently. Empty catches hide bugs and make debugging impossible.

```csharp
❌ WRONG - Empty catch:
try
{
    var button = FindChildButton(element, isIncrement: true);
    button.Click();
}
catch
{
    // Swallow - will fall back to base implementation
}

✅ CORRECT - Log or rethrow with context:
try
{
    var button = FindChildButton(element, isIncrement: true);
    button?.Click();
}
catch (Exception ex)
{
    throw new InvalidOperationException(
        $"Failed to find/click increment button for '{AutomationId}'", ex);
}

✅ CORRECT - Use conditional logic instead of exceptions for control flow:
var button = FindChildButton(element, isIncrement: true);
if (button != null)
{
    button.Click();
    return;
}
// Fall through to base implementation
base.IncrementCore(element);
```

**Why this matters:**
- Empty catches make failures invisible — you get wrong results with no clue why
- Debugging becomes guesswork instead of following a clear error trail
- If something CAN fail, handle it explicitly or let it propagate

**Rule:** Never use exceptions for control flow. Use null checks and conditionals instead. If you catch an exception, log it or wrap it with context.

---

## 1. Markdown File Formatting (Critical)

### ❌ NEVER Start .md Files with Code Fences

Markdown files should contain raw markdown content, NOT be wrapped in code fences.

```markdown
❌ WRONG - File starts with code fence:
````markdown
# My Document
Content here...
````

✅ CORRECT - File starts with markdown content:
# My Document
Content here...
```

**Why this matters:**
- `.md` files ARE markdown - they don't need to be wrapped
- Code fences are for embedding code WITHIN markdown, not wrapping entire files
- Starting with ` ``` ` breaks rendering in most markdown viewers
- GitHub, VS Code, and documentation tools expect raw markdown

**Rule:** When creating or editing `.md` files, write markdown content directly. Never wrap the entire file in ` ```markdown ... ``` ` fences.

---

## 2. Mermaid Diagram Syntax (Critical)

### Current Environment
- **Mermaid Version:** 11.x (latest as of January 2026)
- **Usage:** Class diagrams for control hierarchies, architecture visualization
- **Documentation:** https://mermaid.js.org/syntax/classDiagram.html

### ❌ DEPRECATED SYNTAX (Do NOT Use)

These patterns cause rendering errors in Mermaid 11.x:

**1. HTML tags in class labels**
```mermaid
❌ WRONG:
class IControlObject["IControlObject<br/>(Base)"]

✅ CORRECT:
class IControlObject {
    <<interface>>
}
note for IControlObject "Base interface"
```

**2. Method return types with space separator**
```mermaid
❌ WRONG:
IControlObject : IsExists() bool
IControlObject : GetText() string

✅ CORRECT:
+IsExists() bool
+GetText() string
```

**3. Complex inline method signatures**
```mermaid
❌ WRONG:
ITextControl : Enter(string) 
ITextControl : GetItems() IReadOnlyList~string~
ITextControl : AssertTextMatches(string, string?)

✅ CORRECT:
+Enter(string text) void
+GetItems() List
+AssertTextMatches(string pattern, string message) void
```

**4. Nullable type notation in diagrams**
```mermaid
❌ WRONG:
IControlObject? Page
string? GetText()

✅ CORRECT:
IPageObject Page
GetText() string
(Note: Use actual return/property types, not nullable notation)
```

**5. Generic type notation with tildes**
```mermaid
❌ WRONG:
GetItems() IReadOnlyList~string~
GetChild~T~(string) T

✅ CORRECT:
GetItems() List
GetChild(string) T
(Note: Simplify generics or use plain names)
```

### ✅ CORRECT PATTERNS

**1. Class declarations with interface marker**
```mermaid
class IControlObject {
    <<interface>>
    string AutomationId
    IPageObject Page
    +IsExists() bool
    +GetText() string
}
```

**2. Inheritance relationships**
```mermaid
IClickableControl --|> IControlObject
ITextControl --|> IControlObject
IEditableTextControl --|> ITextControl
```

**3. Visibility modifiers**
```mermaid
+IsExists() bool              ← Public method
#FindElement() AppiumElement  ← Protected method
-WaitForElement() AppiumElement ← Private method
string AutomationId           ← Public property
#IPageObject _page            ← Protected field
```

**4. Method signatures with return types**
```mermaid
+Click() void
+Enter(string text) void
+GetText() string
+WaitExists(bool exists, int timeout) bool
+AssertTextEquals(string expected, string message) void
```

**5. Using notes for additional context**
```mermaid
class ITextControl {
    <<interface>>
    +Enter(string text) void
    +Clear() void
}
note for ITextControl "For text input controls"
```

**6. Multiple inheritance (interfaces)**
```mermaid
class MyControl {
}
MyControl --|> IControlObject
MyControl --|> IClickableControl
MyControl --|> ITextControl
```

### Common Issues & Solutions

| Problem | Cause | Solution |
|---------|-------|----------|
| Classes don't render | Invalid method syntax | Simplify signatures, remove special characters |
| Missing relationships | Syntax errors in method defs | Check method format: `+name(params) return` |
| Diagram blank | HTML tags in class name | Use `<<interface>>` and separate notes |
| Text not displaying | Complex generic notation | Use simplified types (e.g., `List` instead of `IReadOnlyList~T~`) |
| Rendering timeout | Too many complex methods | Split large diagrams or reduce method count |

---

## 3. Class Diagram Best Practices

### Design Principles
1. **Keep it simple** - Too many methods in a class box causes rendering issues
2. **Use notes for descriptions** - Don't put descriptions in class names
3. **Show relationships clearly** - Use inheritance arrows for interface implementation
4. **Group related classes** - Organize base classes, then concrete implementations
5. **Limit methods shown** - Show ~10 key methods, not entire API

### Method Organization
```mermaid
class IControlObject {
    <<interface>>
    % Properties first
    string AutomationId
    IPageObject Page
    
    % Then methods in logical order
    % State checking
    +IsExists() bool
    +IsVisible() bool
    +IsEnabled() bool
    
    % Waiting/polling
    +WaitExists(bool, int) bool
    +WaitVisible(bool, int) bool
    
    % Assertions
    +AssertExists(string) void
    +AssertVisible(string) void
}
```

### Inheritance Chain Example
```mermaid
ControlBase --|> IControlObject
TextControlBase --|> ControlBase
TextControlBase --|> ITextControl
EditableTextControlBase --|> TextControlBase
EditableTextControlBase --|> IEditableTextControl
```

---

## 4. Brinell-Specific Guidelines

### Control Naming Conventions
- **Interfaces:** `I<ControlType>` (e.g., `IClickableControl`, `ITextControl`)
- **Base Classes:** `<ControlType>Base` (e.g., `TextControlBase`, `ToggleControlBase`)
- **Concrete Classes:** `<ControlType>Control` (e.g., `ButtonControl`, `EntryControl`)

### Platform Separation
When documenting multi-platform controls:
1. **Separate diagrams** for MAUI and Blazor implementations
2. **Use notes** to indicate platform context
3. **Reference common interfaces** in both diagrams
4. **Keep inheritance patterns consistent** across platforms

### Interface Categories
- **Core:** IControlObject (all controls)
- **Interaction:** IClickableControl (buttons, links)
- **Text:** ITextControl, IEditableTextControl
- **Selection:** ISelectorControl, IToggleControl
- **Range:** IRangeControl, ISlider
- **Collections:** IItemsControl, IContainerControl
- **Scrolling:** IScrollableControl

---

## 5. Creating New Diagrams

### Step-by-Step Process
1. **Start with interface hierarchy** - Define what each interface does
2. **Add base classes** - Show abstract implementation patterns
3. **Add concrete controls** - Show real control implementations
4. **Add relationships** - Connect inheritance and interface implementation
5. **Test rendering** - Verify no syntax errors
6. **Add notes** - Document complex relationships

### Template for New Diagram
```mermaid
classDiagram
    note "Purpose: Document [what]"
    
    class IBaseInterface {
        <<interface>>
        +CoreMethod() type
    }
    
    class SpecializedInterface {
        <<interface>>
        +SpecializedMethod() type
    }
    SpecializedInterface --|> IBaseInterface
    
    class BaseImplementation {
        -InternalField field
        +ImplementedMethod() type
    }
    BaseImplementation --|> IBaseInterface
    
    class ConcreteControl {
    }
    ConcreteControl --|> BaseImplementation
    ConcreteControl --|> SpecializedInterface
```

### Validation Checklist
- [ ] All method signatures follow `+name(params) type` format
- [ ] No HTML tags in class names
- [ ] No nullable notation (`?`)
- [ ] No generic tildes (`~`)
- [ ] Interface markers use `<<interface>>`
- [ ] Relationships use proper arrows (`--|>`)
- [ ] Notes document complex relationships
- [ ] Diagram renders without errors
- [ ] Method count per class is reasonable (~10-15 max)

---

## 6. Troubleshooting Mermaid Errors

### Real-World Errors Encountered (January 2026)

**Error: Classes Don't Render or Render Blank**

**Root Cause:** Cross-diagram references
```mermaid
❌ WRONG - References class from different diagram:
classDiagram
    class ControlBase
    ControlBase --|> IControlObject  % IControlObject not defined in this diagram
```

**Solution:** Make each diagram self-contained
```mermaid
✅ CORRECT - Define all referenced classes:
classDiagram
    class IControlObject {
        <<interface>>
        +IsExists() bool
    }
    
    class ControlBase {
        -AppiumElement element
    }
    ControlBase --|> IControlObject
```

### Symptom: "Syntax error in text mermaid"
**Causes:**
- Invalid method signature (e.g., extra parentheses, special characters)
- Unclosed class definition
- Invalid relationship syntax
- Complex notes with special characters

**Fix:**
- Check all method signatures for proper format
- Ensure all `class Name { ... }` are closed
- Use `--|>` for inheritance, not `->` or other arrows
- Avoid notes with quotes or special formatting

### Symptom: Diagram renders blank
**Causes:**
- HTML/special characters in class names or notes
- Too many complex methods
- Invalid syntax preventing parse
- Cross-diagram references to undefined classes

**Fix:**
- Remove HTML tags from names (no `<br/>`)
- Move descriptions to separate document sections
- Simplify method signatures (reduce parameters)
- Test with simpler diagram first
- **Ensure all classes referenced are defined in the diagram**

### Symptom: Methods don't show
**Causes:**
- Methods defined outside class body
- Invalid method syntax
- Character encoding issues

**Fix:**
- Ensure methods are indented inside class blocks
- Use simple ASCII characters
- Avoid special symbols except standard operators

### Symptom: Relationships don't appear
**Causes:**
- Typo in class name reference
- Invalid arrow syntax
- Class not properly defined
- Referencing class from another diagram

**Fix:**
- Double-check class names (case-sensitive)
- Use `--|>` for inheritance arrows
- Ensure both classes are defined before relationship
- **Don't reference classes from other diagrams** - redefine them

---

### Best Practice: Self-Contained Diagrams

**Pattern:** Each diagram should work independently

```mermaid
✅ GOOD - Complete and standalone:
classDiagram
    direction TB
    
    class Base {
        -field type
        +method() return
    }
    
    class Derived {
    }
    Derived --|> Base
```

**Key Rules:**
1. **Define all referenced classes** - Don't assume classes from other diagrams exist
2. **Include only necessary methods** - 5-10 key methods per class
3. **Simplify method signatures** - Use simple types (List, string, bool, double)
4. **Add direction hint** - Use `direction TB` for clarity
5. **Remove complex notes** - Use document text instead
6. **Test in isolation** - Verify diagram renders alone before inclusion

---

## 7. Performance Considerations

### Diagram Size Limits
- **Small (good):** < 15 classes, < 100 methods total
- **Medium (acceptable):** 15-30 classes, 100-200 methods
- **Large (risky):** > 30 classes or > 200 methods
- **Split if:** Rendering takes > 5 seconds

### Optimization Tips
1. **Split large diagrams** - Create multiple focused diagrams
2. **Reduce method count** - Show only key methods
3. **Use inheritance** - Don't repeat methods in derived classes
4. **Simplify types** - Use `List` instead of `IReadOnlyList<T>`
5. **Remove redundancy** - Use notes instead of repeating info

### For Large Hierarchies
Instead of one giant diagram:
```
1. CORE-HIERARCHY.md: Interfaces only
2. MAUI-IMPLEMENTATION.md: MAUI controls
3. BLAZOR-IMPLEMENTATION.md: Blazor controls
4. CAPABILITY-MATRIX.md: Cross-reference table
5. METHOD-PATTERNS.md: Pattern examples
```

---

## 8. Examples from SPEC-002b

### Reference Implementation
See `specs/SPEC-002b-001-CONTROL-HIERARCHY-DIAGRAMS.md` for working examples of:
- ✅ Self-contained interface hierarchy diagram
- ✅ Self-contained MAUI implementation diagram  
- ✅ Self-contained Blazor implementation diagram
- ✅ Capability matrix diagram
- ✅ Method patterns diagram
- ✅ Container scoping diagram

### Real-World Fix Example

**Before (Broken):**
```mermaid
❌ WRONG - References undefined IControlObject:
classDiagram
    class ControlBase {
        -AppiumElement element
    }
    ControlBase --|> IControlObject
```

**After (Fixed):**
```mermaid
✅ CORRECT - All classes defined:
classDiagram
    direction TB
    
    class IControlObject {
        <<interface>>
        +IsExists() bool
        +IsVisible() bool
    }
    
    class ControlBase {
        -AppiumElement element
        -FindElement() AppiumElement
    }
    ControlBase --|> IControlObject
```

**Key Changes:**
- Defined `IControlObject` in the same diagram
- Added `<<interface>>` marker
- Included essential methods only
- Used `direction TB` for layout
- Removed complex parameter descriptions

### MAUI Implementation Example

```mermaid
classDiagram
    direction TB
    
    class ControlBase {
        -AppiumTestContext context
        -AppiumElement element
        -FindElement() AppiumElement
    }

    class TextControlBase {
        +Enter(string text) void
        +Clear() void
        +SetText(string) void
    }
    TextControlBase --|> ControlBase

    class EntryControl {
    }
    EntryControl --|> TextControlBase
```

**Pattern Notes:**
- Self-contained with all classes defined
- Clear inheritance hierarchy
- Simplified method signatures
- No HTML or special characters
- Direction hint for layout

---

## 9. When to Use Mermaid vs. Other Tools

### Use Mermaid for:
- ✅ Class hierarchies and inheritance
- ✅ Interface relationships
- ✅ Control implementation patterns
- ✅ Architecture overviews
- ✅ Method organization

### Use tables/lists for:
- ✅ Control capabilities matrix (summary)
- ✅ Property documentation
- ✅ Configuration options
- ✅ API reference (too detailed for diagrams)

### Use other tools for:
- Data flow diagrams → Flowchart
- Sequence of operations → Sequence diagram
- State machines → State diagram
- Entity relationships → ER diagram
- Timeline/roadmap → Gantt chart

---

## 10. Documentation Standards

### Every Mermaid Diagram Should Have

1. **Title/Note** - What does it show?
2. **Section header** - Where in the document?
3. **Legend/explanation** - What do symbols mean?
4. **Context** - How does it relate to other diagrams?
5. **Validation section** - What's documented?

### Example Header
```markdown
## 2. MAUI Control Implementation

### 2.1 Control Class Hierarchy

This diagram shows the inheritance relationships for MAUI platform controls,
including base classes and 17+ concrete control implementations.

\`\`\`mermaid
classDiagram
    note "MAUI Control Implementation Hierarchy"
    ...
\`\`\`

---

## Notes on this Diagram

- ControlBase implements IControlObject interface
- Base classes provide common functionality
- Concrete controls inherit from appropriate base classes
- Multiple inheritance used for interface implementation
```

---

## 11. Future Updates

### Lessons Learned (January 3, 2026)

During SPEC-002b creation, several critical Mermaid issues were discovered and fixed:

1. **Cross-Diagram References Fail** - Cannot reference classes from other diagrams. Each diagram must be self-contained with all referenced classes defined.

2. **Complex Notes Break Rendering** - Notes with special characters or quotes cause parse errors. Use simple text or move complex descriptions to document text.

3. **Self-Contained Diagrams are Essential** - Every diagram must work independently. Don't assume classes from prior diagrams exist.

4. **Direction Hints Help** - Adding `direction TB` clarifies layout and can help prevent rendering issues.

5. **Simplified Method Signatures Work Better** - Complex parameter descriptions cause problems. Use simple types and descriptions in separate documentation.

6. **HTML in Names Must Go** - Never use `<br/>` or other HTML tags in class labels. Use `<<interface>>` markers and notes instead.

### When to Revise This Document
- [ ] New Mermaid version released (check syntax compatibility)
- [ ] New diagram type needed (add examples)
- [ ] New pattern discovered (document it)
- [ ] Common error appears (add troubleshooting)

### Maintenance Schedule
- **Quarterly:** Check Mermaid version and breaking changes
- **Per release:** Update examples with new controls
- **Per document:** Validate diagrams render correctly
- **Per fix:** Document the error and solution here

---

## Quick Reference Card

### Most Common Syntax
```mermaid
classDiagram
    class IExample {
        <<interface>>
        +Method(param) return
        #ProtectedMethod() return
        -PrivateMethod() return
    }
    
    class Implementation {
    }
    Implementation --|> IExample
```

### What Works
- `class Name { ... }`
- `<<interface>>`
- `+method() type`
- `--|>` (inheritance)
- `note for Class "text"`

### What Doesn't Work
- `class Name["Name<br/>"]`
- `~T~` (generics)
- `method() type?` (nullable)
- `Type1? Type2` (nullable fields)
- `->` (wrong arrow)

---

**Version:** 1.0  
**Status:** Active  
**Last Review:** January 3, 2026  
**Next Review:** April 3, 2026

For questions about Mermaid syntax, consult:
- [Official Mermaid Docs](https://mermaid.js.org/)
- [Class Diagram Reference](https://mermaid.js.org/syntax/classDiagram.html)
- `SPEC-002b-001-CONTROL-HIERARCHY-DIAGRAMS.md` (reference implementation)

<!-- GSD:BEGIN -->
# GSD Project Conventions

## Project Detection
If a `.planning/` directory exists in the workspace root, this is a GSD-managed project. All GSD rules below apply.

## STATE.md First Rule  
**Before performing ANY GSD operation, ALWAYS read `.planning/STATE.md` first.** This file contains:
- Current milestone, phase, and plan position
- Active blockers and decisions
- Session context and progress

## File Conventions
- `.planning/STATE.md` — Current project position and context
- `.planning/PROJECT.md` — Project definition and vision
- `.planning/REQUIREMENTS.md` — Requirement specifications with REQ-IDs
- `.planning/ROADMAP.md` — Phase-based execution roadmap
- `.planning/config.json` — Workflow configuration
- `.planning/phases/{NN}-{name}/` — Phase working directories
  - `{NN}-CONTEXT.md` — User decisions for this phase
  - `{NN}-RESEARCH.md` — Research findings
  - `{NN}-{MM}-PLAN.md` — Execution plans
  - `{NN}-{MM}-SUMMARY.md` — Execution results
  - `{NN}-VALIDATION.md` — Plan verification results
  - `{NN}-VERIFICATION.md` — Post-execution verification
  - `{NN}-UAT.md` — User acceptance testing
- `.planning/quick/` — Quick task directory
- `.planning/codebase/` — Codebase analysis docs
- `.planning/milestones/` — Archived milestones

## Commit Conventions
Use conventional commits: `{type}({scope}): {description}`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`, `ci`
Scope: derived from the component being changed

For planning docs: `docs(planning): {description}`

## Context Fidelity
- **Never invent requirements.** Work only from ROADMAP.md phase goals and PLAN.md tasks.
- **Never assume technology choices.** Check CONTEXT.md and PROJECT.md first.
- **Never skip verification.** Every claim in SUMMARY.md must be verifiable against actual code.

## Planning Doc Format
All `.planning/` markdown files use YAML frontmatter:
```yaml
---
key: value
---
```
Do not modify frontmatter manually — use `gsd_frontmatter_set` MCP tool.

## MCP Tools
GSD provides MCP tools prefixed with `gsd_`. Use these for all state management, config, roadmap, and phase operations instead of manual file editing. Key tools:
- `gsd_state_load` / `gsd_state_update` — State management
- `gsd_config_load` / `gsd_config_set` — Configuration
- `gsd_roadmap_analyze` / `gsd_roadmap_get_phase` — Roadmap queries
- `gsd_commit` — Atomic commits with planning doc tracking
- `gsd_find_phase` — Phase directory discovery

## GSD Commands
Use `/gsd-{command}` to invoke GSD prompts:

### Project Lifecycle
- `/gsd-project-new` — Initialize a new GSD project
- `/gsd-codebase-map` — Analyze existing codebase into structured docs
- `/gsd-progress` — Check project status and route to next action
- `/gsd-milestone-new` — Start a new milestone

### Phase Workflow
- `/gsd-phase-discuss N` — Gather preferences and decisions for phase N
- `/gsd-phase-plan N` — Research and plan phase N
- `/gsd-phase-execute N` — Execute phase N plans with atomic commits
- `/gsd-phase-verify N` — Interactive UAT verification for phase N
- `/gsd-phase-research N` — Deep standalone research for phase N

### Phase Management
- `/gsd-phase-add "description"` — Append phase to roadmap
- `/gsd-phase-remove N` — Remove a future phase
- `/gsd-phase-insert N "description"` — Insert urgent work
- `/gsd-phase-list-assumptions N` — List assumptions for phase N

### Quick Operations
- `/gsd-quick "description"` — Execute a quick task with GSD guarantees
- `/gsd-debug "description"` — Scientific debugging with persistent sessions
- `/gsd-todo-add "description"` — Capture task for later
- `/gsd-todo-check` — List pending todos

### Milestone Completion
- `/gsd-milestone-audit` — Audit milestone completeness and integration
- `/gsd-milestone-complete` — Archive and complete current milestone
- `/gsd-milestone-plan-gaps` — Plan fixes for audit gaps

### Session & Config
- `/gsd-work-pause` — Save context for later
- `/gsd-work-resume` — Resume from previous session
- `/gsd-settings` — Configure GSD workflow
- `/gsd-profile-set [quality|balanced|budget]` — Switch model profile
- `/gsd-health` — Check project health and consistency
- `/gsd-cleanup` — Clean stale planning files
- `/gsd-update` — Check for updates
- `/gsd-help` — Show all commands

## Context Management
If the conversation is getting long, consider using `/gsd-work-pause` to save state and start a fresh session with `/gsd-work-resume`.

<!-- GSD:BEGIN -->
# GSD Project Conventions

## Project Detection
If a `.planning/` directory exists in the workspace root, this is a GSD-managed project. All GSD rules below apply.

## STATE.md First Rule  
**Before performing ANY GSD operation, ALWAYS read `.planning/STATE.md` first.** This file contains:
- Current milestone, phase, and plan position
- Active blockers and decisions
- Session context and progress

## File Conventions
- `.planning/STATE.md` — Current project position and context
- `.planning/PROJECT.md` — Project definition and vision
- `.planning/REQUIREMENTS.md` — Requirement specifications with REQ-IDs
- `.planning/ROADMAP.md` — Phase-based execution roadmap
- `.planning/config.json` — Workflow configuration
- `.planning/phases/{NN}-{name}/` — Phase working directories
  - `{NN}-CONTEXT.md` — User decisions for this phase
  - `{NN}-RESEARCH.md` — Research findings
  - `{NN}-{MM}-PLAN.md` — Execution plans
  - `{NN}-{MM}-SUMMARY.md` — Execution results
  - `{NN}-VALIDATION.md` — Plan verification results
  - `{NN}-VERIFICATION.md` — Post-execution verification
  - `{NN}-UAT.md` — User acceptance testing
- `.planning/quick/` — Quick task directory
- `.planning/codebase/` — Codebase analysis docs
- `.planning/milestones/` — Archived milestones

## Commit Conventions
Use conventional commits: `{type}({scope}): {description}`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`, `ci`
Scope: derived from the component being changed

For planning docs: `docs(planning): {description}`

## Context Fidelity
- **Never invent requirements.** Work only from ROADMAP.md phase goals and PLAN.md tasks.
- **Never assume technology choices.** Check CONTEXT.md and PROJECT.md first.
- **Never skip verification.** Every claim in SUMMARY.md must be verifiable against actual code.

## Planning Doc Format
All `.planning/` markdown files use YAML frontmatter:
```yaml
---
key: value
---
```
Do not modify frontmatter manually — use `gsd_frontmatter_set` MCP tool.

## MCP Tools
GSD provides MCP tools prefixed with `gsd_`. Use these for all state management, config, roadmap, and phase operations instead of manual file editing. Key tools:
- `gsd_state_load` / `gsd_state_update` — State management
- `gsd_config_load` / `gsd_config_set` — Configuration
- `gsd_roadmap_analyze` / `gsd_roadmap_get_phase` — Roadmap queries
- `gsd_commit` — Atomic commits with planning doc tracking
- `gsd_find_phase` — Phase directory discovery

## GSD Commands
Use `/gsd-{command}` to invoke GSD prompts:

### Project Lifecycle
- `/gsd-project-new` — Initialize a new GSD project
- `/gsd-codebase-map` — Analyze existing codebase into structured docs
- `/gsd-progress` — Check project status and route to next action
- `/gsd-milestone-new` — Start a new milestone

### Phase Workflow
- `/gsd-phase-discuss N` — Gather preferences and decisions for phase N
- `/gsd-phase-plan N` — Research and plan phase N
- `/gsd-phase-execute N` — Execute phase N plans with atomic commits
- `/gsd-phase-verify N` — Interactive UAT verification for phase N
- `/gsd-phase-research N` — Deep standalone research for phase N

### Phase Management
- `/gsd-phase-add "description"` — Append phase to roadmap
- `/gsd-phase-remove N` — Remove a future phase
- `/gsd-phase-insert N "description"` — Insert urgent work
- `/gsd-phase-list-assumptions N` — List assumptions for phase N

### Quick Operations
- `/gsd-quick "description"` — Execute a quick task with GSD guarantees
- `/gsd-debug "description"` — Scientific debugging with persistent sessions
- `/gsd-todo-add "description"` — Capture task for later
- `/gsd-todo-check` — List pending todos

### Milestone Completion
- `/gsd-milestone-audit` — Audit milestone completeness and integration
- `/gsd-milestone-complete` — Archive and complete current milestone
- `/gsd-milestone-plan-gaps` — Plan fixes for audit gaps

### Session & Config
- `/gsd-work-pause` — Save context for later
- `/gsd-work-resume` — Resume from previous session
- `/gsd-settings` — Configure GSD workflow
- `/gsd-profile-set [quality|balanced|budget]` — Switch model profile
- `/gsd-health` — Check project health and consistency
- `/gsd-cleanup` — Clean stale planning files
- `/gsd-update` — Check for updates
- `/gsd-help` — Show all commands

## Context Management
If the conversation is getting long, consider using `/gsd-work-pause` to save state and start a fresh session with `/gsd-work-resume`.

<!-- GSD:BEGIN -->
# GSD Project Conventions

## Project Detection
If a `.planning/` directory exists in the workspace root, this is a GSD-managed project. All GSD rules below apply.

## STATE.md First Rule  
**Before performing ANY GSD operation, ALWAYS read `.planning/STATE.md` first.** This file contains:
- Current milestone, phase, and plan position
- Active blockers and decisions
- Session context and progress

## File Conventions
- `.planning/STATE.md` — Current project position and context
- `.planning/PROJECT.md` — Project definition and vision
- `.planning/REQUIREMENTS.md` — Requirement specifications with REQ-IDs
- `.planning/ROADMAP.md` — Phase-based execution roadmap
- `.planning/config.json` — Workflow configuration
- `.planning/phases/{NN}-{name}/` — Phase working directories
  - `{NN}-CONTEXT.md` — User decisions for this phase
  - `{NN}-RESEARCH.md` — Research findings
  - `{NN}-{MM}-PLAN.md` — Execution plans
  - `{NN}-{MM}-SUMMARY.md` — Execution results
  - `{NN}-VALIDATION.md` — Plan verification results
  - `{NN}-VERIFICATION.md` — Post-execution verification
  - `{NN}-UAT.md` — User acceptance testing
- `.planning/quick/` — Quick task directory
- `.planning/codebase/` — Codebase analysis docs
- `.planning/milestones/` — Archived milestones

## Commit Conventions
Use conventional commits: `{type}({scope}): {description}`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`, `ci`
Scope: derived from the component being changed

For planning docs: `docs(planning): {description}`

## Context Fidelity
- **Never invent requirements.** Work only from ROADMAP.md phase goals and PLAN.md tasks.
- **Never assume technology choices.** Check CONTEXT.md and PROJECT.md first.
- **Never skip verification.** Every claim in SUMMARY.md must be verifiable against actual code.

## Planning Doc Format
All `.planning/` markdown files use YAML frontmatter:
```yaml
---
key: value
---
```
Do not modify frontmatter manually — use `gsd_frontmatter_set` MCP tool.

## MCP Tools
GSD provides MCP tools prefixed with `gsd_`. Use these for all state management, config, roadmap, and phase operations instead of manual file editing. Key tools:
- `gsd_state_load` / `gsd_state_update` — State management
- `gsd_config_load` / `gsd_config_set` — Configuration
- `gsd_roadmap_analyze` / `gsd_roadmap_get_phase` — Roadmap queries
- `gsd_commit` — Atomic commits with planning doc tracking
- `gsd_find_phase` — Phase directory discovery

## GSD Commands
Use `/gsd-{command}` to invoke GSD prompts:

### Project Lifecycle
- `/gsd-project-new` — Initialize a new GSD project
- `/gsd-codebase-map` — Analyze existing codebase into structured docs
- `/gsd-progress` — Check project status and route to next action
- `/gsd-milestone-new` — Start a new milestone

### Phase Workflow
- `/gsd-phase-discuss N` — Gather preferences and decisions for phase N
- `/gsd-phase-plan N` — Research and plan phase N
- `/gsd-phase-execute N` — Execute phase N plans with atomic commits
- `/gsd-phase-verify N` — Interactive UAT verification for phase N
- `/gsd-phase-research N` — Deep standalone research for phase N

### Phase Management
- `/gsd-phase-add "description"` — Append phase to roadmap
- `/gsd-phase-remove N` — Remove a future phase
- `/gsd-phase-insert N "description"` — Insert urgent work
- `/gsd-phase-list-assumptions N` — List assumptions for phase N

### Quick Operations
- `/gsd-quick "description"` — Execute a quick task with GSD guarantees
- `/gsd-debug "description"` — Scientific debugging with persistent sessions
- `/gsd-todo-add "description"` — Capture task for later
- `/gsd-todo-check` — List pending todos

### Milestone Completion
- `/gsd-milestone-audit` — Audit milestone completeness and integration
- `/gsd-milestone-complete` — Archive and complete current milestone
- `/gsd-milestone-plan-gaps` — Plan fixes for audit gaps

### Session & Config
- `/gsd-work-pause` — Save context for later
- `/gsd-work-resume` — Resume from previous session
- `/gsd-settings` — Configure GSD workflow
- `/gsd-profile-set [quality|balanced|budget]` — Switch model profile
- `/gsd-health` — Check project health and consistency
- `/gsd-cleanup` — Clean stale planning files
- `/gsd-update` — Check for updates
- `/gsd-help` — Show all commands

## Context Management
If the conversation is getting long, consider using `/gsd-work-pause` to save state and start a fresh session with `/gsd-work-resume`.

<!-- GSD:BEGIN -->
# GSD Project Conventions

## Project Detection
If a `.planning/` directory exists in the workspace root, this is a GSD-managed project. All GSD rules below apply.

## STATE.md First Rule  
**Before performing ANY GSD operation, ALWAYS read `.planning/STATE.md` first.** This file contains:
- Current milestone, phase, and plan position
- Active blockers and decisions
- Session context and progress

## File Conventions
- `.planning/STATE.md` — Current project position and context
- `.planning/PROJECT.md` — Project definition and vision
- `.planning/REQUIREMENTS.md` — Requirement specifications with REQ-IDs
- `.planning/ROADMAP.md` — Phase-based execution roadmap
- `.planning/config.json` — Workflow configuration
- `.planning/phases/{NN}-{name}/` — Phase working directories
  - `{NN}-CONTEXT.md` — User decisions for this phase
  - `{NN}-RESEARCH.md` — Research findings
  - `{NN}-{MM}-PLAN.md` — Execution plans
  - `{NN}-{MM}-SUMMARY.md` — Execution results
  - `{NN}-VALIDATION.md` — Plan verification results
  - `{NN}-VERIFICATION.md` — Post-execution verification
  - `{NN}-UAT.md` — User acceptance testing
- `.planning/quick/` — Quick task directory
- `.planning/codebase/` — Codebase analysis docs
- `.planning/milestones/` — Archived milestones

## Commit Conventions
Use conventional commits: `{type}({scope}): {description}`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`, `ci`
Scope: derived from the component being changed

For planning docs: `docs(planning): {description}`

## Context Fidelity
- **Never invent requirements.** Work only from ROADMAP.md phase goals and PLAN.md tasks.
- **Never assume technology choices.** Check CONTEXT.md and PROJECT.md first.
- **Never skip verification.** Every claim in SUMMARY.md must be verifiable against actual code.

## Planning Doc Format
All `.planning/` markdown files use YAML frontmatter:
```yaml
---
key: value
---
```
Do not modify frontmatter manually — use `gsd_frontmatter_set` MCP tool.

## MCP Tools
GSD provides MCP tools prefixed with `gsd_`. Use these for all state management, config, roadmap, and phase operations instead of manual file editing. Key tools:
- `gsd_state_load` / `gsd_state_update` — State management
- `gsd_config_load` / `gsd_config_set` — Configuration
- `gsd_roadmap_analyze` / `gsd_roadmap_get_phase` — Roadmap queries
- `gsd_commit` — Atomic commits with planning doc tracking
- `gsd_find_phase` — Phase directory discovery

## GSD Commands
Use `/gsd-{command}` to invoke GSD prompts:

### Project Lifecycle
- `/gsd-project-new` — Initialize a new GSD project
- `/gsd-codebase-map` — Analyze existing codebase into structured docs
- `/gsd-progress` — Check project status and route to next action
- `/gsd-milestone-new` — Start a new milestone

### Phase Workflow
- `/gsd-phase-discuss N` — Gather preferences and decisions for phase N
- `/gsd-phase-plan N` — Research and plan phase N
- `/gsd-phase-execute N` — Execute phase N plans with atomic commits
- `/gsd-phase-verify N` — Interactive UAT verification for phase N
- `/gsd-phase-research N` — Deep standalone research for phase N

### Phase Management
- `/gsd-phase-add "description"` — Append phase to roadmap
- `/gsd-phase-remove N` — Remove a future phase
- `/gsd-phase-insert N "description"` — Insert urgent work
- `/gsd-phase-list-assumptions N` — List assumptions for phase N

### Quick Operations
- `/gsd-quick "description"` — Execute a quick task with GSD guarantees
- `/gsd-debug "description"` — Scientific debugging with persistent sessions
- `/gsd-todo-add "description"` — Capture task for later
- `/gsd-todo-check` — List pending todos

### Milestone Completion
- `/gsd-milestone-audit` — Audit milestone completeness and integration
- `/gsd-milestone-complete` — Archive and complete current milestone
- `/gsd-milestone-plan-gaps` — Plan fixes for audit gaps

### Session & Config
- `/gsd-work-pause` — Save context for later
- `/gsd-work-resume` — Resume from previous session
- `/gsd-settings` — Configure GSD workflow
- `/gsd-profile-set [quality|balanced|budget]` — Switch model profile
- `/gsd-health` — Check project health and consistency
- `/gsd-cleanup` — Clean stale planning files
- `/gsd-update` — Check for updates
- `/gsd-help` — Show all commands

## Context Management
If the conversation is getting long, consider using `/gsd-work-pause` to save state and start a fresh session with `/gsd-work-resume`.

<!-- GSD:BEGIN -->
# GSD Project Conventions

## Project Detection
If a `.planning/` directory exists in the workspace root, this is a GSD-managed project. All GSD rules below apply.

## STATE.md First Rule  
**Before performing ANY GSD operation, ALWAYS read `.planning/STATE.md` first.** This file contains:
- Current milestone, phase, and plan position
- Active blockers and decisions
- Session context and progress

## File Conventions
- `.planning/STATE.md` — Current project position and context
- `.planning/PROJECT.md` — Project definition and vision
- `.planning/REQUIREMENTS.md` — Requirement specifications with REQ-IDs
- `.planning/ROADMAP.md` — Phase-based execution roadmap
- `.planning/config.json` — Workflow configuration
- `.planning/phases/{NN}-{name}/` — Phase working directories
  - `{NN}-CONTEXT.md` — User decisions for this phase
  - `{NN}-RESEARCH.md` — Research findings
  - `{NN}-{MM}-PLAN.md` — Execution plans
  - `{NN}-{MM}-SUMMARY.md` — Execution results
  - `{NN}-VALIDATION.md` — Plan verification results
  - `{NN}-VERIFICATION.md` — Post-execution verification
  - `{NN}-UAT.md` — User acceptance testing
- `.planning/quick/` — Quick task directory
- `.planning/codebase/` — Codebase analysis docs
- `.planning/milestones/` — Archived milestones

## Commit Conventions
Use conventional commits: `{type}({scope}): {description}`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`, `ci`
Scope: derived from the component being changed

For planning docs: `docs(planning): {description}`

## Context Fidelity
- **Never invent requirements.** Work only from ROADMAP.md phase goals and PLAN.md tasks.
- **Never assume technology choices.** Check CONTEXT.md and PROJECT.md first.
- **Never skip verification.** Every claim in SUMMARY.md must be verifiable against actual code.

## Planning Doc Format
All `.planning/` markdown files use YAML frontmatter:
```yaml
---
key: value
---
```
Do not modify frontmatter manually — use `gsd_frontmatter_set` MCP tool.

## MCP Tools
GSD provides MCP tools prefixed with `gsd_`. Use these for all state management, config, roadmap, and phase operations instead of manual file editing. Key tools:
- `gsd_state_load` / `gsd_state_update` — State management
- `gsd_config_load` / `gsd_config_set` — Configuration
- `gsd_roadmap_analyze` / `gsd_roadmap_get_phase` — Roadmap queries
- `gsd_commit` — Atomic commits with planning doc tracking
- `gsd_find_phase` — Phase directory discovery

## GSD Commands
Use `/gsd-{command}` to invoke GSD prompts:

### Project Lifecycle
- `/gsd-project-new` — Initialize a new GSD project
- `/gsd-codebase-map` — Analyze existing codebase into structured docs
- `/gsd-progress` — Check project status and route to next action
- `/gsd-milestone-new` — Start a new milestone

### Phase Workflow
- `/gsd-phase-discuss N` — Gather preferences and decisions for phase N
- `/gsd-phase-plan N` — Research and plan phase N
- `/gsd-phase-execute N` — Execute phase N plans with atomic commits
- `/gsd-phase-verify N` — Interactive UAT verification for phase N
- `/gsd-phase-research N` — Deep standalone research for phase N

### Phase Management
- `/gsd-phase-add "description"` — Append phase to roadmap
- `/gsd-phase-remove N` — Remove a future phase
- `/gsd-phase-insert N "description"` — Insert urgent work
- `/gsd-phase-list-assumptions N` — List assumptions for phase N

### Quick Operations
- `/gsd-quick "description"` — Execute a quick task with GSD guarantees
- `/gsd-debug "description"` — Scientific debugging with persistent sessions
- `/gsd-todo-add "description"` — Capture task for later
- `/gsd-todo-check` — List pending todos

### Milestone Completion
- `/gsd-milestone-audit` — Audit milestone completeness and integration
- `/gsd-milestone-complete` — Archive and complete current milestone
- `/gsd-milestone-plan-gaps` — Plan fixes for audit gaps

### Session & Config
- `/gsd-work-pause` — Save context for later
- `/gsd-work-resume` — Resume from previous session
- `/gsd-settings` — Configure GSD workflow
- `/gsd-profile-set [quality|balanced|budget]` — Switch model profile
- `/gsd-health` — Check project health and consistency
- `/gsd-cleanup` — Clean stale planning files
- `/gsd-update` — Check for updates
- `/gsd-help` — Show all commands

## Context Management
If the conversation is getting long, consider using `/gsd-work-pause` to save state and start a fresh session with `/gsd-work-resume`.

<!-- GSD:BEGIN -->
# GSD Project Conventions

## Project Detection
If a `.planning/` directory exists in the workspace root, this is a GSD-managed project. All GSD rules below apply.

## STATE.md First Rule  
**Before performing ANY GSD operation, ALWAYS read `.planning/STATE.md` first.** This file contains:
- Current milestone, phase, and plan position
- Active blockers and decisions
- Session context and progress

## File Conventions
- `.planning/STATE.md` — Current project position and context
- `.planning/PROJECT.md` — Project definition and vision
- `.planning/REQUIREMENTS.md` — Requirement specifications with REQ-IDs
- `.planning/ROADMAP.md` — Phase-based execution roadmap
- `.planning/config.json` — Workflow configuration
- `.planning/phases/{NN}-{name}/` — Phase working directories
  - `{NN}-CONTEXT.md` — User decisions for this phase
  - `{NN}-RESEARCH.md` — Research findings
  - `{NN}-{MM}-PLAN.md` — Execution plans
  - `{NN}-{MM}-SUMMARY.md` — Execution results
  - `{NN}-VALIDATION.md` — Plan verification results
  - `{NN}-VERIFICATION.md` — Post-execution verification
  - `{NN}-UAT.md` — User acceptance testing
- `.planning/quick/` — Quick task directory
- `.planning/codebase/` — Codebase analysis docs
- `.planning/milestones/` — Archived milestones

## Commit Conventions
Use conventional commits: `{type}({scope}): {description}`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`, `ci`
Scope: derived from the component being changed

For planning docs: `docs(planning): {description}`

## Context Fidelity
- **Never invent requirements.** Work only from ROADMAP.md phase goals and PLAN.md tasks.
- **Never assume technology choices.** Check CONTEXT.md and PROJECT.md first.
- **Never skip verification.** Every claim in SUMMARY.md must be verifiable against actual code.

## Planning Doc Format
All `.planning/` markdown files use YAML frontmatter:
```yaml
---
key: value
---
```
Do not modify frontmatter manually — use `gsd_frontmatter_set` MCP tool.

## MCP Tools
GSD provides MCP tools prefixed with `gsd_`. Use these for all state management, config, roadmap, and phase operations instead of manual file editing. Key tools:
- `gsd_state_load` / `gsd_state_update` — State management
- `gsd_config_load` / `gsd_config_set` — Configuration
- `gsd_roadmap_analyze` / `gsd_roadmap_get_phase` — Roadmap queries
- `gsd_commit` — Atomic commits with planning doc tracking
- `gsd_find_phase` — Phase directory discovery

## GSD Commands
Use `/gsd-{command}` to invoke GSD prompts:

### Project Lifecycle
- `/gsd-project-new` — Initialize a new GSD project
- `/gsd-codebase-map` — Analyze existing codebase into structured docs
- `/gsd-progress` — Check project status and route to next action
- `/gsd-milestone-new` — Start a new milestone

### Phase Workflow
- `/gsd-phase-discuss N` — Gather preferences and decisions for phase N
- `/gsd-phase-plan N` — Research and plan phase N
- `/gsd-phase-execute N` — Execute phase N plans with atomic commits
- `/gsd-phase-verify N` — Interactive UAT verification for phase N
- `/gsd-phase-research N` — Deep standalone research for phase N

### Phase Management
- `/gsd-phase-add "description"` — Append phase to roadmap
- `/gsd-phase-remove N` — Remove a future phase
- `/gsd-phase-insert N "description"` — Insert urgent work
- `/gsd-phase-list-assumptions N` — List assumptions for phase N

### Quick Operations
- `/gsd-quick "description"` — Execute a quick task with GSD guarantees
- `/gsd-debug "description"` — Scientific debugging with persistent sessions
- `/gsd-todo-add "description"` — Capture task for later
- `/gsd-todo-check` — List pending todos

### Milestone Completion
- `/gsd-milestone-audit` — Audit milestone completeness and integration
- `/gsd-milestone-complete` — Archive and complete current milestone
- `/gsd-milestone-plan-gaps` — Plan fixes for audit gaps

### Session & Config
- `/gsd-work-pause` — Save context for later
- `/gsd-work-resume` — Resume from previous session
- `/gsd-settings` — Configure GSD workflow
- `/gsd-profile-set [quality|balanced|budget]` — Switch model profile
- `/gsd-health` — Check project health and consistency
- `/gsd-cleanup` — Clean stale planning files
- `/gsd-update` — Check for updates
- `/gsd-help` — Show all commands

## Context Management
If the conversation is getting long, consider using `/gsd-work-pause` to save state and start a fresh session with `/gsd-work-resume`.

<!-- GSD:BEGIN -->
# GSD Project Conventions

## Project Detection
If a `.planning/` directory exists in the workspace root, this is a GSD-managed project. All GSD rules below apply.

## STATE.md First Rule  
**Before performing ANY GSD operation, ALWAYS read `.planning/STATE.md` first.** This file contains:
- Current milestone, phase, and plan position
- Active blockers and decisions
- Session context and progress

## File Conventions
- `.planning/STATE.md` — Current project position and context
- `.planning/PROJECT.md` — Project definition and vision
- `.planning/REQUIREMENTS.md` — Requirement specifications with REQ-IDs
- `.planning/ROADMAP.md` — Phase-based execution roadmap
- `.planning/config.json` — Workflow configuration
- `.planning/phases/{NN}-{name}/` — Phase working directories
  - `{NN}-CONTEXT.md` — User decisions for this phase
  - `{NN}-RESEARCH.md` — Research findings
  - `{NN}-{MM}-PLAN.md` — Execution plans
  - `{NN}-{MM}-SUMMARY.md` — Execution results
  - `{NN}-VALIDATION.md` — Plan verification results
  - `{NN}-VERIFICATION.md` — Post-execution verification
  - `{NN}-UAT.md` — User acceptance testing
- `.planning/quick/` — Quick task directory
- `.planning/codebase/` — Codebase analysis docs
- `.planning/milestones/` — Archived milestones

## Commit Conventions
Use conventional commits: `{type}({scope}): {description}`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`, `ci`
Scope: derived from the component being changed

For planning docs: `docs(planning): {description}`

## Context Fidelity
- **Never invent requirements.** Work only from ROADMAP.md phase goals and PLAN.md tasks.
- **Never assume technology choices.** Check CONTEXT.md and PROJECT.md first.
- **Never skip verification.** Every claim in SUMMARY.md must be verifiable against actual code.

## Planning Doc Format
All `.planning/` markdown files use YAML frontmatter:
```yaml
---
key: value
---
```
Do not modify frontmatter manually — use `gsd_frontmatter_set` MCP tool.

## MCP Tools
GSD provides MCP tools prefixed with `gsd_`. Use these for all state management, config, roadmap, and phase operations instead of manual file editing. Key tools:
- `gsd_state_load` / `gsd_state_update` — State management
- `gsd_config_load` / `gsd_config_set` — Configuration
- `gsd_roadmap_analyze` / `gsd_roadmap_get_phase` — Roadmap queries
- `gsd_commit` — Atomic commits with planning doc tracking
- `gsd_find_phase` — Phase directory discovery

## GSD Commands
Use `/gsd-{command}` to invoke GSD prompts:

### Project Lifecycle
- `/gsd-project-new` — Initialize a new GSD project
- `/gsd-codebase-map` — Analyze existing codebase into structured docs
- `/gsd-progress` — Check project status and route to next action
- `/gsd-milestone-new` — Start a new milestone

### Phase Workflow
- `/gsd-phase-discuss N` — Gather preferences and decisions for phase N
- `/gsd-phase-plan N` — Research and plan phase N
- `/gsd-phase-execute N` — Execute phase N plans with atomic commits
- `/gsd-phase-verify N` — Interactive UAT verification for phase N
- `/gsd-phase-research N` — Deep standalone research for phase N

### Phase Management
- `/gsd-phase-add "description"` — Append phase to roadmap
- `/gsd-phase-remove N` — Remove a future phase
- `/gsd-phase-insert N "description"` — Insert urgent work
- `/gsd-phase-list-assumptions N` — List assumptions for phase N

### Quick Operations
- `/gsd-quick "description"` — Execute a quick task with GSD guarantees
- `/gsd-debug "description"` — Scientific debugging with persistent sessions
- `/gsd-todo-add "description"` — Capture task for later
- `/gsd-todo-check` — List pending todos

### Milestone Completion
- `/gsd-milestone-audit` — Audit milestone completeness and integration
- `/gsd-milestone-complete` — Archive and complete current milestone
- `/gsd-milestone-plan-gaps` — Plan fixes for audit gaps

### Session & Config
- `/gsd-work-pause` — Save context for later
- `/gsd-work-resume` — Resume from previous session
- `/gsd-settings` — Configure GSD workflow
- `/gsd-profile-set [quality|balanced|budget]` — Switch model profile
- `/gsd-health` — Check project health and consistency
- `/gsd-cleanup` — Clean stale planning files
- `/gsd-update` — Check for updates
- `/gsd-help` — Show all commands

## Context Management
If the conversation is getting long, consider using `/gsd-work-pause` to save state and start a fresh session with `/gsd-work-resume`.

<!-- GSD:BEGIN -->
# GSD Project Conventions

## Project Detection
If a `.planning/` directory exists in the workspace root, this is a GSD-managed project. All GSD rules below apply.

## STATE.md First Rule  
**Before performing ANY GSD operation, ALWAYS read `.planning/STATE.md` first.** This file contains:
- Current milestone, phase, and plan position
- Active blockers and decisions
- Session context and progress

## File Conventions
- `.planning/STATE.md` — Current project position and context
- `.planning/PROJECT.md` — Project definition and vision
- `.planning/REQUIREMENTS.md` — Requirement specifications with REQ-IDs
- `.planning/ROADMAP.md` — Phase-based execution roadmap
- `.planning/config.json` — Workflow configuration
- `.planning/phases/{NN}-{name}/` — Phase working directories
  - `{NN}-CONTEXT.md` — User decisions for this phase
  - `{NN}-RESEARCH.md` — Research findings
  - `{NN}-{MM}-PLAN.md` — Execution plans
  - `{NN}-{MM}-SUMMARY.md` — Execution results
  - `{NN}-VALIDATION.md` — Plan verification results
  - `{NN}-VERIFICATION.md` — Post-execution verification
  - `{NN}-UAT.md` — User acceptance testing
- `.planning/quick/` — Quick task directory
- `.planning/codebase/` — Codebase analysis docs
- `.planning/milestones/` — Archived milestones

## Commit Conventions
Use conventional commits: `{type}({scope}): {description}`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`, `ci`
Scope: derived from the component being changed

For planning docs: `docs(planning): {description}`

## Context Fidelity
- **Never invent requirements.** Work only from ROADMAP.md phase goals and PLAN.md tasks.
- **Never assume technology choices.** Check CONTEXT.md and PROJECT.md first.
- **Never skip verification.** Every claim in SUMMARY.md must be verifiable against actual code.

## Planning Doc Format
All `.planning/` markdown files use YAML frontmatter:
```yaml
---
key: value
---
```
Do not modify frontmatter manually — use `gsd_frontmatter_set` MCP tool.

## MCP Tools
GSD provides MCP tools prefixed with `gsd_`. Use these for all state management, config, roadmap, and phase operations instead of manual file editing. Key tools:
- `gsd_state_load` / `gsd_state_update` — State management
- `gsd_config_load` / `gsd_config_set` — Configuration
- `gsd_roadmap_analyze` / `gsd_roadmap_get_phase` — Roadmap queries
- `gsd_commit` — Atomic commits with planning doc tracking
- `gsd_find_phase` — Phase directory discovery

## GSD Commands
Use `/gsd-{command}` to invoke GSD prompts:

### Project Lifecycle
- `/gsd-project-new` — Initialize a new GSD project
- `/gsd-codebase-map` — Analyze existing codebase into structured docs
- `/gsd-progress` — Check project status and route to next action
- `/gsd-milestone-new` — Start a new milestone

### Phase Workflow
- `/gsd-phase-discuss N` — Gather preferences and decisions for phase N
- `/gsd-phase-plan N` — Research and plan phase N
- `/gsd-phase-execute N` — Execute phase N plans with atomic commits
- `/gsd-phase-verify N` — Interactive UAT verification for phase N
- `/gsd-phase-research N` — Deep standalone research for phase N

### Phase Management
- `/gsd-phase-add "description"` — Append phase to roadmap
- `/gsd-phase-remove N` — Remove a future phase
- `/gsd-phase-insert N "description"` — Insert urgent work
- `/gsd-phase-list-assumptions N` — List assumptions for phase N

### Quick Operations
- `/gsd-quick "description"` — Execute a quick task with GSD guarantees
- `/gsd-debug "description"` — Scientific debugging with persistent sessions
- `/gsd-todo-add "description"` — Capture task for later
- `/gsd-todo-check` — List pending todos

### Milestone Completion
- `/gsd-milestone-audit` — Audit milestone completeness and integration
- `/gsd-milestone-complete` — Archive and complete current milestone
- `/gsd-milestone-plan-gaps` — Plan fixes for audit gaps

### Session & Config
- `/gsd-work-pause` — Save context for later
- `/gsd-work-resume` — Resume from previous session
- `/gsd-settings` — Configure GSD workflow
- `/gsd-profile-set [quality|balanced|budget]` — Switch model profile
- `/gsd-health` — Check project health and consistency
- `/gsd-cleanup` — Clean stale planning files
- `/gsd-update` — Check for updates
- `/gsd-help` — Show all commands

## Context Management
If the conversation is getting long, consider using `/gsd-work-pause` to save state and start a fresh session with `/gsd-work-resume`.

<!-- GSD:BEGIN -->
# GSD Project Conventions

## Project Detection
If a `.planning/` directory exists in the workspace root, this is a GSD-managed project. All GSD rules below apply.

## STATE.md First Rule  
**Before performing ANY GSD operation, ALWAYS read `.planning/STATE.md` first.** This file contains:
- Current milestone, phase, and plan position
- Active blockers and decisions
- Session context and progress

## File Conventions
- `.planning/STATE.md` — Current project position and context
- `.planning/PROJECT.md` — Project definition and vision
- `.planning/REQUIREMENTS.md` — Requirement specifications with REQ-IDs
- `.planning/ROADMAP.md` — Phase-based execution roadmap
- `.planning/config.json` — Workflow configuration
- `.planning/phases/{NN}-{name}/` — Phase working directories
  - `{NN}-CONTEXT.md` — User decisions for this phase
  - `{NN}-RESEARCH.md` — Research findings
  - `{NN}-{MM}-PLAN.md` — Execution plans
  - `{NN}-{MM}-SUMMARY.md` — Execution results
  - `{NN}-VALIDATION.md` — Plan verification results
  - `{NN}-VERIFICATION.md` — Post-execution verification
  - `{NN}-UAT.md` — User acceptance testing
- `.planning/quick/` — Quick task directory
- `.planning/codebase/` — Codebase analysis docs
- `.planning/milestones/` — Archived milestones

## Commit Conventions
Use conventional commits: `{type}({scope}): {description}`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`, `ci`
Scope: derived from the component being changed

For planning docs: `docs(planning): {description}`

## Context Fidelity
- **Never invent requirements.** Work only from ROADMAP.md phase goals and PLAN.md tasks.
- **Never assume technology choices.** Check CONTEXT.md and PROJECT.md first.
- **Never skip verification.** Every claim in SUMMARY.md must be verifiable against actual code.

## Planning Doc Format
All `.planning/` markdown files use YAML frontmatter:
```yaml
---
key: value
---
```
Do not modify frontmatter manually — use `gsd_frontmatter_set` MCP tool.

## MCP Tools
GSD provides MCP tools prefixed with `gsd_`. Use these for all state management, config, roadmap, and phase operations instead of manual file editing. Key tools:
- `gsd_state_load` / `gsd_state_update` — State management
- `gsd_config_load` / `gsd_config_set` — Configuration
- `gsd_roadmap_analyze` / `gsd_roadmap_get_phase` — Roadmap queries
- `gsd_commit` — Atomic commits with planning doc tracking
- `gsd_find_phase` — Phase directory discovery

## GSD Commands
Use `/gsd-{command}` to invoke GSD prompts:

### Project Lifecycle
- `/gsd-project-new` — Initialize a new GSD project
- `/gsd-codebase-map` — Analyze existing codebase into structured docs
- `/gsd-progress` — Check project status and route to next action
- `/gsd-milestone-new` — Start a new milestone

### Phase Workflow
- `/gsd-phase-discuss N` — Gather preferences and decisions for phase N
- `/gsd-phase-plan N` — Research and plan phase N
- `/gsd-phase-execute N` — Execute phase N plans with atomic commits
- `/gsd-phase-verify N` — Interactive UAT verification for phase N
- `/gsd-phase-research N` — Deep standalone research for phase N

### Phase Management
- `/gsd-phase-add "description"` — Append phase to roadmap
- `/gsd-phase-remove N` — Remove a future phase
- `/gsd-phase-insert N "description"` — Insert urgent work
- `/gsd-phase-list-assumptions N` — List assumptions for phase N

### Quick Operations
- `/gsd-quick "description"` — Execute a quick task with GSD guarantees
- `/gsd-debug "description"` — Scientific debugging with persistent sessions
- `/gsd-todo-add "description"` — Capture task for later
- `/gsd-todo-check` — List pending todos

### Milestone Completion
- `/gsd-milestone-audit` — Audit milestone completeness and integration
- `/gsd-milestone-complete` — Archive and complete current milestone
- `/gsd-milestone-plan-gaps` — Plan fixes for audit gaps

### Session & Config
- `/gsd-work-pause` — Save context for later
- `/gsd-work-resume` — Resume from previous session
- `/gsd-settings` — Configure GSD workflow
- `/gsd-profile-set [quality|balanced|budget]` — Switch model profile
- `/gsd-health` — Check project health and consistency
- `/gsd-cleanup` — Clean stale planning files
- `/gsd-update` — Check for updates
- `/gsd-help` — Show all commands

## Context Management
If the conversation is getting long, consider using `/gsd-work-pause` to save state and start a fresh session with `/gsd-work-resume`.

<!-- GSD:BEGIN -->
# GSD Project Conventions

## Project Detection
If a `.planning/` directory exists in the workspace root, this is a GSD-managed project. All GSD rules below apply.

## STATE.md First Rule  
**Before performing ANY GSD operation, ALWAYS read `.planning/STATE.md` first.** This file contains:
- Current milestone, phase, and plan position
- Active blockers and decisions
- Session context and progress

## File Conventions
- `.planning/STATE.md` — Current project position and context
- `.planning/PROJECT.md` — Project definition and vision
- `.planning/REQUIREMENTS.md` — Requirement specifications with REQ-IDs
- `.planning/ROADMAP.md` — Phase-based execution roadmap
- `.planning/config.json` — Workflow configuration
- `.planning/phases/{NN}-{name}/` — Phase working directories
  - `{NN}-CONTEXT.md` — User decisions for this phase
  - `{NN}-RESEARCH.md` — Research findings
  - `{NN}-{MM}-PLAN.md` — Execution plans
  - `{NN}-{MM}-SUMMARY.md` — Execution results
  - `{NN}-VALIDATION.md` — Plan verification results
  - `{NN}-VERIFICATION.md` — Post-execution verification
  - `{NN}-UAT.md` — User acceptance testing
- `.planning/quick/` — Quick task directory
- `.planning/codebase/` — Codebase analysis docs
- `.planning/milestones/` — Archived milestones

## Commit Conventions
Use conventional commits: `{type}({scope}): {description}`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`, `ci`
Scope: derived from the component being changed

For planning docs: `docs(planning): {description}`

## Context Fidelity
- **Never invent requirements.** Work only from ROADMAP.md phase goals and PLAN.md tasks.
- **Never assume technology choices.** Check CONTEXT.md and PROJECT.md first.
- **Never skip verification.** Every claim in SUMMARY.md must be verifiable against actual code.

## Planning Doc Format
All `.planning/` markdown files use YAML frontmatter:
```yaml
---
key: value
---
```
Do not modify frontmatter manually — use `gsd_frontmatter_set` MCP tool.

## MCP Tools
GSD provides MCP tools prefixed with `gsd_`. Use these for all state management, config, roadmap, and phase operations instead of manual file editing. Key tools:
- `gsd_state_load` / `gsd_state_update` — State management
- `gsd_config_load` / `gsd_config_set` — Configuration
- `gsd_roadmap_analyze` / `gsd_roadmap_get_phase` — Roadmap queries
- `gsd_commit` — Atomic commits with planning doc tracking
- `gsd_find_phase` — Phase directory discovery

## GSD Commands
Use `/gsd-{command}` to invoke GSD prompts:

### Project Lifecycle
- `/gsd-project-new` — Initialize a new GSD project
- `/gsd-codebase-map` — Analyze existing codebase into structured docs
- `/gsd-progress` — Check project status and route to next action
- `/gsd-milestone-new` — Start a new milestone

### Phase Workflow
- `/gsd-phase-discuss N` — Gather preferences and decisions for phase N
- `/gsd-phase-plan N` — Research and plan phase N
- `/gsd-phase-execute N` — Execute phase N plans with atomic commits
- `/gsd-phase-verify N` — Interactive UAT verification for phase N
- `/gsd-phase-research N` — Deep standalone research for phase N

### Phase Management
- `/gsd-phase-add "description"` — Append phase to roadmap
- `/gsd-phase-remove N` — Remove a future phase
- `/gsd-phase-insert N "description"` — Insert urgent work
- `/gsd-phase-list-assumptions N` — List assumptions for phase N

### Quick Operations
- `/gsd-quick "description"` — Execute a quick task with GSD guarantees
- `/gsd-debug "description"` — Scientific debugging with persistent sessions
- `/gsd-todo-add "description"` — Capture task for later
- `/gsd-todo-check` — List pending todos

### Milestone Completion
- `/gsd-milestone-audit` — Audit milestone completeness and integration
- `/gsd-milestone-complete` — Archive and complete current milestone
- `/gsd-milestone-plan-gaps` — Plan fixes for audit gaps

### Session & Config
- `/gsd-work-pause` — Save context for later
- `/gsd-work-resume` — Resume from previous session
- `/gsd-settings` — Configure GSD workflow
- `/gsd-profile-set [quality|balanced|budget]` — Switch model profile
- `/gsd-health` — Check project health and consistency
- `/gsd-cleanup` — Clean stale planning files
- `/gsd-update` — Check for updates
- `/gsd-help` — Show all commands

## Context Management
If the conversation is getting long, consider using `/gsd-work-pause` to save state and start a fresh session with `/gsd-work-resume`.

<!-- GSD:BEGIN -->
# GSD Project Conventions

## Project Detection
If a `.planning/` directory exists in the workspace root, this is a GSD-managed project. All GSD rules below apply.

## STATE.md First Rule  
**Before performing ANY GSD operation, ALWAYS read `.planning/STATE.md` first.** This file contains:
- Current milestone, phase, and plan position
- Active blockers and decisions
- Session context and progress

## File Conventions
- `.planning/STATE.md` — Current project position and context
- `.planning/PROJECT.md` — Project definition and vision
- `.planning/REQUIREMENTS.md` — Requirement specifications with REQ-IDs
- `.planning/ROADMAP.md` — Phase-based execution roadmap
- `.planning/config.json` — Workflow configuration
- `.planning/phases/{NN}-{name}/` — Phase working directories
  - `{NN}-CONTEXT.md` — User decisions for this phase
  - `{NN}-RESEARCH.md` — Research findings
  - `{NN}-{MM}-PLAN.md` — Execution plans
  - `{NN}-{MM}-SUMMARY.md` — Execution results
  - `{NN}-VALIDATION.md` — Plan verification results
  - `{NN}-VERIFICATION.md` — Post-execution verification
  - `{NN}-UAT.md` — User acceptance testing
- `.planning/quick/` — Quick task directory
- `.planning/codebase/` — Codebase analysis docs
- `.planning/milestones/` — Archived milestones

## Commit Conventions
Use conventional commits: `{type}({scope}): {description}`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`, `ci`
Scope: derived from the component being changed

For planning docs: `docs(planning): {description}`

## Context Fidelity
- **Never invent requirements.** Work only from ROADMAP.md phase goals and PLAN.md tasks.
- **Never assume technology choices.** Check CONTEXT.md and PROJECT.md first.
- **Never skip verification.** Every claim in SUMMARY.md must be verifiable against actual code.

## Planning Doc Format
All `.planning/` markdown files use YAML frontmatter:
```yaml
---
key: value
---
```
Do not modify frontmatter manually — use `gsd_frontmatter_set` MCP tool.

## MCP Tools
GSD provides MCP tools prefixed with `gsd_`. Use these for all state management, config, roadmap, and phase operations instead of manual file editing. Key tools:
- `gsd_state_load` / `gsd_state_update` — State management
- `gsd_config_load` / `gsd_config_set` — Configuration
- `gsd_roadmap_analyze` / `gsd_roadmap_get_phase` — Roadmap queries
- `gsd_commit` — Atomic commits with planning doc tracking
- `gsd_find_phase` — Phase directory discovery

## GSD Commands
Use `/gsd-{command}` to invoke GSD prompts:

### Project Lifecycle
- `/gsd-project-new` — Initialize a new GSD project
- `/gsd-codebase-map` — Analyze existing codebase into structured docs
- `/gsd-progress` — Check project status and route to next action
- `/gsd-milestone-new` — Start a new milestone

### Phase Workflow
- `/gsd-phase-discuss N` — Gather preferences and decisions for phase N
- `/gsd-phase-plan N` — Research and plan phase N
- `/gsd-phase-execute N` — Execute phase N plans with atomic commits
- `/gsd-phase-verify N` — Interactive UAT verification for phase N
- `/gsd-phase-research N` — Deep standalone research for phase N

### Phase Management
- `/gsd-phase-add "description"` — Append phase to roadmap
- `/gsd-phase-remove N` — Remove a future phase
- `/gsd-phase-insert N "description"` — Insert urgent work
- `/gsd-phase-list-assumptions N` — List assumptions for phase N

### Quick Operations
- `/gsd-quick "description"` — Execute a quick task with GSD guarantees
- `/gsd-debug "description"` — Scientific debugging with persistent sessions
- `/gsd-todo-add "description"` — Capture task for later
- `/gsd-todo-check` — List pending todos

### Milestone Completion
- `/gsd-milestone-audit` — Audit milestone completeness and integration
- `/gsd-milestone-complete` — Archive and complete current milestone
- `/gsd-milestone-plan-gaps` — Plan fixes for audit gaps

### Session & Config
- `/gsd-work-pause` — Save context for later
- `/gsd-work-resume` — Resume from previous session
- `/gsd-settings` — Configure GSD workflow
- `/gsd-profile-set [quality|balanced|budget]` — Switch model profile
- `/gsd-health` — Check project health and consistency
- `/gsd-cleanup` — Clean stale planning files
- `/gsd-update` — Check for updates
- `/gsd-help` — Show all commands

## Context Management
If the conversation is getting long, consider using `/gsd-work-pause` to save state and start a fresh session with `/gsd-work-resume`.
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
<!-- GSD:END -->
