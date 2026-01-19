# Design Document: Interface Hierarchy Consolidation

## Overview

This design consolidates the Brinell interface hierarchy based on analysis of:
- Current interfaces in `srcnew/Brinell.Core/Interfaces/` (8 interfaces)
- Legacy interfaces in `src/Brinell.Core/ControlObject6/Interfaces/` (29 interfaces)
- MAUI control implementations in `srcnew/Brinell.Maui/Controls/`
- Framework patterns (TScope fluent chaining, Is/Wait/Assert pattern)

The goal is to establish a comprehensive, consistent interface design that supports both existing MAUI implementations and future Blazor/WPF/WinForms platforms.

## Steering Document Alignment

### Technical Standards (tech.str.spx.md)

| Standard | How Design Follows |
|----------|-------------------|
| TScope generic pattern | All interfaces use `IXxxControlObject<TScope>` |
| Is/Wait/Assert pattern | Every state has Is*/Wait*/Assert* triplet |
| Nullable skip pattern | All Wait/Assert accept nullable expected |
| Fluent chaining | Action/Assert methods return TScope |
| Interface-based Core | All interfaces in `Brinell.Core.Interfaces` namespace |

### Project Structure (structure.str.spx.md)

| Convention | How Design Follows |
|------------|-------------------|
| Interface file naming | `I{ControlType}ControlObject.cs` |
| Namespace | `Brinell.Core.Interfaces` |
| Interface segregation | One capability per interface |
| Single responsibility | Each interface has focused purpose |

## Code Reuse Analysis

### Existing Components to Leverage

- **IControlObject<TScope>**: Current base interface, minimal changes needed
- **IClickableControlObject<TScope>**: Add Hover and LongPress methods
- **ITextControlObject<TScope>**: Add missing Assert methods
- **IEditableTextControlObject<TScope>**: Add Append method
- **IToggleControlObject<TScope>**: Complete, no changes needed
- **IRangeControlObject<TScope>**: Complete, no changes needed
- **ISelectorControlObject<TScope>**: Complete, no changes needed
- **IScrollableControlObject<TScope>**: Complete, no changes needed

### Integration Points

- **MauiControlBase<TScope>**: Will need to implement new interface methods
- **MauiButtonControl<TScope>**: Will implement Hover and LongPress
- **MauiEntryControl<TScope>**: Will implement new text assertion methods

## Architecture

### Interface Hierarchy Diagram

