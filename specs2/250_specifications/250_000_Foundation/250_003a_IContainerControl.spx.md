# 250.003a IContainerControl Specification

**Block Type:** SPC (Specification)  
**ID:** 250.003a  
**Title:** IContainerControl Interface Specification  
**Status:** Draft  
**Version:** 1.1  
**Level:** 0 - Foundation

---

## 1. Overview

`IContainerControl` provides scoped control searching capabilities for controls that act as containers. It inherits from `IControlObject` and adds methods for dynamically finding controls within the container's scope. This is useful for containers where the exact content is not known at compile time.

For typed containers with known content, see [IContainerControlObject<T>](250_003_IContainerControlObject.spx.md).

### Interface Identity

- **Package:** `Brinell.Core`
- **Namespace:** `Brinell.Core.Interfaces`
- **Dependencies:** `IControlObject`, `Locator`
- **Implementors:** `ContainerControlBase`, `FrameControl`, `ModalDialogControl`, platform-specific containers

---

## 2. Behavior

### 2.1 Interface Definition

```csharp
public interface IContainerControl : IControlObject
{
    /// <summary>
    /// Find a control within this container's scope.
    /// </summary>
    /// <typeparam name="T">Type of control to find.</typeparam>
    /// <param name="locator">Locator relative to this container.</param>
    /// <param name="timeoutMs">Timeout to wait for element. Null = use default.</param>
    /// <returns>The control.</returns>
    /// <exception cref="ElementNotFoundException">If control not found.</exception>
    T FindControl<T>(Locator locator, int? timeoutMs = null) where T : IControlObject;
    
    /// <summary>
    /// Find multiple controls within this container's scope.
    /// </summary>
    /// <typeparam name="T">Type of controls to find.</typeparam>
    /// <param name="locator">Locator relative to this container.</param>
    /// <param name="timeoutMs">Timeout to wait for elements. Null = use default.</param>
    /// <returns>List of controls. Empty if none found.</returns>
    IReadOnlyList<T> FindControls<T>(Locator locator, int? timeoutMs = null) where T : IControlObject;
    
    /// <summary>
    /// Check if a control exists within this container's scope.
    /// </summary>
    /// <param name="locator">Locator relative to this container.</param>
    /// <param name="timeoutMs">Timeout to wait. Null = use default.</param>
    /// <returns>True if control exists, false otherwise.</returns>
    bool ControlExists(Locator locator, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until control existence matches expected value.
    /// </summary>
    /// <param name="locator">Locator relative to this container.</param>
    /// <param name="expected">Expected existence. Null = skip operation.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    bool WaitControlExists(Locator locator, bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Get the count of controls matching the locator within this container.
    /// </summary>
    /// <param name="locator">Locator relative to this container.</param>
    /// <param name="timeoutMs">Timeout to wait. Null = use default.</param>
    /// <returns>Number of matching controls, or null if container not found.</returns>
    int? GetControlCount(Locator locator, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until control count matches expected value.
    /// </summary>
    /// <param name="locator">Locator relative to this container.</param>
    /// <param name="expected">Expected count. Null = skip.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitControlCount(Locator locator, int? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert control count matches expected value.
    /// </summary>
    void AssertControlCount(Locator locator, int? expected, string? message = null, int? timeoutMs = null);
}
```

### 2.2 Scoped Control Access

When searching within a container, all searches are scoped to that container's element:

```csharp
// Container scopes all searches
var modal = new ConfirmDialog(context, Locator.ByAutomationId("ConfirmModal"), page);
var okButton = modal.FindControl<ButtonControl>(Locator.ByAutomationId("OkBtn"));
var cancelButton = modal.FindControl<ButtonControl>(Locator.ByAutomationId("CancelBtn"));

// Frame scopes searches to frame content
var frame = new FrameControl(context, Locator.ByAutomationId("ContentFrame"), page);
var submitButton = frame.FindControl<ButtonControl>(Locator.ByAutomationId("Submit"));
```

**Behavior:**
- Control searches are scoped to the container's element
- Prevents finding elements outside the container's scope
- Inherits all IControlObject methods (IsExists, IsVisible, IsEnabled, etc.)

### 2.3 Inheritance Hierarchy

```
IControlObject
    └── IContainerControl
            ├── FrameControl (frames/iframes)
            ├── ModalDialogControl (modal dialogs)
            └── DynamicContainerControl (unknown content)
```

**Behavior:**
- `IContainerControl` is a control with scoping capabilities
- Inherits standard control methods from `IControlObject`
- Used when content is dynamic or unknown at compile time

---

## 3. Boundary

### 3.1 Container Not Found

| Scenario | Behavior |
|----------|----------|
| `FindControl<T>()` when container doesn't exist | Throws ElementNotFoundException |
| `ControlExists()` when container doesn't exist | Returns false |
| `IsExists()` (inherited) when container doesn't exist | Returns false |

### 3.2 Control Not Found

| Scenario | Behavior |
|----------|----------|
| `FindControl<T>()` when control doesn't exist | Throws ElementNotFoundException |
| `FindControls<T>()` when no controls match | Returns empty list |
| `ControlExists()` when control doesn't exist | Returns false |
| `GetControlCount()` when no controls match | Returns 0 |

---

## 4. Acceptance Criteria

### ACC-001: Container as Control

