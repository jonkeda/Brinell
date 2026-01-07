# ADR-004: Control Interface Hierarchy

**Block:** 202 decision  
**Edition:** 🟡Ⅱ Core  
**Version:** 1.0  
**Created:** January 7, 2026

---

## decision ADR-004

- **title**: Capability-Based Control Interface Hierarchy
- **status**: accepted
- **date**: 2026-01-07
- **context**: Controls have varying capabilities (click, text, toggle, select); need a type-safe way to express what each control can do.
- **decision**: Use single base interface (IControlObject) with capability interfaces; controls implement multiple interfaces to express capabilities.
- **consequences**: Compile-time capability checking, flexible composition, clear contracts, requires understanding of interface composition.

---

## 1. Context

UI controls have varying capabilities:

| Control | Click | Text | Edit | Toggle | Select | Range |
|---------|-------|------|------|--------|--------|-------|
| Button | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Label | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Entry | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| CheckBox | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ |
| Picker | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ |
| Slider | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |

### Requirements

1. **Type safety** — Compiler catches capability misuse
2. **Discoverability** — IntelliSense shows available operations
3. **Composition** — Controls can have multiple capabilities
4. **Extension** — New capabilities can be added
5. **Testability** — Each capability can be mocked independently

---

## 2. Decision

**Capability-based interface hierarchy** with:

1. Single base interface: `IControlObject`
2. Capability interfaces extending `IControlObject`
3. Controls implement relevant capability interfaces
4. Base classes provide default implementations

### Interface Hierarchy

```
IControlObject                     # Base for ALL controls
│
├── IClickableControl              # Click/tap actions
│
├── ITextControl                   # Read text content
│   └── IEditableTextControl       # Modify text content
│
├── IToggleControl                 # On/off state
│
├── ISelectorControl               # Single selection from options
│   └── IMultiSelectorControl      # Multiple selection
│
├── IRangeControl                  # Numeric value in range
│
├── IContainerControl              # Scope child element searches
│
├── ICollectionControl             # Enumerate child items
│
└── IScrollableControl             # Scroll operations
```

### Control Composition Example

```csharp
// Button: clickable + text
public class ButtonControl : ClickableControlBase, ITextControl

// Entry: editable text + clickable (for focus)
public class EntryControl : EditableTextControlBase, IClickableControl

// CheckBox: toggle + clickable + text (label)
public class CheckBoxControl : ToggleControlBase, IClickableControl, ITextControl

// Slider: range + clickable (for drag)
public class SliderControl : RangeControlBase, IClickableControl

// Picker: selector + clickable + text
public class PickerControl : SelectorControlBase, IClickableControl, ITextControl
```

---

## 3. Interface Definitions

### IControlObject (Base)

```csharp
public interface IControlObject
{
    // Identity
    string AutomationId { get; }
    IPageObject? Page { get; }
    
    // State
    bool IsExists();
    bool IsVisible();
    bool IsEnabled();
    
    // Waiting
    bool WaitExists(bool exists = true, int? timeout = null);
    bool WaitVisible(bool visible = true, int? timeout = null);
    bool WaitEnabled(bool enabled = true, int? timeout = null);
    
    // Assertions
    void AssertExists(string? message = null);
    void AssertNotExists(string? message = null);
    void AssertVisible(string? message = null);
    void AssertNotVisible(string? message = null);
    void AssertEnabled(string? message = null);
    void AssertDisabled(string? message = null);
}
```

### IClickableControl

```csharp
public interface IClickableControl : IControlObject
{
    void Click();
    void Tap();
    void DoubleClick();
    void LongPress(int durationMs = 1000);
}
```

### ITextControl

```csharp
public interface ITextControl : IControlObject
{
    string GetText();
    
    void AssertTextEquals(string expected, string? message = null);
    void AssertTextContains(string substring, string? message = null);
    void AssertTextMatches(string pattern, string? message = null);
    void AssertTextEmpty(string? message = null);
    void AssertTextNotEmpty(string? message = null);
}
```

### IEditableTextControl

```csharp
public interface IEditableTextControl : ITextControl
{
    void Enter(string text);
    void Clear();
    void SetText(string text); // Clear + Enter
    
    string? GetPlaceholder();
}
```

### IToggleControl

```csharp
public interface IToggleControl : IControlObject
{
    bool IsOn { get; }
    
    void Toggle();
    void SetState(bool on);
    
    void AssertOn(string? message = null);
    void AssertOff(string? message = null);
}
```

### ISelectorControl

```csharp
public interface ISelectorControl : IControlObject
{
    IReadOnlyList<string> GetOptions();
    string? GetSelectedOption();
    int GetSelectedIndex();
    
    void Select(string option);
    void SelectByIndex(int index);
    
    void AssertSelected(string option, string? message = null);
    void AssertSelectedIndex(int index, string? message = null);
}
```

### IRangeControl

```csharp
public interface IRangeControl : IControlObject
{
    double GetValue();
    double GetMinimum();
    double GetMaximum();
    
    void SetValue(double value);
    void Increment(double step = 1);
    void Decrement(double step = 1);
    
    void AssertValue(double expected, double tolerance = 0.001, string? message = null);
    void AssertInRange(double min, double max, string? message = null);
}
```

### IContainerControl

