# 21d30 - UITestFramework Container Support Architecture Proposal

## Status: Proposed
## Date: 2025-12-25

---

## 1. Problem Statement

### 1.1 The Container Issue

The current `ControlBase` class uses a non-virtual `FindElement()` method that always searches from the main window:

```csharp
// ControlBase.cs
protected AutomationElement? FindElement() => _context.FindElement(AutomationId);
```

When testing list items (e.g., `UserModelItemControl`), each item contains controls with the same AutomationId (e.g., `UserModelName`). The framework needs to search **within a container element**, not from the window root.

The current workaround in `UserModelItemControl.cs` uses `new` to shadow `FindElement()`:

```csharp
public class LabelControl : Oravey.UITestFramework.Wpf.Controls.LabelControl
{
    private readonly AutomationElement? _container;

    protected new AutomationElement? FindElement()
    {
        if (_container != null)
            return _container.FindFirstDescendant(cf => cf.ByAutomationId(AutomationId));
        return base.FindElement();
    }
}
```

**Problem**: The `new` keyword only shadows when called directly on the derived type. When base class methods like `GetText()` call `FindElement()`, they call the base implementation, not the shadowed one. This causes all list items to return the first item's values.

### 1.2 Duplicate LabelControl Classes

There are currently **two LabelControl classes**:

| Location | Purpose |
|----------|---------|
| `Oravey.UITestFramework.Wpf/Controls/LabelControl.cs` | Framework control - searches from window root |
| `Oravey.Tools.Wpf.UITests/Controls/UserModelItemControl.cs` | Test-specific override with container support |

**Why this happened**: The framework was designed for simple dialogs where each control has a unique AutomationId. When list items with repeated AutomationIds were introduced, the framework lacked container support. Rather than modifying the framework, a local override was created as a quick fix.

**Problems with this approach**:
- Every control type needs a duplicate class with container support
- The `new` keyword shadowing doesn't work for virtual method calls from base
- Violates DRY - logic duplicated between framework and test project
- Test authors may unknowingly use the wrong control class

### 1.3 Context Exposes FindElement Methods

`FlaUITestContext` currently exposes element-finding methods:

```csharp
public AutomationElement? FindElement(string automationId)
public AutomationElement? FindElementByName(string name)
```

**Problems**:
- Tests can bypass the control abstraction layer
- Encourages direct element manipulation instead of using ControlObjects
- Makes tests brittle and harder to maintain
- Violates the Page Object pattern principle of encapsulation

---

## 2. Proposed Architecture Changes

### 2.1 Make FindElement Virtual with Container Support

**Change**: Modify `ControlBase` to support container-scoped element searching.

```csharp
// ControlBase.cs - PROPOSED
public abstract class ControlBase : IControlObject
{
    protected readonly FlaUITestContext _context;
    protected readonly IPageObject? _page;
    protected readonly AutomationElement? _container;

    public string AutomationId { get; }

    /// <summary>
    /// Create a control that searches from the main window.
    /// </summary>
    protected ControlBase(FlaUITestContext context, IPageObject? page, string automationId)
        : this(context, page, container: null, automationId)
    {
    }

    /// <summary>
    /// Create a control that searches within a container element.
    /// Use this for controls inside list items or repeated templates.
    /// </summary>
    protected ControlBase(
        FlaUITestContext context, 
        IPageObject? page, 
        AutomationElement? container,
        string automationId)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _page = page;
        _container = container;
        AutomationId = automationId ?? throw new ArgumentNullException(nameof(automationId));
    }

    /// <summary>
    /// Find the element by AutomationId.
    /// Searches within container if specified, otherwise from window root.
    /// </summary>
    protected virtual AutomationElement? FindElement()
    {
        if (_container != null)
        {
            return _container.FindFirstDescendant(cf => cf.ByAutomationId(AutomationId));
        }
        return _context.FindElementInternal(AutomationId);
    }
}
```

**Benefits**:
- Single source of truth for element finding
- `virtual` allows proper polymorphism - derived classes can override
- Container support is built into the framework
- No need for duplicate control classes in test projects

### 2.2 Add Container Constructors to All Control Types

Each control class needs a constructor accepting a container:

```csharp
// LabelControl.cs - PROPOSED
public class LabelControl : ContentControlBase, ILabel
{
    public LabelControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public LabelControl(
        FlaUITestContext context, 
        IPageObject? page, 
        AutomationElement container, 
        string automationId)
        : base(context, page, container, automationId)
    {
    }

    // ... rest unchanged
}
```

### 2.3 Hide FindElement from Test Code

**Change**: Make element-finding methods internal to the framework.

