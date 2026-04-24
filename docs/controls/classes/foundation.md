# Foundation Classes

**Source of truth:** `srcnew/Brinell.Maui/Controls/`

## MAUI Base Class Hierarchy (SPEC-004)

```
MauiObjectBase (shared Run/RunWithElement/Poll engine)
├── MauiControlBase<TScope> : IControlObject<TScope>
│   ├── MauiClickableControlBase<TScope> : IClickableControlObject<TScope>
│   │   └── provides Click, DoubleClick, RightClick, Hover, LongPress
│   ├── MauiTextControlBase<TScope> : ITextControlObject<TScope>
│   │   └── provides GetText, AssertText*, WaitText*
│   ├── MauiToggleControlBase<TScope> : IToggleControlObject<TScope>
│   │   └── provides IsChecked, Toggle, SetChecked, Check, Uncheck
│   ├── MauiRangeControlBase<TScope> : IRangeControlObject<TScope>
│   │   └── provides GetValue, SetValue, Increment, Decrement, GetMin/Max/Step
│   ├── MauiSelectorControlBase<TScope> : ISelectorControlObject<TScope>
│   │   └── provides SelectByText, SelectByIndex, GetSelectedText, GetItemTexts
│   ├── MauiScrollableControlBase<TScope> : IScrollableControlObject<TScope>
│   │   └── provides ScrollToTop, ScrollToEnd, ScrollBy, ScrollTo
│   ├── MauiExpandableControlBase<TScope> : IExpandableControlObject<TScope>
│   │   └── provides Expand, Collapse, ToggleExpanded
│   ├── MauiFocusableControlBase<TScope> : IFocusableControlObject<TScope>
│   │   └── provides Focus, Blur, IsFocused
│   ├── MauiSwipeableControlBase<TScope> : ISwipeableControlObject<TScope>
│   │   └── provides SwipeLeft, SwipeRight, SwipeUp, SwipeDown
│   └── MauiRefreshableControlBase<TScope> : IRefreshableControlObject<TScope>
│       └── provides PullToRefresh, IsRefreshing
└── MauiPageBase<TElement> : IPageObject<TElement>
```

**Implementation status:** 13/15 tasks complete (Phases 1-3 creation + refactoring done, Phase 3 testing 1/3 remaining).

## Key Design Patterns

### Run/RunWithElement/Poll Pattern

All control methods use three internal patterns:

- **`Run(action)`** — Execute an action with logging, error handling, and timeout
- **`RunWithElement(action)`** — Find element first, then execute action on it (avoids redundant FindElement calls)
- **`Poll(condition, timeout)`** — Poll a condition until true or timeout, with configurable interval

These are consolidated in `MauiObjectBase`. All intermediate base classes (MauiClickableControlBase, MauiToggleControlBase, etc.) use these engine methods, eliminating code duplication across control implementations.

### Element Finding

Controls find elements via their `Locator` through the scope's `IElementScope<TElement>`:
- First attempt: direct find
- On failure: `ScrollIntoView` then retry (see SPEC-015)
- All find operations go through the scope's element search, respecting container boundaries

### Intermediate Base Class Pattern

Each capability interface has a corresponding intermediate base class that provides the default implementation. Concrete controls extend from the appropriate base:

```csharp
// Concrete control = intermediate base + interface
public class MauiCheckBoxControl<TScope> : MauiToggleControlBase<TScope>
public class MauiSliderControl<TScope> : MauiRangeControlBase<TScope>
public class MauiPickerControl<TScope> : MauiSelectorControlBase<TScope>
```

Controls needing multiple capabilities compose via interface implementation on top of a primary base class.

### Logging

Every action logs: timestamp, control type, locator, action name, result.
Logging is built into `Run`/`RunWithElement` — implementations don't need to log explicitly.

## Blazor Base Classes

Scaffolded in `srcnew/Brinell.Blazor/Controls/`. Will use async variants for Playwright operations.
Current status: `Placeholder.cs` only.
