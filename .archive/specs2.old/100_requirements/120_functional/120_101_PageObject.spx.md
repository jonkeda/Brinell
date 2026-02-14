# functional PageObject
- **id**: FR-101
- **title**: Page Object Model
- **priority**: high
- **status**: draft
- **category**: Object Model

The framework Core must define page object interfaces for organizing test code. Page objects represent views/screens in the application and provide access to controls on that page.

**Architecture:**
- **Core defines IPageObject interface** with contracts for page behavior
- **Technology packages provide base classes** that implement IPageObject
- Tests inherit from technology-specific page base classes

## capabilities

### PageDefinition
- **id**: FR-101.1
- **title**: Page object definition

A page object represents a view/screen and provides:
- Access to controls on the page
- Page state verification (displayed, ready)
- Navigation to other pages
- Page-level operations (scroll, refresh)

Pages search for elements from the application/document root.

### PageStateVerification
- **id**: FR-101.2
- **title**: Page state verification

Pages must provide state verification using nullable skip pattern:

| Method | Description |
|--------|-------------|
| IsLoaded | Check if page is currently loaded (immediate) |
| WaitLoaded(expected?) | Wait for loaded/unloaded state; null skips |
| AssertLoaded(expected?) | Assert loaded state; null skips |
| GetTitle | Get page title |
| AssertTitle(expected?) | Assert page title; null skips |

**Nullable skip pattern:**
- When `expected` parameter is null, method returns immediately
- Enables conditional verification without explicit null checks

**Ready state:**
- Page-specific readiness criteria
- May include: loading complete, required elements present, no busy indicators
- Customizable per page type via PageLocator override

### AutomaticReadinessCheck
- **id**: FR-101.3
- **title**: Automatic readiness on construction

When a page object is created:
1. Framework waits for page to be ready (by default)
2. Readiness check can be disabled via parameter
3. Timeout applies to readiness wait

This ensures page is interactive before tests proceed.

### ControlAccess
- **id**: FR-101.4
- **title**: Control access from page

Pages provide control access via direct construction:
- Define controls as properties using `new` pattern
- Pass page reference (`this`) to enable scoping and logging
- Controls inherit page's context settings

```
// Pseudocode - page defines controls via new
public EntryControl UsernameField => new(Context, "UsernameEntry", this);
public ButtonControl LoginButton => new(Context, "LoginButton", this);
```

Control definitions should be declarative properties, not factory methods.

### NavigationMethods
- **id**: FR-101.5
- **title**: Navigation method pattern

Navigation methods on pages must:
- Perform the navigation action only
- NOT create or return target page objects
- NOT verify navigation succeeded

Tests explicitly create target page objects after navigation:
```
// Pseudocode
loginPage.ClickLogin()           // Performs click only
homePage = new HomePage(context) // Test creates target page
```

Rationale: Tests control page object lifecycle; navigation destination may vary.

### PageLevelOperations
- **id**: FR-101.6
- **title**: Page-level operations

Pages must support page-level operations:

| Operation | Description |
|-----------|-------------|
| ScrollToTop | Scroll page to top |
| ScrollToBottom | Scroll page to bottom |
| ScrollToElement | Scroll to make element visible |
| Refresh | Refresh page content (where applicable) |

### BusyStateTracking
- **id**: FR-101.7
- **title**: Page busy state tracking

Pages that track busy/loading state should implement `IBusyPageObject`:

| Method | Description |
|--------|-------------|
| IsBusy | Check if page shows busy indicator |
| WaitForNotBusy | Wait until busy state clears |

Busy indicator definition is page-specific (spinner, overlay, progress bar).

This capability is provided as an optional interface because:
- Not all pages have busy indicators
- Busy indicator implementations vary by application
- Tests can opt-in by casting or interface check

### DefaultLocatorStrategy
- **id**: FR-101.8
- **title**: Page-level default locator strategy

Pages may define a default locator strategy:
- Controls on page use this strategy unless overridden
- Simplifies control definitions when consistent strategy used
- Strategy can be overridden per control

---

## relationships

- Pages create [FR-100 Controls](120_100_ControlObject.spx.md) within their scope
- Pages may contain [FR-102 Containers](120_102_ContainerObject.spx.md)
- Page state methods follow [FR-300 State Verification](120_300_StateVerification.spx.md) patterns
- Pages created within [FR-400 Test Context](120_400_TestContext.spx.md)

---

## constraints

- Page objects must not hold references to other page objects
- Page objects must not navigate and return different page type
- Page construction must not have side effects beyond readiness waiting
