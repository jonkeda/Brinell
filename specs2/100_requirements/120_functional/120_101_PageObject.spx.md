# functional PageObject
- **id**: FR-101
- **title**: Page Object Model
- **priority**: high
- **status**: draft
- **category**: Object Model

The framework must provide a Page Object abstraction for organizing test code. Page objects represent views/screens in the application and provide access to controls on that page.

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

Pages must provide state verification:

| Method | Description |
|--------|-------------|
| IsDisplayed | Check if page is currently displayed |
| IsReady | Check if page is ready for interaction |
| WaitForDisplayed | Wait until page is displayed |
| WaitForReady | Wait until page is ready |

**Ready state:**
- Page-specific readiness criteria
- May include: loading complete, required elements present, no busy indicators
- Customizable per page type

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

Pages must provide control access:
- Define controls as properties or methods
- Controls created with locator relative to page
- Controls inherit page's default settings (timeout)

Control definitions should be declarative, not imperative.

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

Pages may track busy/loading state:

| Method | Description |
|--------|-------------|
| IsBusy | Check if page shows busy indicator |
| WaitForNotBusy | Wait until busy state clears |

Busy indicator definition is page-specific (spinner, overlay, progress bar).

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