```mermaid
classDiagram
    direction TB
    
    class IControlObject~TScope~ {
        <<interface>>
        +Locator Locator
        +TScope ContainingScope
        +IPageObject Page
        +IsExists() bool?
        +WaitExists(bool?, int?) bool
        +AssertExists(bool?, string?, int?) TScope
        +IsVisible() bool?
        +WaitVisible(bool?, int?) bool
        +AssertVisible(bool?, string?, int?) TScope
        +IsEnabled() bool?
        +WaitEnabled(bool?, int?) bool
        +AssertEnabled(bool?, string?, int?) TScope
        +GetText() string?
        +WaitText(string?, int?) bool
        +AssertText(string?, string?, int?) TScope
        +GetAttribute(string) string?
    }
    
    class IClickableControlObject~TScope~ {
        <<interface>>
        +IsClickable() bool?
        +WaitClickable(bool?, int?) bool
        +AssertClickable(bool?, string?, int?) TScope
        +Click(int?) TScope
        +DoubleClick(int?) TScope
        +RightClick(int?) TScope
        +Hover(int?) TScope
        +LongPress(int?, int?) TScope
    }
    IClickableControlObject --|> IControlObject
    
    class ITextControlObject~TScope~ {
        <<interface>>
        +WaitTextEquals(string?, int?) bool
        +WaitTextContains(string?, int?) bool
        +AssertTextMatches(string?, string?, int?) TScope
        +AssertTextContains(string?, string?, int?) TScope
        +AssertTextStartsWith(string?, string?, int?) TScope
        +AssertTextEndsWith(string?, string?, int?) TScope
        +AssertTextEmpty(bool?, string?, int?) TScope
    }
    ITextControlObject --|> IControlObject
    
    class IEditableTextControlObject~TScope~ {
        <<interface>>
        +Enter(string?, int?) TScope
        +Clear(int?) TScope
        +SetText(string?, int?) TScope
        +Append(string?, int?) TScope
        +GetPlaceholder() string?
        +AssertPlaceholder(string?, string?, int?) TScope
        +IsReadOnly() bool?
        +WaitReadOnly(bool?, int?) bool
        +AssertReadOnly(bool?, string?, int?) TScope
    }
    IEditableTextControlObject --|> ITextControlObject
    
    class IToggleControlObject~TScope~ {
        <<interface>>
        +IsChecked() bool?
        +WaitChecked(bool?, int?) bool
        +AssertChecked(bool?, string?, int?) TScope
        +Toggle(int?) TScope
        +SetChecked(bool?, int?) TScope
        +Check(int?) TScope
        +Uncheck(int?) TScope
    }
    IToggleControlObject --|> IControlObject
    
    class IRangeControlObject~TScope~ {
        <<interface>>
        +GetValue() double?
        +GetMinimum() double?
        +GetMaximum() double?
        +WaitValue(double?, int?) bool
        +AssertValue(double?, string?, int?) TScope
        +AssertValueInRange(double?, double?, string?, int?) TScope
        +SetValue(double?, int?) TScope
        +Increment(int?) TScope
        +Decrement(int?) TScope
    }
    IRangeControlObject --|> IControlObject
    
    class ISelectorControlObject~TScope~ {
        <<interface>>
        +SelectByText(string?, int?) TScope
        +SelectByIndex(int?, int?) TScope
        +SelectByValue(string?, int?) TScope
        +GetSelectedText() string?
        +GetSelectedIndex() int?
        +WaitSelectedText(string?, int?) bool
        +WaitSelectedIndex(int?, int?) bool
        +AssertSelectedText(string?, string?, int?) TScope
        +AssertSelectedIndex(int?, string?, int?) TScope
        +GetItemTexts() List
        +GetItemCount() int
    }
    ISelectorControlObject --|> IControlObject
    
    class IScrollableControlObject~TScope~ {
        <<interface>>
        +ScrollToTop(int?) TScope
        +ScrollToEnd(int?) TScope
        +ScrollBy(int, int, int?) TScope
        +ScrollTo(double, int?) TScope
        +GetScrollPosition() double?
        +WaitScrollPosition(double?, int?) bool
        +AssertScrollPosition(double?, string?, int?) TScope
    }
    IScrollableControlObject --|> IControlObject
```

### New Specialized Interfaces

```mermaid
classDiagram
    direction TB
    
    class IControlObject~TScope~ {
        <<interface>>
    }
    
    class IClickableControlObject~TScope~ {
        <<interface>>
    }
    IClickableControlObject --|> IControlObject
    
    class ITabControlObject~TScope~ {
        <<interface>>
        +Title string
        +IsSelected() bool?
        +WaitSelected(bool?, int?) bool
        +AssertSelected(bool?, string?, int?) TScope
    }
    ITabControlObject --|> IClickableControlObject
    
    class IExpandableControlObject~TScope~ {
        <<interface>>
        +IsExpanded() bool?
        +WaitExpanded(bool?, int?) bool
        +AssertExpanded(bool?, string?, int?) TScope
        +Expand(int?) TScope
        +Collapse(int?) TScope
        +Toggle(int?) TScope
    }
    IExpandableControlObject --|> IClickableControlObject
    
    class IFocusableControlObject~TScope~ {
        <<interface>>
        +IsFocused() bool?
        +WaitFocused(bool?, int?) bool
        +AssertFocused(bool?, string?, int?) TScope
        +Focus(int?) TScope
        +Blur(int?) TScope
    }
    IFocusableControlObject --|> IControlObject
    
    class IProgressControlObject~TScope~ {
        <<interface>>
        +IsIndeterminate() bool?
        +GetProgress() double?
        +WaitProgress(double?, int?) bool
        +AssertProgress(double?, string?, int?) TScope
        +WaitComplete(int?) bool
        +AssertComplete(string?, int?) TScope
    }
    IProgressControlObject --|> IControlObject
    
    class IDateControlObject~TScope~ {
        <<interface>>
        +GetDate() DateTime?
        +SetDate(DateTime?, int?) TScope
        +WaitDate(DateTime?, int?) bool
        +AssertDate(DateTime?, string?, int?) TScope
    }
    IDateControlObject --|> IControlObject
    
    class ITimeControlObject~TScope~ {
        <<interface>>
        +GetTime() TimeSpan?
        +SetTime(TimeSpan?, int?) TScope
        +WaitTime(TimeSpan?, int?) bool
        +AssertTime(TimeSpan?, string?, int?) TScope
    }
    ITimeControlObject --|> IControlObject
```