```gherkin
Given a ModalDialog that implements IContainerControl
When IsExists() is called
Then it checks the modal dialog element existence (inherited from IControlObject)

When IsVisible() is called
Then it checks the modal dialog visibility (inherited from IControlObject)
```

### ACC-002: Scoped Control Finding

```gherkin
Given a ModalDialog containing a form
When FindControl<ButtonControl>(Locator.ByAutomationId("Submit")) is called
Then it finds the submit button within the modal scope only
And it does not find a submit button outside the modal
```

### ACC-003: Control Existence

```gherkin
Given a Frame with a visible Username field
When ControlExists(Locator.ByAutomationId("Username")) is called
Then it returns true

Given a Frame without a NonExistent field
When ControlExists(Locator.ByAutomationId("NonExistent")) is called
Then it returns false
```

### ACC-004: Wait for Control

```gherkin
Given a container where a control appears after 500ms
And a timeout of 2000ms
When WaitControlExists(locator, true, 2000) is called
Then it returns true after approximately 500ms
```

---

## 5. Assumptions

- **ASM-001:** Container element is accessible via platform driver
- **ASM-002:** Container is a control first, scoping second
- **ASM-003:** Scoped searches find elements within container's DOM subtree
- **ASM-004:** Control creation uses platform-specific factories

---

## 6. Exclusions

- **EXC-001:** Page-level operations (navigation, title) — this is a control, not a page
- **EXC-002:** Typed child access — use IContainerControlObject<T> for that
- **EXC-003:** Cross-container references — containers are independent

---

## 7. Implementation Example

```csharp
public abstract class ContainerControlBase : ControlBase, IContainerControl
{
    protected ContainerControlBase(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public T FindControl<T>(Locator locator, int? timeoutMs = null) where T : IControlObject
    {
        var scopedLocator = locator.ScopedTo(Locator);
        var element = FindElementInScope(scopedLocator, timeoutMs);
        return CreateControl<T>(scopedLocator, element);
    }
    
    public IReadOnlyList<T> FindControls<T>(Locator locator, int? timeoutMs = null) where T : IControlObject
    {
        var scopedLocator = locator.ScopedTo(Locator);
        var elements = FindElementsInScope(scopedLocator, timeoutMs);
        return elements.Select((e, i) => CreateControl<T>(scopedLocator, e)).ToList();
    }
    
    public bool ControlExists(Locator locator, int? timeoutMs = null)
    {
        try
        {
            var scopedLocator = locator.ScopedTo(Locator);
            return TryFindElementInScope(scopedLocator) != null;
        }
        catch { return false; }
    }
    
    public bool WaitControlExists(Locator locator, bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return WaitHelper.WaitFor(() => ControlExists(locator) == expected.Value, timeout);
    }
    
    public int? GetControlCount(Locator locator, int? timeoutMs = null)
    {
        if (!IsExists()) return null;
        var scopedLocator = locator.ScopedTo(Locator);
        return FindElementsInScope(scopedLocator, timeoutMs).Count;
    }
    
    public bool WaitControlCount(Locator locator, int? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return WaitHelper.WaitFor(() => GetControlCount(locator) == expected.Value, timeout);
    }
    
    public void AssertControlCount(Locator locator, int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitControlCount(locator, expected, timeoutMs);
        var actual = GetControlCount(locator);
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected control count {expected} but was {actual}");
    }
    
    // Abstract - platform-specific element finding
    protected abstract object? TryFindElementInScope(Locator locator);
    protected abstract object FindElementInScope(Locator locator, int? timeoutMs);
    protected abstract IReadOnlyList<object> FindElementsInScope(Locator locator, int? timeoutMs);
    protected abstract T CreateControl<T>(Locator locator, object element) where T : IControlObject;
}
```

---

## 8. Usage Example

```csharp
// Modal dialog as container control
public class ConfirmDialog : ContainerControlBase
{
    public ButtonControl OkButton { get; }
    public ButtonControl CancelButton { get; }
    public LabelControl Message { get; }
    
    public ConfirmDialog(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        // Define controls as properties (like page object pattern)
        OkButton = new ButtonControl(context, Locator.ByAutomationId("OkBtn").ScopedTo(locator), page);
        CancelButton = new ButtonControl(context, Locator.ByAutomationId("CancelBtn").ScopedTo(locator), page);
        Message = new LabelControl(context, Locator.ByAutomationId("Message").ScopedTo(locator), page);
    }
}

// Test using container control
[Test]
public void CanConfirmDialog()
{
    var page = new SettingsPage(context);
    page.DeleteButton.Click();
    
    // Modal dialog is a control - check if it exists/visible
    var confirmDialog = new ConfirmDialog(context, Locator.ByAutomationId("ConfirmModal"), page);
    confirmDialog.WaitVisible(true);  // Inherited from IControlObject
    
    // Access defined controls
    confirmDialog.Message.AssertTextContains("Are you sure?");
    confirmDialog.OkButton.Click();
    
    // Wait for dialog to close
    confirmDialog.WaitVisible(false);
}
```

---

## Related Documents

- [IControlObject Specification](250_001_IControlObject.spx.md)
- [IContainerControlObject Specification](250_003_IContainerControlObject.spx.md)
- [IListContainerControlObject Specification](250_003b_IListContainerControlObject.spx.md)
- [Container Pattern](../../200_architecture/231_Patterns/231_004_ContainerPattern.spx.md)