```csharp
public interface IContainerControl : IControlObject
{
    T Find<T>(string automationId) where T : IControlObject;
    T? TryFind<T>(string automationId) where T : IControlObject;
    IReadOnlyList<T> FindAll<T>() where T : IControlObject;
}
```

---

## 4. Consequences

### Positive

| Benefit | Description |
|---------|-------------|
| **Type safety** | `button.Enter()` won't compile if Button doesn't implement IEditableTextControl |
| **IntelliSense** | Shows only methods available for that control type |
| **Composition** | Controls naturally express their capabilities |
| **Mockability** | Mock specific capability interfaces in tests |
| **Extension** | Add new capabilities without changing existing interfaces |
| **Clarity** | Interface name describes what it does |

### Negative

| Trade-off | Mitigation |
|-----------|------------|
| **Multiple interfaces** | Keep capabilities granular but not excessive |
| **Casting sometimes needed** | Factory methods return appropriate interface |
| **Learning curve** | Clear documentation, consistent patterns |
| **Method duplication** | Base classes provide common implementations |

---

## 5. Alternatives Considered

### Alternative 1: Single Interface with Optional Methods

```csharp
// NOT CHOSEN
public interface IControlObject
{
    void Click(); // Throws if not clickable
    string GetText(); // Throws if no text
    void Enter(string text); // Throws if not editable
}
```

**Rejected because:**
- Runtime exceptions instead of compile errors
- No way to know capabilities at compile time
- Poor discoverability

### Alternative 2: Capability Flags

```csharp
// NOT CHOSEN
public interface IControlObject
{
    ControlCapabilities Capabilities { get; }
    // Check Capabilities.HasFlag(ControlCapabilities.Clickable)
}
```

**Rejected because:**
- Runtime checking
- No IntelliSense for available methods
- Flags don't define method contracts

### Alternative 3: Deep Inheritance

```csharp
// NOT CHOSEN
IControlObject
  └── IClickableControl
        └── IClickableTextControl
              └── IClickableEditableTextControl
                    └── IClickableEditableToggleControl
                          └── ...
```

**Rejected because:**
- Combinatorial explosion
- Rigid hierarchy
- New capability = new deep branches

---

## 6. Base Class Mapping

Each platform implements base classes:

| Interface | Base Class | Provides |
|-----------|------------|----------|
| IControlObject | ControlBase | State, waiting, assertions |
| IClickableControl | ClickableControlBase | Click, tap, long-press |
| ITextControl | TextControlBase | GetText, text assertions |
| IEditableTextControl | EditableTextControlBase | Enter, clear, set |
| IToggleControl | ToggleControlBase | Toggle, state management |
| ISelectorControl | SelectorControlBase | Selection operations |
| IRangeControl | RangeControlBase | Value operations |
| IContainerControl | ContainerControlBase | Scoped find methods |
| ICollectionControl | CollectionControlBase | Item enumeration |
| IScrollableControl | ScrollableControlBase | Scroll operations |

---

## 7. Control Capability Matrix

Complete mapping of controls to capabilities:

| Control | Click | Text | Edit | Toggle | Select | Range | Container | Collection | Scroll |
|---------|-------|------|------|--------|--------|-------|-----------|------------|--------|
| Button | ✅ | ✅ | | | | | | | |
| ImageButton | ✅ | | | | | | | | |
| Label | | ✅ | | | | | | | |
| Entry | ✅ | ✅ | ✅ | | | | | | |
| Editor | ✅ | ✅ | ✅ | | | | | | |
| SearchBar | ✅ | ✅ | ✅ | | | | | | |
| CheckBox | ✅ | ✅ | | ✅ | | | | | |
| Switch | ✅ | | | ✅ | | | | | |
| RadioButton | ✅ | ✅ | | ✅ | | | | | |
| Picker | ✅ | ✅ | | | ✅ | | | | |
| DatePicker | ✅ | ✅ | | | ✅ | | | | |
| TimePicker | ✅ | ✅ | | | ✅ | | | | |
| Slider | ✅ | | | | | ✅ | | | |
| Stepper | ✅ | | | | | ✅ | | | |
| ProgressBar | | | | | | ✅ | | | |
| Frame | | | | | | | ✅ | | |
| ContentView | | | | | | | ✅ | | |
| ScrollView | | | | | | | ✅ | | ✅ |
| ListView | ✅ | | | | | | | ✅ | ✅ |
| CollectionView | ✅ | | | | | | | ✅ | ✅ |
| CarouselView | ✅ | | | | | | | ✅ | ✅ |

---

## 8. Validation

This decision is validated when:

- [ ] All controls implement appropriate capability interfaces
- [ ] Compiler catches capability misuse
- [ ] IntelliSense shows correct methods per control
- [ ] Unit tests can mock any capability interface
- [ ] New capabilities can be added without breaking existing interfaces

---

## Related Decisions

- [ADR-001: Clean Architecture](202_001_CleanArchitecture.spx.md)
- [ADR-002: Interface-First Design](202_002_InterfaceFirst.spx.md)
- [ADR-003: Platform Separation](202_003_PlatformSeparation.spx.md)

---

## Related Documents

- [211_001_Interfaces.spx.md](../211_Modules/211_001_Interfaces.spx.md) — Interface module details
- [211_002_BaseClasses.spx.md](../211_Modules/211_002_BaseClasses.spx.md) — Base class module details