### Mobile-Specific Interfaces

```mermaid
classDiagram
    direction TB
    
    class IControlObject~TScope~ {
        <<interface>>
    }
    
    class ISwipeableControlObject~TScope~ {
        <<interface>>
        +SwipeLeft(int?) TScope
        +SwipeRight(int?) TScope
        +SwipeUp(int?) TScope
        +SwipeDown(int?) TScope
        +Swipe(int, int, int, int, int?) TScope
    }
    ISwipeableControlObject --|> IControlObject
    
    class IRefreshableControlObject~TScope~ {
        <<interface>>
        +IsRefreshing() bool?
        +WaitRefreshing(bool?, int?) bool
        +AssertRefreshing(bool?, string?, int?) TScope
        +PullToRefresh(int?) TScope
    }
    IRefreshableControlObject --|> IControlObject
```

## Components and Interfaces

### Component 1: Enhanced Core Interfaces

**Purpose:** Extend existing interfaces with missing methods

**Files to Modify:**
- `srcnew/Brinell.Core/Interfaces/IClickableControlObject.cs`
- `srcnew/Brinell.Core/Interfaces/ITextControlObject.cs`
- `srcnew/Brinell.Core/Interfaces/IEditableTextControlObject.cs`

**Changes:**

```csharp
// IClickableControlObject.cs - Add methods
TScope Hover(int? timeoutMs = null);
TScope LongPress(int? durationMs = null, int? timeoutMs = null);

// ITextControlObject.cs - Add methods
TScope AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
TScope AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null);
TScope AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null);
TScope AssertTextEmpty(bool? expected, string? message = null, int? timeoutMs = null);

// IEditableTextControlObject.cs - Add method
TScope Append(string? text, int? timeoutMs = null);
```

### Component 2: New Specialized Interfaces

**Purpose:** Add missing specialized control interfaces

**New Files:**
| File | Interface | Priority |
|------|-----------|----------|
| `IExpandableControlObject.cs` | Expanders, accordions | High |
| `IFocusableControlObject.cs` | Focus management | Medium |
| `IProgressControlObject.cs` | Progress indicators | Medium |
| `IDateControlObject.cs` | Date pickers | Medium |
| `ITimeControlObject.cs` | Time pickers | Medium |
| `ISwipeableControlObject.cs` | Mobile gestures | Medium |
| `IRefreshableControlObject.cs` | Pull-to-refresh | Medium |

### Component 3: Interface Relocation

**Purpose:** Move ITabControlObject to standard location

**Current Location:** `srcnew/Brinell.Core/Abstractions/Controls/ITabControlObject.cs`
**Target Location:** `srcnew/Brinell.Core/Interfaces/ITabControlObject.cs`

## Interface Specifications

### IExpandableControlObject<TScope>

```csharp
namespace Brinell.Core.Interfaces;

/// <summary>
/// Interface for controls that can be expanded and collapsed.
/// Used for expanders, accordions, tree nodes, and collapsible sections.
/// </summary>
public interface IExpandableControlObject<TScope> : IClickableControlObject<TScope>
{
    /// <summary>
    /// Checks if the control is currently expanded.
    /// </summary>
    bool? IsExpanded();
    
    /// <summary>
    /// Waits for the control to be expanded or collapsed.
    /// </summary>
    bool WaitExpanded(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Asserts the control's expanded state.
    /// </summary>
    TScope AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Expands the control. No-op if already expanded.
    /// </summary>
    TScope Expand(int? timeoutMs = null);
    
    /// <summary>
    /// Collapses the control. No-op if already collapsed.
    /// </summary>
    TScope Collapse(int? timeoutMs = null);
    
    /// <summary>
    /// Toggles the expanded state.
    /// </summary>
    TScope ToggleExpanded(int? timeoutMs = null);
}
```

