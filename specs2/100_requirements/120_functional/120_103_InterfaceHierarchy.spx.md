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
- **title**: Core defines interfaces only

The framework Core package must:
- Define all control and page interfaces
- Provide **NO** concrete control or page implementations
- Be technology-agnostic (no platform-specific types)
- Serve as contracts for all technology implementations

Technology packages (Brinell.Maui, Brinell.Web, Brinell.Wpf, etc.) provide concrete implementations.

### InterfaceStructure
- **id**: FR-103.2
- **title**: Interface hierarchy structure

The interface hierarchy must follow this pattern:

```
IControlObject (base for all controls)
├── IClickableControl (click actions)
│   └── IContentControl (clickable with content)
├── ITextControl (text display)
│   └── IEditableTextControl (text input)
├── IToggleControl (binary state)
├── ISelectorControl (selection from options)
├── IRangeControl (numeric range)
├── IItemsControl (collection of items)
└── IContainerControl (scoped region)
```

This is the core hierarchy. Additional interfaces may be defined for platform-specific capabilities.

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

Interfaces define capabilities that controls may have:

**IClickableControl:**
- Click, DoubleClick, RightClick
- WaitClickable, AssertClickable

**ITextControl:**
- GetText, GetValue
- AssertText, AssertValue

**IEditableTextControl (extends ITextControl):**
- Enter, Clear, SetText
- AssertEditable

**IToggleControl:**
- GetChecked, SetChecked, Toggle
- AssertChecked, AssertUnchecked

**ISelectorControl:**
- GetSelectedItem, GetSelectedIndex
- Select, SelectByIndex, SelectByText
- GetItems, GetItemCount
- AssertSelected

**IRangeControl:**
- GetValue, SetValue, GetMinimum, GetMaximum
- Increment, Decrement
- AssertValue, AssertInRange

**IItemsControl:**
- GetItems, GetItemCount
- GetItemAt, FindItem
- AssertItemCount, AssertContainsItem

**IContainerControl:**
- GetControl, GetContainer
- FindControl, FindControls

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
