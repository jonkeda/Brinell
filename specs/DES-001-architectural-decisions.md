# DES-001: Architectural Decisions

**Version:** 3.0  
**Status:** Active  
**Last Updated:** December 2025  
**Relates To:** SPEC-001 (Core Architecture), REQ-001 (Functional Requirements)

---

## 1. Purpose

This document records the key architectural decisions made in designing the UI Test Framework v3.0, including the rationale, alternatives considered, and trade-offs.

---

## 2. Decision Summary

| ID | Decision | Status | Impact |
|----|----------|--------|--------|
| AD-001 | Core Contains Only Interfaces | **ADOPTED** | High |
| AD-002 | No Adapter Abstraction Layer | **ADOPTED** | High |
| AD-003 | Platform-Specific Base Classes | **ADOPTED** | High |
| AD-004 | Navigation Returns Void | **ADOPTED** | Medium |
| AD-005 | All Base Methods Virtual | **ADOPTED** | Medium |
| AD-006 | Four-Tier State Verification | **ADOPTED** | Medium |
| AD-007 | CSV Structured Logging | **ADOPTED** | Low |
| AD-008 | Platform Enum (Not String) | **ADOPTED** | Low |

---

## 3. AD-001: Core Contains Only Interfaces

### 3.1 Context

In v1.0 and v2.0, the Core project contained base class implementations that platform projects inherited from. This included `ControlObjectBase`, `PageObjectBase`, and various adapter abstractions.

### 3.2 Decision

**Core project contains ONLY interfaces.** No base classes, no adapter implementations, no platform-specific code.

Core provides:
- Interface contracts (`ITestContext`, `IPageObject`, `IControlObject`, etc.)
- Platform enum with extension methods
- Exception types
- Logging interfaces
- Utilities (configuration, attributes)

Each platform project provides its own complete base class hierarchy.

### 3.3 Rationale

**Benefits:**
1. **Simpler Core** - Easier to understand, maintain, and document
2. **Platform Flexibility** - Each platform can optimize for its native driver
3. **No Forced Compromises** - Platforms don't inherit limitations from shared base classes
4. **Clear Boundaries** - Interface contracts are explicit
5. **Better Testing** - Each platform can be tested independently

**Drawbacks:**
1. **Code Duplication** - Similar code across platform base classes
2. **Maintenance Overhead** - Changes need to be applied to multiple platforms
3. **Learning Curve** - Need to understand platform-specific implementations

### 3.4 Alternatives Considered

#### Alternative 1: Keep Shared Base Classes (v2.0 Pattern)

```csharp
// Core contains base classes
public abstract class ControlObjectBase : IControlObject
{
    protected IDriverAdapter Driver { get; }
    // Shared implementation
}

// Platform inherits
public class WpfControlBase : ControlObjectBase
{
    // WPF-specific additions
}
```

**Rejected Because:**
- Forces all platforms through same abstraction
- Adapter layer adds complexity and performance overhead
- Hard to add platform-specific features
- Creates tight coupling between Core and platform concepts

#### Alternative 2: Mixins/Traits Pattern

Use composition instead of inheritance.

