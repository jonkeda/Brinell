# Foundation Classes

**Source of truth:** `srcnew/Brinell.Maui/Controls/`

## MAUI Base Class Hierarchy

```
MauiObjectBase (shared Run/RunWithElement/Poll engine)
├── MauiControlBase<TScope> : IControlObject<TScope>
│   ├── MauiClickableControlBase<TScope> : IClickableControlObject<TScope>
│   ├── MauiTextControlBase<TScope> : ITextControlObject<TScope>
│   ├── MauiToggleControlBase<TScope> : IToggleControlObject<TScope>
│   └── ...capability bases
└── MauiPageBase<TElement> : IPageObject<TElement>
```

## Key Design Patterns

### Run/RunWithElement/Poll Pattern

All control methods use three internal patterns:

- **`Run(action)`** — Execute an action with logging, error handling, and timeout
- **`RunWithElement(action)`** — Find element first, then execute action on it (avoids redundant FindElement calls)
- **`Poll(condition, timeout)`** — Poll a condition until true or timeout, with configurable interval

### Element Finding

Controls find elements via their `Locator` through the scope's `IElementScope<TElement>`:
- First attempt: direct find
- On failure: `ScrollIntoView` then retry (see SPEC-015)
- All find operations go through the scope's element search, respecting container boundaries

### Logging

Every action logs: timestamp, control type, locator, action name, result.
Logging is built into `Run`/`RunWithElement` — implementations don't need to log explicitly.

## Blazor Base Classes

Scaffolded in `srcnew/Brinell.Blazor/Controls/`. Will use async variants for Playwright operations.
Current status: `Placeholder.cs` only.
