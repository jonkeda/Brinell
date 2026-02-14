# functional ContainerObject
- **id**: FR-102
- **title**: Container Object Model
- **priority**: high
- **status**: draft
- **category**: Object Model

The framework must provide a Container abstraction for scoped element searching. Containers represent UI regions that group related controls.

## capabilities

### ContainerDefinition
- **id**: FR-102.1
- **title**: Container object definition

A container object represents a UI region and provides:
- Scoped element searching (descendants only)
- Access to child controls
- Access to nested containers
- Container-level operations (scroll within container)

Containers have an underlying element in the UI tree.

### ScopedSearch
- **id**: FR-102.2
- **title**: Scoped element searching

Containers search differently than pages:

| Object Type | Search Scope |
|-------------|--------------|
| Page | Application/document root |
| Container | Container element's descendants only |

Controls created from a container are scoped to that container:
```
// Pseudocode
form = page.GetContainer("loginForm")
username = form.GetControl("username")  // Searches within form only
```

### ContainerElement
- **id**: FR-102.3
- **title**: Container underlying element

Containers must have an underlying automation element:
- Located when container is accessed
- Must exist in the UI tree
- Container operations apply to this element

### ContainerInstantiation
- **id**: FR-102.4
- **title**: Container instantiation patterns

Containers are created from pages or other containers:

| Pattern | Description |
|---------|-------------|
| GetContainer by locator | Find container by locator |
| GetContainer by type | Find container with type-specific logic |
| Property access | Container defined as page property |

### NestedContainers
- **id**: FR-102.5
- **title**: Nested container support

Containers may contain other containers:
- Unlimited nesting depth
- Each level scopes to its parent
- Search cascades through hierarchy

```
// Pseudocode
page
  └── mainPanel (container)
        └── sidebar (container)
              └── menuItem (control)
```

### ParentAccess
- **id**: FR-102.6
- **title**: Container parent access

Containers must provide access to their parent:
- Parent is page or containing container
- Enables navigation up the hierarchy
- Root page has no parent (null)

### ContainerScrolling
- **id**: FR-102.7
- **title**: Container scroll support

Scrollable containers must support:

| Operation | Description |
|-----------|-------------|
| ScrollToElement | Scroll within container to show element |
| ScrollToTop | Scroll to top of container content |
| ScrollToBottom | Scroll to bottom of container content |
| ScrollUp | Scroll up by increment |
| ScrollDown | Scroll down by increment |
| IsScrollable | Check if container supports scrolling |

### ContainerVsControl
- **id**: FR-102.8
- **title**: When to use container vs control

Decision criteria:

| Use Container When | Use Control When |
|--------------------|------------------|
| Region has multiple child controls | Element is a leaf/action target |
| Need to scope searches | No child elements needed |
| Logical grouping in UI | Single interaction point |
| Region can scroll independently | Part of parent scroll region |

Examples:
- Form panel → Container (has input fields)
- Submit button → Control (leaf element)
- List item → Container if has child elements, Control if just text
- Card → Container (typically has multiple elements)

---

## relationships

- Containers are created from [FR-101 Pages](120_101_PageObject.spx.md) or other containers
- Containers provide [FR-100 Controls](120_100_ControlObject.spx.md)
- Container locators use [FR-200 Element Location](120_200_ElementLocation.spx.md)

---

## constraints

- Container element must exist when container operations are performed
- Containers must not be created without a locator or parent scope
- Container scope must be respected by all descendant controls
