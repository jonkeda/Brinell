# functional ControlObjectPattern
- **id**: FR-002
- **title**: Abstraction for UI control interactions
- **priority**: high
- **status**: approved
- **tags**: core, controls, abstraction

The framework must provide abstraction for UI control interactions through the Control Object pattern.

## capabilities

### ControlIdentification
- **id**: FR-002.1
- **title**: Control identification via locators

Controls must be identifiable using the `ControlLocator` system defined in `Brinell.Core.Locators`.

**Locator Strategies:**
- AutomationId — XAML AutomationId or HTML data-automation-id
- Name — Name attribute
- Id — HTML id or AccessibilityId
- ClassName — Class name
- XPath — XPath expression
- Css — CSS selector (HTML only)
- TestId — data-testid attribute
- Text — Exact text content
- PartialText — Partial text match
- TagName — HTML tag name
- AccessibilityLabel — Accessibility label

**Fluent API:**
Controls can be located using the `By` factory class:
```csharp
By.AutomationId("submitButton")
By.Css(".btn-primary")
By.XPath("//button[@type='submit']")
```

**Chained Locators:**
Locators can be chained for hierarchical element finding:
```csharp
By.AutomationId("form").Then(By.Css("input[name='email']"))
```

**Page-Level Defaults:**
Page objects may define a default locator strategy. Controls on that page inherit the default unless overridden.

**Implicit Conversion:**
A string value implicitly converts to `ControlLocator` using AutomationId strategy for backward compatibility.

### ControlStateVerification
- **id**: FR-002.2
- **title**: Control state checking

Controls must support:
- Existence checking
- Visibility checking
- Enabled/disabled state checking
- Clickability checking (visible AND enabled)

### ControlActions
- **id**: FR-002.3
- **title**: Verified control actions

Controls must verify preconditions before performing actions. Controls must fail fast with clear error messages when preconditions not met. Controls must log all actions performed.

**Timeout Override:**
All action methods (Click, Enter, Select, etc.) and Set methods must accept an optional `timeoutMs` parameter:
- When provided, overrides the default timeout for that operation
- When null/omitted, uses the configured default timeout

```csharp
button.Click();                    // Uses default timeout
button.Click(timeoutMs: 5000);     // 5 second timeout

entry.Enter("text");               // Uses default timeout  
entry.Enter("text", timeoutMs: 10000);  // 10 second timeout
```

### ControlCapabilities
- **id**: FR-002.4
- **title**: Supported control types

The framework must at minimum support:
- Text input controls
- Clickable controls (buttons, links)
- Toggle controls (checkboxes, switches)
- Selection controls (dropdowns, lists)
- Range controls (sliders, progress bars)
- Collection controls (lists, grids)

Platform implementations may support any additional control types available in MAUI or Blazor standard libraries.

### UnifiedInterfaceHierarchy
- **id**: FR-002.5
- **title**: Single unified interface hierarchy in Core

The framework must define a unified interface hierarchy for control objects. Example structure:

```
IControlObject (base)
├── IClickableControl
│   └── IContentControl
├── ITextControl
├── IToggleControl
├── ISelectorControl
├── IRangeControl
├── IItemsControl
└── IContainerControl
```

**Note:** The above is an illustrative example. The complete interface hierarchy must be based on MAUI and Blazor standard control libraries and will be formally defined in separate specification documents.

Platform implementations may implement these interfaces as needed for their supported control types.

### ContainerScopedControls
- **id**: FR-002.6
- **title**: Container-scoped element searching

See [120_012_ContainerPattern](120_012_ContainerPattern.spx.md) for detailed container specification.

### ScrollToElement
- **id**: FR-002.7
- **title**: Scroll-to-element support
- **priority**: medium

Scrollable controls (both pages and containers) should support scrolling to make elements visible.

**Page-level scrolling:**
- ScrollToElement(locator) — Scroll page to make element visible
- ScrollToTop() — Scroll to top of page
- ScrollToBottom() — Scroll to bottom of page

**Container-level scrolling:**
- ScrollToElement(locator) — Scroll within container to make element visible
- ScrollToTop() — Scroll to top of container content
- ScrollToBottom() — Scroll to bottom of container content
- ScrollUp() — Scroll up by increment
- ScrollDown() — Scroll down by increment

See [120_012_ContainerPattern](120_012_ContainerPattern.spx.md) for container scrolling details.
