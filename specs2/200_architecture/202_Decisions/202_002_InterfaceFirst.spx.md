# ADR-002: Interface-First Design

**Block:** 202 decision  
**Edition:** 🟡Ⅱ Core  
**Version:** 1.0  
**Created:** January 7, 2026

---

## decision ADR-002

- **title**: Interface-First Design for All Control Types
- **status**: accepted
- **date**: 2026-01-07
- **context**: Framework needs consistent API across platforms while allowing platform-specific implementations and enabling unit testing with mocks.
- **decision**: Define all control contracts as interfaces in Core; platform packages implement these interfaces.
- **consequences**: Consistent API, mockable for testing, requires interface discipline, enables capability composition.

---

## 1. Context

The Brinell framework provides ControlObjects for UI automation. These controls must:

1. **Work consistently across platforms** — Same API for Button whether MAUI or Blazor
2. **Support unit testing** — Tests can mock controls without running apps
3. **Allow platform-specific behavior** — MAUI button click differs from Blazor
4. **Support capability composition** — Controls can implement multiple capabilities

### Current Challenge

Controls have varying capabilities:
- Button: clickable
- Entry: clickable + editable text
- CheckBox: clickable + toggle
- Slider: range value

How do we represent these capabilities while maintaining:
- Type safety
- Discoverability
- Testability
- Consistency

---

## 2. Decision

Adopt **Interface-First Design**:

1. All control capabilities are defined as interfaces in Brinell.Core
2. Each interface represents a single capability
3. Concrete controls implement relevant interfaces
4. Test code works with interfaces, not concrete classes

### Interface Hierarchy

```csharp
// Core interfaces
public interface IControlObject
{
    string AutomationId { get; }
    bool IsExists();
    bool IsVisible();
    bool IsEnabled();
    void AssertExists(string? message = null);
    // ... state and assertion methods
}

public interface IClickableControl : IControlObject
{
    void Click();
    void Tap();
    // ... click actions
}

public interface ITextControl : IControlObject
{
    string GetText();
    void AssertTextEquals(string expected, string? message = null);
    // ... text reading and assertions
}

public interface IEditableTextControl : ITextControl
{
    void Enter(string text);
    void Clear();
    void SetText(string text);
    // ... text manipulation
}
```

### Capability Composition

Controls implement the interfaces they support:

```csharp
// A Button is clickable and may have text
public class ButtonControl : ClickableControlBase, ITextControl
{
    // Gets click capability from base
    // Implements ITextControl for button label
}

// An Entry is clickable, has text, and is editable
public class EntryControl : EditableTextControlBase, IClickableControl
{
    // Gets text editing from base
    // Implements IClickableControl for focus
}

// A CheckBox is clickable and toggleable
public class CheckBoxControl : ToggleControlBase, IClickableControl
{
    // Gets toggle from base
    // Implements IClickableControl for checking
}
```

---

## 3. Consequences

### Positive

| Benefit | Description |
|---------|-------------|
| **Mockability** | All interfaces can be mocked for unit tests |
| **Type Safety** | Compiler ensures correct capability usage |
| **Discoverability** | IntelliSense shows available operations per interface |
| **Composition** | Controls gain capabilities by implementing interfaces |
| **Consistency** | Same interface = same behavior expectations |
| **Flexibility** | One control can implement multiple capability interfaces |

### Negative

| Trade-off | Mitigation |
|-----------|------------|
| **More types** | Organized namespace structure |
| **Interface explosion** | Limit to meaningful capabilities |
| **Learning curve** | Clear documentation, consistent patterns |
| **Casting needed** | Factory methods return appropriate interface |

### Neutral

| Aspect | Notes |
|--------|-------|
| **Documentation** | Each interface fully documented |
| **Testing patterns** | Standard mocking approaches work |

---

## 4. Alternatives Considered

### Alternative 1: Abstract Base Class Only

