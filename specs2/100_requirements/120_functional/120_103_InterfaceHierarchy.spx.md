# functional InterfaceHierarchy
- **id**: FR-103
- **title**: Interface Hierarchy
- **priority**: high
- **status**: draft
- **category**: Object Model

The framework Core must define a unified interface hierarchy for control objects. Interfaces are defined in Core and implemented by technology-specific packages.

**Critical Design Principle:**
- **Core defines interfaces ONLY** - no concrete implementations in Core
- **Technology packages provide implementations** - concrete classes that implement Core interfaces
- Tests depend on Core interfaces; runtime uses technology implementations

## capabilities

### CoreInterfacesOnly
- **id**: FR-103.1
- **title**: Core defines interfaces and cross-cutting concerns

The framework Core package must:
- Define all control and page interfaces
- Provide cross-cutting concerns (logging, timeout, retry, assertions) using only .NET types
- Provide **NO** concrete control or page implementations
- Contain **NO** technology-specific code (no Appium, Selenium, Playwright references)
- Be technology-agnostic (no platform-specific types)
- Serve as contracts for all technology implementations

Technology packages (Brinell.Maui, Brinell.Web, Brinell.Wpf, etc.) provide concrete control implementations.

### InterfaceStructure
- **id**: FR-103.2
- **title**: Interface hierarchy structure

The interface hierarchy must:

1. **Cover all standard controls** in both MAUI and Blazor platforms
2. **Follow capability-based design** — interfaces represent capabilities (clickable, editable, selectable)
3. **Use composition over deep inheritance** — controls implement multiple capability interfaces
4. **Have a single base interface** — IControlObject for common state/assertion methods

**Design Pattern:**
```
IControlObject (base for ALL controls)
├── Capability interfaces (IClickable, IText, IToggle, etc.)
│   └── Extended capabilities (IEditableText extends IText)
└── Structural interfaces (IContainer, IItems, IScrollable)
```

**Scope:** The complete interface list is defined in specifications, derived from:
- All standard MAUI controls (Button, Entry, Label, CheckBox, Picker, Slider, etc.)
- All standard Blazor/HTML controls (button, input, select, checkbox, etc.)
- Common capabilities across both platforms

**Note:** The specifications define the complete interface catalog. This requirement defines the pattern.

### BaseInterface
- **id**: FR-103.3
- **title**: Base control interface

All controls must implement a base interface providing:

| Member | Type | Description |
|--------|------|-------------|
| Locator | Property | Element locator |
| Page | Property | Containing page reference |
| IsExists | Method | Check existence |
| IsVisible | Method | Check visibility |
| IsEnabled | Method | Check enabled state |
| WaitExists | Method | Wait for existence |
| WaitVisible | Method | Wait for visibility |
| WaitEnabled | Method | Wait for enabled |
| AssertExists | Method | Assert existence |
| AssertVisible | Method | Assert visibility |
| GetText | Method | Get text content |
| GetAttribute | Method | Get attribute value |

### CapabilityInterfaces
- **id**: FR-103.4
- **title**: Capability-based interfaces

Interfaces define capabilities that controls may have. Each capability interface:

1. **Extends IControlObject** — inherits base state/assertion methods
2. **Defines capability-specific methods** — actions and assertions for that capability
3. **Is independently implementable** — controls choose which capabilities they support

**Capability Categories:**

| Category | Purpose | Examples |
|----------|---------|----------|
| Interaction | User actions | Click, tap, swipe, scroll |
| Text | Text content | Read text, enter text, clear |
| State | Binary/multi state | Toggle, select, expand |
| Range | Numeric values | Slider value, progress |
| Structure | Element organization | Container scope, item collection |
| Navigation | View/page control | Navigate, tab, flyout |
| Media | Rich content | Image, video, web content |

**Note:** The complete interface catalog with all methods is defined in specifications. This requirement defines the capability pattern.

### MultipleInterfaces
- **id**: FR-103.5
- **title**: Multiple interface implementation

A control may implement multiple interfaces:
- Button: IClickableControl
- Entry: IEditableTextControl, IClickableControl
- Checkbox: IToggleControl, IClickableControl
- ComboBox: ISelectorControl, IEditableTextControl

Interface combination reflects actual control capabilities.

### TechnologyClassHierarchy
- **id**: FR-103.6
- **title**: Per-technology class hierarchy implements Core interfaces

Each technology package defines its own class hierarchy:
- Classes implement Core interfaces (IControlObject, IClickableControl, etc.)
- Class hierarchy optimized for that platform's automation needs
- Base classes provide common functionality within that technology
- Concrete classes for specific control types

**Package Structure:**
```
Core Package (Brinell.Core):
  Interfaces ONLY:
    IControlObject
    IClickableControl
    ITextControl
    IEditableTextControl
    IPageObject
    ...

Technology Package A (Brinell.Maui):
  ControlBase (implements IControlObject)
  ├── ClickableControlBase (implements IClickableControl)
  │   ├── ButtonControl
  │   └── ImageButtonControl
  └── TextControlBase (implements ITextControl)
      └── EntryControl (implements IEditableTextControl)

Technology Package B (Brinell.Web):
  WebControlBase (implements IControlObject)
  ├── WebButtonControl (implements IClickableControl)
  └── WebInputControl (implements IEditableTextControl)
```

**NO implementations in Core** - Core is purely interface definitions.

### CodeReuse
- **id**: FR-103.7
- **title**: Technology class code reuse

Technology implementations should maximize code reuse:
- Base classes encapsulate common behavior
- Composition for cross-cutting concerns
- Utility classes for shared operations
- Only platform-specific code in concrete classes

### PlatformContextInterfaces
- **id**: FR-103.8
- **title**: Platform-specific context interfaces

Each technology package defines a platform-specific context interface:
- Extends `ITestContext` from Core
- Adds platform-specific element finding methods
- Exposes driver for advanced scenarios
- Controls use the platform context interface type

**Pattern:**
```
Core (Brinell.Core):
  ITestContext (base interface)

MAUI (Brinell.Maui):
  IMauiTestContext : ITestContext
    - AppiumDriver Driver
    - FindElement(Locator)
    - TryFindElement(Locator)
    - FindElements(Locator)

Blazor (Brinell.Blazor):
  IBlazorTestContext : ITestContext
    - IWebDriver Driver
    - string BaseUrl
    - FindElement(Locator)
    - TryFindElement(Locator)
    - FindElements(Locator)
```

This enables:
- Interface-based programming in controls
- Mocking for unit tests
- Consistent element finding API per platform

---

## relationships

- Interfaces are implemented by [FR-100 Controls](120_100_ControlObject.spx.md)
- Method signatures follow [FR-300 State Verification](120_300_StateVerification.spx.md) patterns
- Interfaces used across [FR-010 Platforms](120_010_PlatformSupport.spx.md)

---

## constraints

- Interfaces must not expose platform-specific types
- Interface methods must use framework types only
- Breaking interface changes require major version increment
