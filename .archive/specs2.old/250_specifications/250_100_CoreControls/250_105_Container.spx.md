# specification ContainerControl

- **id**: SPC-105
- **version**: 1.0
- **created**: January 8, 2026
- **status**: Draft
- **level**: 1
- **requirement**: FR-100, FR-150
- **interfaces**: IControlObject, IContainerControlObject

---

## Overview

The Container control represents a grouping element that can scope child control searches. It validates the container scoping pattern essential for handling multiple similar controls in different sections of the UI.

---

## behavior

### Core Behaviors (IControlObject)

1. Container can be located by automation ID, name, or other locator strategies
2. Container reports existence state via `IsExists()` returning `bool`
3. Container reports visibility state via `IsVisible()` returning `bool`
4. Container reports enabled state via `IsEnabled()` returning `bool`
5. Container supports waiting for state changes with configurable timeout
6. Container supports assertion methods that throw on failure

### Container Behaviors (IContainerControlObject)

7. Container can scope child control searches via `Find<T>(Locator)` method
8. Container returns child controls scoped to container element
9. Container can enumerate children via `GetChildren<T>()` method
10. Container supports counting children via `GetChildCount()` method
11. Container scoping is relative to container element, not page root

### Scoping Behaviors

12. Controls found via container are scoped to container's subtree only
13. Nested containers maintain proper scope hierarchy
14. `Find<T>()` returns single control or throws if not found
15. `TryFind<T>()` returns control or null if not found
16. `FindAll<T>()` returns collection of matching controls

---

## boundary

### Scoping Boundaries

- `Find<T>()` searches only within container's DOM/visual subtree
- `Find<T>()` with locator matching element outside container returns not found
- `Find<T>()` with locator matching multiple elements within container throws `AmbiguousElementException`
- `FindAll<T>()` returns empty collection if no matches
- `GetChildren<T>()` returns immediate children only (not descendants)

### Container State Boundaries

- Container `IsVisible()` returns `false` if container is hidden
- Hidden container's children are also hidden
- Disabled container may still have enabled children (platform-dependent)
- Empty container is valid (`GetChildCount()` returns 0)

### Wait Boundaries

- Wait methods on container apply to container element only
- Child control wait methods are independent
- Wait timeout uses `DefaultWait` from context when not specified

### Type Boundaries

- `Find<T>()` type parameter must be a control interface (IControlObject or derived)
- `GetChildren<T>()` type parameter filters children by interface type
- Invalid type parameter throws compile-time error

---

## acceptance

### Existence and Location

```gherkin
Scenario: Container is located by automation ID
  Given a page with a container having AutomationId "formSection"
  When I create a Container control with locator By.AutomationId("formSection")
  Then IsExists() returns true
```

### Child Scoping

```gherkin
Scenario: Find scopes to container
  Given a page with two buttons "Submit" - one in header, one in form container
  And a form container with AutomationId "mainForm"
  When I get the container By.AutomationId("mainForm")
  And I call Find<IClickableControlObject>(By.AutomationId("Submit"))
  Then it returns the Submit button inside the form container
  And not the Submit button in the header

Scenario: Find returns not found for element outside container
  Given a container with AutomationId "section1"
  And a button with AutomationId "outsideButton" outside section1
  When I call container.Find<IClickableControlObject>(By.AutomationId("outsideButton"))
  Then ElementNotFoundException is thrown

Scenario: FindAll returns all matching children
  Given a container with three checkboxes
  When I call container.FindAll<IToggleControlObject>(By.ClassName("checkbox"))
  Then it returns a collection of 3 checkbox controls
```

### TryFind Operations

```gherkin
Scenario: TryFind returns control when found
  Given a container with a button "Submit"
  When I call container.TryFind<IClickableControlObject>(By.AutomationId("Submit"))
  Then it returns the Submit button

Scenario: TryFind returns null when not found
  Given a container with no button "Missing"
  When I call container.TryFind<IClickableControlObject>(By.AutomationId("Missing"))
  Then it returns null
```

### Child Enumeration

```gherkin
Scenario: GetChildren returns immediate children
  Given a container with 2 direct child buttons and 1 nested button
  When I call container.GetChildren<IClickableControlObject>()
  Then it returns a collection of 2 buttons (not the nested one)

Scenario: GetChildCount returns number of children
  Given a container with 5 child controls
  When I call container.GetChildCount()
  Then it returns 5

Scenario: GetChildren returns empty for empty container
  Given an empty container
  When I call container.GetChildren<IControlObject>()
  Then it returns an empty collection
```

### Nested Container Scoping

```gherkin
Scenario: Nested containers maintain scope
  Given an outer container with inner container
  And inner container has a button "NestedButton"
  When I call outerContainer.Find<IContainerControlObject>(innerLocator)
  And I call innerContainer.Find<IClickableControlObject>(By.AutomationId("NestedButton"))
  Then it returns the button from the inner container scope
```

### Visibility Propagation

```gherkin
Scenario: Hidden container reports children as hidden context
  Given a hidden container with visible children
  When I get a child control from the container
  And I call childControl.IsVisible()
  Then it returns false (because parent is hidden)
```

---

## assumption

### Platform Assumptions

1. Underlying automation library supports scoped element search
2. Platform provides parent-child relationship traversal
3. DOM/visual tree structure is accessible
4. Relative locators work within container context

### Framework Assumptions

1. TestContext is initialized before container operations
2. Logging is available via `_context.Logger`
3. Timeout settings are configured in `_context.Timeouts`
4. Control factory can create typed controls from elements

### Element Assumptions

