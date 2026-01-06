# functional ContainerPattern
- **id**: FR-012
- **title**: Container control pattern for scoped element searching
- **priority**: high
- **status**: approved
- **tags**: core, containers, scoping

The framework must support container controls that scope element searches to their descendants.

## capabilities

### PageVsContainer
- **id**: FR-012.1
- **title**: IPageObject as page or container

The `IPageObject` parameter in control constructors can represent either:
- **Page** — A full page/view that is the root of the visual tree
- **Container** — A control that contains other controls (e.g., panels, groups, cards)

Both pages and containers implement `IPageObject` but have different search behaviors.

### SearchBehavior
- **id**: FR-012.2
- **title**: Different search behavior for pages vs containers

**Page search:**
- Search starts from application root or document root
- Can find any element in the visual tree
- Uses platform-specific root element access

**Container search:**
- Search is scoped to descendants of the container element
- Cannot find elements outside the container
- More efficient for localized searches

### ContainerElement
- **id**: FR-012.3
- **title**: Container must have underlying element

A container must have an underlying automation element that:
- Has been located in the visual tree
- Serves as the root for descendant searches
- Can be used to determine container bounds

### ParentAccess
- **id**: FR-012.4
- **title**: Container parent access

A container must be able to access its parent:
- `Parent` property returns the parent `IPageObject`
- For top-level containers, parent is the page
- For nested containers, parent is the enclosing container
- Pages have no parent (returns null)

### ContainerCreation
- **id**: FR-012.5
- **title**: Container instantiation patterns

Containers can be created:
- From a page: `page.GetContainer<CardContainer>(By.AutomationId("userCard"))`
- From another container: `container.GetContainer<ListItem>(By.XPath(".//li[1]"))`
- As control properties: `public CardContainer UserCard => GetContainer<CardContainer>("userCard")`

### ScopedControlCreation
- **id**: FR-012.6
- **title**: Controls created within container scope

Controls created from a container are automatically scoped:
```csharp
var card = page.GetContainer<CardContainer>("userCard");
var nameLabel = card.GetControl<LabelControl>("userName");  // Searches only within card
var editButton = card.GetControl<ButtonControl>("edit");    // Searches only within card
```

### ContainerScrolling
- **id**: FR-012.7
- **title**: Container scroll support

Containers that support scrolling must implement:
- `ScrollToElement(ControlLocator locator)` — Scroll to make element visible within container
- `ScrollToTop()` — Scroll to top of container content
- `ScrollToBottom()` — Scroll to bottom of container content
- `ScrollUp(int? pixels)` — Scroll up by specified amount or default increment
- `ScrollDown(int? pixels)` — Scroll down by specified amount or default increment
- `IsScrollable` — Property indicating if container supports scrolling

### NestedContainers
- **id**: FR-012.8
- **title**: Nested container support

Containers can be nested to any depth:
```csharp
var page = new DashboardPage(context);
var sidebar = page.GetContainer<SidebarContainer>("sidebar");
var menuSection = sidebar.GetContainer<MenuSection>("settings");
var menuItem = menuSection.GetControl<ButtonControl>("preferences");
```

Each level scopes the search to its parent container.

### ContainerVsControl
- **id**: FR-012.9
- **title**: When to use container vs control

**Use container when:**
- The element contains multiple child controls
- You need scoped searches within that element
- The element represents a logical grouping (card, panel, section, list item)

**Use control when:**
- The element is a leaf control (button, label, input)
- You only need to interact with the element itself
- No child control access is needed