```csharp
// NOT CHOSEN
public abstract class ControlObject
{
    public abstract void Click(); // Not all controls click
    public abstract string GetText(); // Not all controls have text
}
```

**Rejected because:**
- Forces all controls to implement all methods
- NotSupportedException for missing capabilities is poor design
- Cannot mock specific capabilities
- No compile-time capability checking

### Alternative 2: Marker Interfaces + Methods

```csharp
// NOT CHOSEN
public interface IClickable { } // Just a marker
public class Button : ControlObject, IClickable { }
// Click method on base class, checked at runtime
```

**Rejected because:**
- No compile-time method availability checking
- Runtime errors instead of compile errors
- Markers don't define contracts

### Alternative 3: Capability Flags

```csharp
// NOT CHOSEN
public class ControlObject
{
    public ControlCapabilities Capabilities { get; }
    // Check capabilities at runtime
}
```

**Rejected because:**
- Runtime checking instead of compile-time
- No IntelliSense for available methods
- More error-prone for test writers

---

## 5. Design Rules

### Rule 1: Interfaces Define Contracts

```csharp
// ✅ CORRECT - Interface in Core defines contract
public interface IToggleControl : IControlObject
{
    bool IsOn { get; }
    void Toggle();
    void SetState(bool on);
}
```

### Rule 2: Base Classes Implement Interfaces

```csharp
// ✅ CORRECT - Base class in Platform implements interface
public abstract class ToggleControlBase : ControlBase, IToggleControl
{
    public abstract bool IsOn { get; }
    public virtual void Toggle() => SetState(!IsOn);
    public abstract void SetState(bool on);
}
```

### Rule 3: Single Interface per Capability

```csharp
// ✅ CORRECT - One interface per capability
public interface IClickableControl : IControlObject { ... }
public interface ITextControl : IControlObject { ... }

// ❌ WRONG - Combined capabilities
public interface IClickableTextControl : IControlObject { ... }
```

### Rule 4: Test Against Interfaces

```csharp
// ✅ CORRECT - Test uses interface
[Fact]
public void ClickableControl_Click_InvokesAction()
{
    var mock = new Mock<IClickableControl>();
    // ...
}

// ❌ WRONG - Test uses concrete class
[Fact]
public void ButtonControl_Click_InvokesAction()
{
    var button = new ButtonControl(...); // Requires running app
}
```

---

## 6. Interface Catalog

### State Interfaces (All Controls)

| Interface | Purpose |
|-----------|---------|
| IControlObject | Base state and assertions |

### Capability Interfaces

| Interface | Extends | Purpose |
|-----------|---------|---------|
| IClickableControl | IControlObject | Click/tap actions |
| ITextControl | IControlObject | Read text content |
| IEditableTextControl | ITextControl | Modify text content |
| IToggleControl | IControlObject | On/off state |
| ISelectorControl | IControlObject | Single selection |
| IMultiSelectorControl | ISelectorControl | Multiple selection |
| IRangeControl | IControlObject | Numeric range |
| IContainerControl | IControlObject | Child scoping |
| ICollectionControl | IControlObject | Item enumeration |
| IScrollableControl | IControlObject | Scroll operations |

---

## 7. Validation

This decision is validated when:

- [ ] All capabilities are expressed as interfaces in Core
- [ ] Platform controls implement appropriate interfaces
- [ ] Unit tests can mock any interface
- [ ] Compiler catches capability misuse
- [ ] IntelliSense shows correct methods per interface

---

## Related Decisions

- [ADR-001: Clean Architecture](202_001_CleanArchitecture.spx.md)
- [ADR-003: Platform Separation](202_003_PlatformSeparation.spx.md)
- [ADR-004: Control Interface Hierarchy](202_004_ControlHierarchy.spx.md)

---

## Related Documents

- [211_001_Interfaces.spx.md](../211_Modules/211_001_Interfaces.spx.md) — Interface module details
- [203_001_CoreLayer.spx.md](../203_Layers/203_001_CoreLayer.spx.md) — Core layer specification
