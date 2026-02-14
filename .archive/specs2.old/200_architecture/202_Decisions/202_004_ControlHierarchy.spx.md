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

> **📋 Complete interface definitions:** See [250_005_InterfaceHierarchy.spx.md](../../250_specifications/250_000_Foundation/250_005_InterfaceHierarchy.spx.md) for the authoritative interface catalog.

This ADR defines the **design decision** for capability-based interfaces. For complete method signatures and behavior specifications, refer to the specification documents.

### Interface Summary

| Interface | Purpose | Key Methods |
|-----------|---------|-------------|
| IControlObject | Base for all controls | State, waiting, assertions |
| IClickableControl | Click/tap actions | `Click()`, `DoubleClick()`, `LongPress()` |
| ITextControl | Text display | `GetText()`, `AssertText*()` |
| IEditableTextControl | Text input | `Enter()`, `Clear()`, `SetText()` |
| IToggleControl | On/off state | `IsOn`, `Toggle()`, `SetState()` |
| ISelectorControl | Selection | `Select()`, `GetSelectedOption()` |
| IRangeControl | Numeric range | `GetValue()`, `SetValue()` |
| IContainerControl | Child scoping | `Find<T>()`, `TryFind<T>()` |

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