1. Container has accessible automation properties
2. Container children are in same DOM/visual tree
3. Shadow DOM elements require explicit handling (platform-specific)

---

## exclusion

### Explicitly Out of Scope

1. **Shadow DOM traversal** — Platform-specific handling required
2. **IFrame content** — Cross-document access requires separate context
3. **Virtual scrolling containers** — Item virtualization is platform-specific
4. **Dynamic child loading** — Loading indicators/spinners are app concern
5. **Layout properties** — Width, height, positioning is visual, not behavioral
6. **Child ordering guarantees** — DOM order may differ from visual order
7. **Drag-drop targets** — Container as drop zone is separate capability

### Deferred to Platform Implementation

1. Scroll container into view
2. Shadow root access
3. Cross-frame element access
4. Performance optimization for large child counts

---

## Platform Implementation Notes

### MAUI (AppiumElement)

```
Control: Frame, Grid, StackLayout, ContentView, Border
Locator: AutomationId, Name, XPath
Find: element.FindElement(locator) with relative XPath
FindAll: element.FindElements(locator)
GetChildren: element.FindElements(By.XPath("./*"))
Note: XPath must be relative (start with . or ./)
```

### Blazor (IWebElement)

```
Control: <div>, <section>, <form>, <article>, <aside>, <main>
Locator: id, data-testid, CSS selector, XPath
Find: element.FindElement(locator) - CSS/XPath relative
FindAll: element.FindElements(locator)
GetChildren: element.FindElements(By.XPath("./*"))
Note: CSS selectors are scoped automatically; XPath needs relative path
```

### WPF (AutomationElement)

```
Control: Panel, Grid, StackPanel, Border, GroupBox
Locator: AutomationId, Name, TreeScope.Descendants
Find: TreeWalker or FindFirst with TreeScope.Descendants
FindAll: FindAll with TreeScope.Descendants
GetChildren: FindAll with TreeScope.Children
Note: Use TreeScope.Descendants for subtree, Children for immediate
```

---

## Method Signatures

### IControlObject Methods

| Method | Signature | Returns | Description |
| ------ | --------- | ------- | ----------- |
| IsExists | `IsExists()` | `bool` | Check if element exists |
| IsVisible | `IsVisible()` | `bool` | Check if element is visible |
| IsEnabled | `IsEnabled()` | `bool` | Check if element is enabled |
| WaitExists | `WaitExists(bool? expected, int? timeoutMs)` | `bool` | Wait for existence state |
| WaitVisible | `WaitVisible(bool? expected, int? timeoutMs)` | `bool` | Wait for visibility state |
| WaitEnabled | `WaitEnabled(bool? expected, int? timeoutMs)` | `bool` | Wait for enabled state |
| AssertExists | `AssertExists(bool? expected, string? message, int? timeoutMs)` | `void` | Assert existence state |
| AssertVisible | `AssertVisible(bool? expected, string? message, int? timeoutMs)` | `void` | Assert visibility state |
| AssertEnabled | `AssertEnabled(bool? expected, string? message, int? timeoutMs)` | `void` | Assert enabled state |

### IContainerControlObject Methods

| Method | Signature | Returns | Description |
| ------ | --------- | ------- | ----------- |
| Find | `Find<T>(Locator locator)` | `T` | Find single child control |
| TryFind | `TryFind<T>(Locator locator)` | `T?` | Find child or return null |
| FindAll | `FindAll<T>(Locator locator)` | `IReadOnlyList<T>` | Find all matching children |
| GetChildren | `GetChildren<T>()` | `IReadOnlyList<T>` | Get immediate children by type |
| GetChildCount | `GetChildCount()` | `int` | Count immediate children |

### Type Constraints

```csharp
where T : IControlObject
```

---

## Usage Examples

### Scoped Control Access

```csharp
// Find form container
var form = page.Find<IContainerControlObject>(By.AutomationId("loginForm"));

// Find controls within form scope
var usernameEntry = form.Find<IEditableTextControlObject>(By.AutomationId("username"));
var passwordEntry = form.Find<IEditableTextControlObject>(By.AutomationId("password"));
var submitButton = form.Find<IClickableControlObject>(By.AutomationId("submit"));

// Enter credentials and submit
usernameEntry.SetText("user@example.com");
passwordEntry.SetText("password123");
submitButton.Click();
```

### Iterating Children

```csharp
// Get all checkboxes in a settings section
var settingsSection = page.Find<IContainerControlObject>(By.AutomationId("settings"));
var checkboxes = settingsSection.FindAll<IToggleControlObject>(By.ClassName("setting-checkbox"));

// Check all checkboxes
foreach (var checkbox in checkboxes)
{
    checkbox.Check();
}
```

### Nested Container Navigation

```csharp
// Navigate nested structure
var mainContent = page.Find<IContainerControlObject>(By.AutomationId("mainContent"));
var sidebar = mainContent.Find<IContainerControlObject>(By.AutomationId("sidebar"));
var menuItem = sidebar.Find<IClickableControlObject>(By.Text("Settings"));
menuItem.Click();
```

---

## Related Documents

- [250_100_INDEX.md](250_100_INDEX.md) — Core Controls Index
- [250_001_IControlObject.spx.md](../250_000_Foundation/250_001_IControlObject.spx.md) — Base interface
- [250_003_IContainerScope.spx.md](../250_000_Foundation/250_003_IContainerScope.spx.md) — Container scoping interface
- [250_005_InterfaceHierarchy.spx.md](../250_000_Foundation/250_005_InterfaceHierarchy.spx.md) — Interface hierarchy