**Rejected Because:**
- C# doesn't have native mixin support
- Would require interface default implementations (limited in C# 8)
- Adds complexity without significant benefit

### 3.5 Consequences

**Positive:**
- Platform projects are self-contained
- Native driver performance
- Easier to add new platforms
- Clearer separation of concerns

**Negative:**
- More code overall (duplication across platforms)
- Need to keep platform implementations in sync
- No compile-time guarantee of consistent behavior across platforms

**Mitigation:**
- Use interface contracts to enforce consistency
- Comprehensive test suites for each platform
- Code reviews to ensure pattern compliance
- Documentation of expected behaviors

### 3.6 Status

**ADOPTED** in v3.0 (December 2025)

**Related Decisions:** AD-002 (No Adapters), AD-003 (Platform-Specific Base Classes)

---

## 4. AD-002: No Adapter Abstraction Layer

### 4.1 Context

v1.0-v2.0 used adapter pattern to abstract differences between FlaUI, Appium, and Selenium:

```csharp
public interface IDriverAdapter
{
    IElementAdapter FindElement(string automationId);
}

public interface IElementAdapter
{
    void Click();
    string GetText();
}
```

Platforms implemented adapters that wrapped native drivers.

### 4.2 Decision

**Remove all adapter abstractions.** Platform implementations access native drivers (FlaUI, Appium, Selenium) directly.

```csharp
// WPF - Direct FlaUI access
public class ButtonControl : ContentControlBase
{
    public override void Click()
    {
        CheckClickable();
        
        var element = _context.FindElement(_automationId);  // Returns FlaUI AutomationElement
        element.Click();  // Direct FlaUI call
        
        Logger.LogAction(...);
    }
}
```

### 4.3 Rationale

**Benefits:**
1. **Performance** - No indirection through adapter layer
2. **Full Native Access** - Can use all native driver capabilities
3. **Simpler Code** - Fewer layers, easier to debug
4. **Better Stack Traces** - Direct path to native driver
5. **Platform Optimization** - Each platform can optimize independently

**Drawbacks:**
1. **No Shared Element Code** - Element operations not shared
2. **Platform Learning Curve** - Need to understand native drivers

### 4.4 Alternatives Considered

#### Alternative 1: Keep Adapter Layer (v2.0 Pattern)

**Rejected Because:**
- Adapters added complexity without value
- Native APIs are fundamentally different (FlaUI vs Selenium)
- Adapter "lowest common denominator" limited capabilities
- Performance overhead for every operation
- Debugging harder (extra layer in stack traces)

#### Alternative 2: Thin Adapter with Pass-Through

Minimal adapter that mostly forwards to native driver.

**Rejected Because:**
- Still adds overhead
- If mostly pass-through, why have it at all?
- Doesn't solve the "different native APIs" problem

### 4.5 Consequences

**Positive:**
- Native performance
- Full access to platform capabilities
- Simpler codebase
- Better debugging experience

**Negative:**
- Can't share element operation code
- Each platform must implement its own element operations
- Platform-specific knowledge required

**Mitigation:**
- Interface contracts ensure consistent API
- Copy-paste acceptable for simple operations
- Documentation explains platform differences

### 4.6 Status

**ADOPTED** in v3.0 (December 2025)

**Related Decisions:** AD-001 (Interfaces Only), AD-003 (Platform-Specific Base Classes)

---

## 5. AD-003: Platform-Specific Base Classes

### 5.1 Context

With Core containing only interfaces (AD-001) and no adapter layer (AD-002), each platform needs its own complete base class hierarchy.

### 5.2 Decision

**Each platform project contains its own complete base class hierarchy:**

```
Platform.Wpf/
├── ControlBase : IControlObject
├── PageBase : IPageObject
├── BusyPageBase : PageBase
├── ContentControlBase : ControlBase, IContentControl
├── TextControlBase : ControlBase, ITextControl
└── ... (all capability base classes)

Platform.Maui/
├── ControlBase : IControlObject
├── PageBase : IPageObject
└── ... (same hierarchy, different implementation)

Platform.Html/
├── ControlBase : IControlObject
├── PageBase : IPageObject
└── ... (same hierarchy, different implementation)
```

### 5.3 Rationale

**Benefits:**
1. **Platform Independence** - Each platform fully self-contained
2. **Platform Optimization** - Can optimize for native driver
3. **Platform-Specific Features** - Can add platform-specific methods
   - WPF: `GetAutomationPatterns()`
   - HTML: `GetCssClasses()`, `GetAttribute()`
4. **Easier Understanding** - All platform code in one place

**Drawbacks:**
1. **Code Duplication** - Similar base class implementations
2. **Maintenance** - Changes need to be applied to all platforms
3. **Consistency Risk** - Platforms might drift apart

### 5.4 Alternatives Considered

#### Alternative 1: Shared Base Classes in Core (v2.0)

**Rejected** - See AD-001 rationale

#### Alternative 2: Code Generation

Generate platform base classes from templates.

**Rejected Because:**
- Adds build complexity
- Generated code hard to debug
- Loses flexibility for platform-specific optimizations
- Templates become complex to maintain

### 5.5 Consequences

**Positive:**
- Complete platform independence
- Can add platform-specific features freely
- Easier to understand one platform at a time

**Negative:**
- Need to keep implementations in sync manually
- More total code to maintain

**Mitigation:**
- Interface contracts enforce API consistency
- Test suites verify behavior consistency
- Code reviews check for pattern compliance
- Documentation describes expected behaviors

### 5.6 Status

**ADOPTED** in v3.0 (December 2025)

**Related Decisions:** AD-001 (Interfaces Only), AD-002 (No Adapters)

---

## 6. AD-004: Navigation Returns Void

### 6.1 Context

v1.0-v2.0 navigation methods returned target page objects:

```csharp
// OLD PATTERN
public SettingsPage NavigateToSettings()
{
    SettingsButton.Click();
    var settings = new SettingsPage(Context);
    settings.WaitForPageReady();
    return settings;
}

// In test
var settings = homePage.NavigateToSettings();
```

### 6.2 Decision

**Navigation methods return void.** Tests create page objects explicitly.

```csharp
// NEW PATTERN
public void NavigateToSettings()
{
    Log("Navigating to Settings");
    SettingsButton.Click();
}

// In test
homePage.NavigateToSettings();
var settings = new SettingsPage(Context);
settings.WaitForPageReady();
```

### 6.3 Rationale

**Benefits:**
1. **Clearer Ownership** - Tests own page object lifecycle
2. **Explicit Waiting** - Tests decide when to wait for page
3. **Better Testability** - Can test navigation independently
4. **More Flexible** - Tests can use custom wait strategies
5. **Simpler Page Objects** - Navigation doesn't manage page creation

**Drawbacks:**
1. **More Verbose** - Tests have more lines
2. **Manual Page Creation** - Test must create page object
3. **Easy to Forget Wait** - Test might forget `WaitForPageReady()`

### 6.4 Alternatives Considered

#### Alternative 1: Keep Return Page Pattern (v2.0)

**Rejected Because:**
- Hides page creation inside navigation
- Test loses control over page initialization
- Harder to test navigation separately from page creation
- Couples navigation to specific page type

#### Alternative 2: Return IPageObject Interface

```csharp
public IPageObject NavigateToSettings()
{
    SettingsButton.Click();
    return CreatePage<SettingsPage>();
}
```

**Rejected Because:**
- Still couples navigation to page creation
- Test still doesn't control page lifecycle
- Adds complexity for little benefit

### 6.5 Consequences

**Positive:**
- Tests have full control over page lifecycle
- Navigation logic is simpler
- Easier to test navigation independently
- More explicit test code

**Negative:**
- Tests are slightly more verbose
- Easy to forget `WaitForPageReady()` call

**Mitigation:**
- Documentation emphasizes importance of `WaitForPageReady()`
- Test templates include wait pattern
- Code reviews catch missing waits
- Consider analyzer rule to detect missing waits

### 6.6 Status

**ADOPTED** in v3.0 (December 2025)

---

## 7. AD-005: All Base Methods Virtual

### 7.1 Context

Need to decide whether base class methods should be virtual, sealed, or abstract.

### 7.2 Decision

**All base class methods are `virtual`.** Derived classes can override any behavior.

```csharp
public class ControlBase : IControlObject
{
    public virtual bool IsVisible() { ... }
    public virtual bool WaitVisible(bool expected, int? timeout) { ... }
    public virtual void CheckVisible(bool expected, int? timeout) { ... }
    public virtual void AssertVisible(string? message) { ... }
}
```

### 7.3 Rationale

**Benefits:**
1. **Maximum Flexibility** - Any method can be customized
2. **Consistent Pattern** - All methods follow same rule
3. **Platform Overrides** - Platforms can optimize any operation
4. **Test Customization** - Custom controls can override behaviors

**Drawbacks:**
1. **Breaking Changes Risk** - Overrides might break with framework updates
2. **Testing Overhead** - More code paths to test

### 7.4 Alternatives Considered

#### Alternative 1: Selective Virtual

Mark only "extension points" as virtual.

**Rejected Because:**
- Hard to predict what users will want to override
- Inconsistent pattern
- Might need to make more virtual later (breaking change)

#### Alternative 2: All Sealed

Only allow composition, not inheritance.

**Rejected Because:**
- Reduces flexibility significantly
- Composition harder to use for small customizations
- Doesn't fit well with page object pattern

### 7.5 Consequences

**Positive:**
- Users can customize anything
- Platforms can optimize anything
- Consistent extensibility pattern

**Negative:**
- More testing surface area
- Risk of users depending on implementation details

**Mitigation:**
- Clear documentation about stable public API
- Semantic versioning for breaking changes
- Virtual method behavior documented

### 7.6 Status

**ADOPTED** since v1.0, **REINFORCED** in v3.0

---

## 8. Decision Record Template

For new architectural decisions, use this template:

```markdown
## X. AD-XXX: [Decision Title]

### X.1 Context
[Describe the situation and problem]

### X.2 Decision
[State the decision clearly]

### X.3 Rationale
**Benefits:**
[List benefits]

**Drawbacks:**
[List drawbacks]

### X.4 Alternatives Considered
[Alternatives and why rejected]

### X.5 Consequences
**Positive:**
[Positive outcomes]

**Negative:**
[Negative outcomes]

**Mitigation:**
[How to address negative consequences]

### X.6 Status
[PROPOSED | ADOPTED | SUPERSEDED | DEPRECATED]
```

---

## 9. Change History

| Version | Date | Decisions Added/Changed |
|---------|------|------------------------|
| 3.0 | Dec 2025 | AD-001 through AD-008 |

---

*See also: [DES-002: Interface-Based Design](DES-002-interface-based-design.md), [DES-003: Native Driver Access](DES-003-native-driver-access.md)*
