# Control Object Framework — Index

**Status:** Active | Based on implementation in `srcnew/Brinell.Core/Interfaces/`

## Documents

| Document | Content |
|----------|---------|
| [001-INTERFACES.md](001-INTERFACES.md) | All 25 interfaces with method signatures |
| [classes/](classes/) | Class specifications by category |
| [hierarchy/](hierarchy/) | Platform hierarchy and base class specs |
| [TESTING-GUIDE.md](TESTING-GUIDE.md) | Mockability and test patterns |

## Design Rules

1. **Nullable expected:** `null` value in Wait/Assert = skip (no-op)
2. **Locator system:** `Locator` value object with 14 strategies via factory methods
3. **Parameter order:** `required` → `nullable expected` → `message?` → `timeoutMs?`
4. **Fluent chaining:** action/assertion methods return `TScope`
5. **Tri-state queries:** `Is*()` returns `bool?` — null means element not found

## Interface Categories

| Category | Interfaces | Count |
|----------|-----------|-------|
| Foundation | `IControlObject`, `IClickableControlObject`, `IFocusableControlObject` | 3 |
| Text | `ITextControlObject`, `IEditableTextControlObject` | 2 |
| Selection | `ISelectorControlObject`, `IToggleControlObject`, `ITabControlObject`, `IExpandableControlObject` | 4 |
| Range | `IRangeControlObject`, `IProgressControlObject` | 2 |
| DateTime | `IDateControlObject`, `ITimeControlObject` | 2 |
| Scrolling | `IScrollableControlObject`, `ISwipeableControlObject`, `IRefreshableControlObject` | 3 |
| Infrastructure | `IElement`, `IElementScope`, `IPagedScope`, `IPageObject`, `IDriver`, `IDiagnosticDriver`, `ITestContext`, `IContainerControl`, `IScreenshotService` | 9 |
| **Total** | | **25** |