### IFocusableControlObject<TScope>

```csharp
namespace Brinell.Core.Interfaces;

/// <summary>
/// Interface for controls that can receive keyboard focus.
/// </summary>
public interface IFocusableControlObject<TScope> : IControlObject<TScope>
{
    /// <summary>
    /// Checks if the control currently has focus.
    /// </summary>
    bool? IsFocused();
    
    /// <summary>
    /// Waits for the control to have or lose focus.
    /// </summary>
    bool WaitFocused(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Asserts the control's focus state.
    /// </summary>
    TScope AssertFocused(bool? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Sets focus to the control.
    /// </summary>
    TScope Focus(int? timeoutMs = null);
    
    /// <summary>
    /// Removes focus from the control.
    /// </summary>
    TScope Blur(int? timeoutMs = null);
}
```

### IProgressControlObject<TScope>

```csharp
namespace Brinell.Core.Interfaces;

/// <summary>
/// Interface for progress indicator controls.
/// </summary>
public interface IProgressControlObject<TScope> : IControlObject<TScope>
{
    /// <summary>
    /// Checks if the progress is indeterminate (unknown duration).
    /// </summary>
    bool? IsIndeterminate();
    
    /// <summary>
    /// Gets the current progress value (0.0 to 1.0).
    /// </summary>
    double? GetProgress();
    
    /// <summary>
    /// Waits for progress to reach a specific value.
    /// </summary>
    bool WaitProgress(double? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Asserts the current progress value.
    /// </summary>
    TScope AssertProgress(double? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Waits for progress to complete (reach 1.0 or disappear).
    /// </summary>
    bool WaitComplete(int? timeoutMs = null);
    
    /// <summary>
    /// Asserts progress is complete.
    /// </summary>
    TScope AssertComplete(string? message = null, int? timeoutMs = null);
}
```

### IDateControlObject<TScope>

```csharp
namespace Brinell.Core.Interfaces;

/// <summary>
/// Interface for date picker controls.
/// </summary>
public interface IDateControlObject<TScope> : IControlObject<TScope>
{
    /// <summary>
    /// Gets the currently selected date.
    /// </summary>
    DateTime? GetDate();
    
    /// <summary>
    /// Sets the date value. Skip if null.
    /// </summary>
    TScope SetDate(DateTime? date, int? timeoutMs = null);
    
    /// <summary>
    /// Waits for the date to match expected value.
    /// </summary>
    bool WaitDate(DateTime? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Asserts the current date value.
    /// </summary>
    TScope AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null);
}
```

### ITimeControlObject<TScope>

```csharp
namespace Brinell.Core.Interfaces;

/// <summary>
/// Interface for time picker controls.
/// </summary>
public interface ITimeControlObject<TScope> : IControlObject<TScope>
{
    /// <summary>
    /// Gets the currently selected time.
    /// </summary>
    TimeSpan? GetTime();
    
    /// <summary>
    /// Sets the time value. Skip if null.
    /// </summary>
    TScope SetTime(TimeSpan? time, int? timeoutMs = null);
    
    /// <summary>
    /// Waits for the time to match expected value.
    /// </summary>
    bool WaitTime(TimeSpan? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Asserts the current time value.
    /// </summary>
    TScope AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null);
}
```

### ISwipeableControlObject<TScope>

```csharp
namespace Brinell.Core.Interfaces;

/// <summary>
/// Interface for controls that support swipe gestures.
/// Primarily for mobile platforms.
/// </summary>
public interface ISwipeableControlObject<TScope> : IControlObject<TScope>
{
    /// <summary>
    /// Performs a swipe left gesture.
    /// </summary>
    TScope SwipeLeft(int? timeoutMs = null);
    
    /// <summary>
    /// Performs a swipe right gesture.
    /// </summary>
    TScope SwipeRight(int? timeoutMs = null);
    
    /// <summary>
    /// Performs a swipe up gesture.
    /// </summary>
    TScope SwipeUp(int? timeoutMs = null);
    
    /// <summary>
    /// Performs a swipe down gesture.
    /// </summary>
    TScope SwipeDown(int? timeoutMs = null);
    
    /// <summary>
    /// Performs a swipe from one point to another.
    /// </summary>
    TScope Swipe(int startX, int startY, int endX, int endY, int? timeoutMs = null);
}
```