```csharp
// FlaUITestContext.cs - PROPOSED
public class FlaUITestContext
{
    // PUBLIC API - for test code
    public AutomationElement MainWindow { get; }
    public int DefaultTimeoutMs { get; }
    public int ShortTimeoutMs { get; }
    
    public bool WaitFor(Func<bool> condition, int? timeoutMs = null, string? description = null);
    public void Log(string message);

    // INTERNAL API - for framework controls only
    internal AutomationElement? FindElementInternal(string automationId)
    {
        return MainWindow?.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
    }

    internal AutomationElement? FindElementByNameInternal(string name)
    {
        return MainWindow?.FindFirstDescendant(cf => cf.ByName(name));
    }

    // REMOVED from public API:
    // public AutomationElement? FindElement(string automationId)
    // public AutomationElement? FindElementByName(string name)
}
```

**Benefits**:
- Tests must use ControlObjects for all element interaction
- Framework internals are properly encapsulated
- Prevents accidental misuse of low-level APIs
- Makes the Page Object pattern the only supported approach

### 2.4 Introduce IContainerControl Interface

For controls that contain other controls (list items, panels):

```csharp
// IContainerControl.cs - PROPOSED
public interface IContainerControl : IControlObject
{
    /// <summary>
    /// The underlying automation element for this container.
    /// Used to create child controls that search within this container.
    /// </summary>
    AutomationElement ContainerElement { get; }
}
```

```csharp
// Usage in test code
public class UserModelItemControl : ControlBase, IContainerControl
{
    public AutomationElement ContainerElement { get; }
    
    public LabelControl NameLabel { get; }
    public ButtonControl EditButton { get; }

    public UserModelItemControl(
        FlaUITestContext context,
        PageBase? page,
        AutomationElement container,
        int index)
        : base(context, page, container, $"UserModelItem[{index}]")
    {
        ContainerElement = container;
        
        // Child controls search within this container
        NameLabel = new LabelControl(context, page, container, "UserModelName");
        EditButton = new ButtonControl(context, page, container, "EditUserModelButton");
    }
}
```

---

## 3. Migration Plan

### Phase 1: Framework Changes (Non-Breaking)
1. Add `_container` field and container constructor to `ControlBase`
2. Make `FindElement()` virtual
3. Add container constructors to all control types
4. Keep existing public constructors for backward compatibility

### Phase 2: Internal API Migration
1. Rename `FindElement` to `FindElementInternal` in `FlaUITestContext`
2. Make it `internal`
3. Update `ControlBase` to use the internal method
4. Add `[Obsolete]` to any remaining public element-finding methods

### Phase 3: Test Code Cleanup
1. Remove duplicate control classes from `UserModelItemControl.cs`
2. Update all container-aware controls to use framework versions
3. Update any tests directly using `context.FindElement()` to use controls

### Phase 4: Remove Deprecated APIs
1. Remove obsolete public `FindElement` methods
2. Final cleanup and documentation update

---

## 4. Impact Assessment

### Breaking Changes
- `context.FindElement()` will no longer be available to test code
- Tests using direct element access will need refactoring

### Non-Breaking Changes
- Existing controls without containers will work unchanged
- New container constructors are additive

### Files to Modify

| File | Changes |
|------|---------|
| `ControlBase.cs` | Add container field, virtual FindElement |
| `ContentControlBase.cs` | Add container constructor |
| `LabelControl.cs` | Add container constructor |
| `ButtonControl.cs` | Add container constructor |
| `TextBoxControl.cs` | Add container constructor |
| `CheckBoxControl.cs` | Add container constructor |
| `FlaUITestContext.cs` | Make FindElement internal |
| `UserModelItemControl.cs` | Remove duplicate classes |
| `SystemModelItemControl.cs` | Update to use framework controls |

---

## 5. Alternative Approaches Considered

### 5.1 Keep Duplicate Classes (Rejected)
- Continue with test-specific control overrides
- **Rejected**: Violates DRY, shadowing doesn't work properly

### 5.2 Use Composition Instead of Inheritance (Considered)
- Controls wrap an element finder instead of inheriting search behavior
- **Considered**: More flexible but larger refactor

### 5.3 Pass Element Directly to Controls (Rejected)
- Find element first, pass to control constructor
- **Rejected**: Loses lazy evaluation, can't handle dynamic content

---

## 6. Acceptance Criteria

1. ✅ All framework control types support container-scoped searching
2. ✅ `FindElement()` is virtual and respects container scope
3. ✅ No duplicate control classes in test projects
4. ✅ `FlaUITestContext.FindElement()` is not accessible from test code
5. ✅ All existing tests pass without modification (backward compatible)
6. ✅ List item controls correctly return their own values, not first item's

---

## 7. References

- [FlaUI Documentation](https://github.com/FlaUI/FlaUI)
- [Page Object Pattern](https://martinfowler.com/bliki/PageObject.html)
- [UI Automation in .NET](https://docs.microsoft.com/en-us/dotnet/framework/ui-automation/)