### IRefreshableControlObject<TScope>

```csharp
namespace Brinell.Core.Interfaces;

/// <summary>
/// Interface for controls that support pull-to-refresh.
/// Primarily for mobile platforms.
/// </summary>
public interface IRefreshableControlObject<TScope> : IControlObject<TScope>
{
    /// <summary>
    /// Checks if the control is currently refreshing.
    /// </summary>
    bool? IsRefreshing();
    
    /// <summary>
    /// Waits for refresh state to match expected.
    /// </summary>
    bool WaitRefreshing(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Asserts the refresh state.
    /// </summary>
    TScope AssertRefreshing(bool? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Performs a pull-to-refresh gesture.
    /// </summary>
    TScope PullToRefresh(int? timeoutMs = null);
}
```

## Error Handling

### Error Scenarios

1. **Element Not Found**
   - **Handling:** Is* methods return null, Wait* methods return false after timeout, Assert* methods throw ElementNotFoundException
   - **User Impact:** Clear error message with Locator information

2. **Timeout Exceeded**
   - **Handling:** Wait* returns false, Assert* throws TimeoutException with expected vs actual state
   - **User Impact:** Message includes timeout duration, expected state, and current state

3. **Invalid Operation**
   - **Handling:** Throw InvalidOperationException (e.g., SetDate on read-only control)
   - **User Impact:** Clear message explaining why operation failed

## Testing Strategy

### Unit Testing

- Each new interface method needs unit tests
- Test null skip behavior for all Wait/Assert methods
- Test return type (TScope) for fluent chaining
- Mock element states to verify Is*/Wait*/Assert* logic

### Integration Testing

- Test interface implementations in MAUI controls
- Verify fluent chaining works end-to-end
- Test timeout behavior with real delays

### Platform Compatibility Matrix

| Interface | MAUI | Blazor | WPF | HTML |
|-----------|------|--------|-----|------|
| IControlObject | ✓ | ✓ | ✓ | ✓ |
| IClickableControlObject | ✓ | ✓ | ✓ | ✓ |
| ITextControlObject | ✓ | ✓ | ✓ | ✓ |
| IEditableTextControlObject | ✓ | ✓ | ✓ | ✓ |
| IToggleControlObject | ✓ | ✓ | ✓ | ✓ |
| IRangeControlObject | ✓ | ✓ | ✓ | ✓ |
| ISelectorControlObject | ✓ | ✓ | ✓ | ✓ |
| IScrollableControlObject | ✓ | ✓ | ✓ | ✓ |
| ITabControlObject | ✓ | ✓ | ✓ | ✓ |
| IExpandableControlObject | ✓ | ✓ | ✓ | ✓ |
| IFocusableControlObject | ✓ | ✓ | ✓ | ✓ |
| IProgressControlObject | ✓ | ✓ | ✓ | ✓ |
| IDateControlObject | ✓ | ✓ | ✓ | ✓ |
| ITimeControlObject | ✓ | ✓ | ✓ | ✓ |
| ISwipeableControlObject | ✓ | - | - | - |
| IRefreshableControlObject | ✓ | - | - | - |

## Implementation Order

### Phase 1: Core Interface Enhancements (Priority: High)

1. Add Hover/LongPress to IClickableControlObject
2. Add text assertion methods to ITextControlObject  
3. Add Append to IEditableTextControlObject
4. Move ITabControlObject to standard location

### Phase 2: Common Specialized Interfaces (Priority: Medium)

5. Create IExpandableControlObject
6. Create IFocusableControlObject
7. Create IProgressControlObject

### Phase 3: Date/Time Interfaces (Priority: Medium)

8. Create IDateControlObject
9. Create ITimeControlObject

### Phase 4: Mobile-Specific Interfaces (Priority: Low)

10. Create ISwipeableControlObject
11. Create IRefreshableControlObject

---

**Document Version:** 1.0  
**Created:** January 19, 2026  
**Spec ID:** 003  
**Status:** Draft  
**Workflow:** spec_workflow/design
