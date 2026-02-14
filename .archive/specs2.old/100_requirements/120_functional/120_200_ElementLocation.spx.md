# functional ElementLocation
- **id**: FR-200
- **title**: Element Location Strategies
- **priority**: high
- **status**: draft
- **category**: Element Location

The framework must provide flexible element location strategies for finding UI elements across all platforms.

## capabilities

### LocatorSystem
- **id**: FR-200.1
- **title**: Unified locator system

The framework must provide a unified locator system:
- Single locator type used across all platforms
- Platform implementations translate to native locators
- Locators are immutable value objects

### LocatorStrategies
- **id**: FR-200.2
- **title**: Supported locator strategies

The framework must support these locator strategies:

| Strategy | Description | Platform Support |
|----------|-------------|------------------|
| AutomationId | XAML AutomationId or data-automation-id | All |
| Id | HTML id or AccessibilityId | All |
| Name | Name attribute | All |
| ClassName | Class name | All |
| XPath | XPath expression | All |
| Css | CSS selector | Web only |
| TestId | data-testid attribute | Web only |
| Text | Exact text content | All |
| PartialText | Partial text match | All |
| TagName | HTML tag or control type | All |
| AccessibilityLabel | Accessibility label/name | Mobile, Desktop |

### FluentLocatorAPI
- **id**: FR-200.3
- **title**: Fluent locator construction

The framework must provide a fluent API for creating locators:
```
// Pseudocode examples
By.AutomationId("submitButton")
By.Css(".btn-primary")
By.XPath("//button[@type='submit']")
By.Text("Submit")
By.Id("email").WithTimeout(5000)
```

### ChainedLocators
- **id**: FR-200.4
- **title**: Locator chaining for hierarchy

Locators must support chaining for hierarchical element finding:
```
// Pseudocode - find input within form
By.AutomationId("loginForm").Then(By.Name("username"))

// Multiple levels
By.Id("panel").Then(By.ClassName("card")).Then(By.TagName("input"))
```

Chained locators search within the element found by the previous locator.

### ImplicitConversion
- **id**: FR-200.5
- **title**: String to locator conversion

String values should implicitly convert to locators:
- Default strategy: AutomationId
- Enables concise control definitions
- Can be configured per page

```
// Pseudocode - these are equivalent
GetControl("submitButton")
GetControl(By.AutomationId("submitButton"))
```

### LocatorOptions
- **id**: FR-200.6
- **title**: Locator options

Locators may include additional options:

| Option | Description |
|--------|-------------|
| Timeout | Override default element search timeout |
| Multiple | Expect multiple matches |
| Index | Select nth match when multiple exist |
| Visible | Only match visible elements |

### RelativeLocators
- **id**: FR-200.7
- **title**: Relative locators (optional)

The framework may support relative locators where platform allows:
- Above/below another element
- Left/right of another element
- Near another element

Note: Relative locator support varies by platform and automation tool.

### LocatorDiagnostics
- **id**: FR-200.8
- **title**: Locator diagnostics

Locators must support diagnostics:
- ToString representation for logging
- Strategy and value accessible for error messages
- Full chain visible in hierarchical locators

---

## relationships

- Locators used by [FR-100 Controls](120_100_ControlObject.spx.md)
- Locators used by [FR-102 Containers](120_102_ContainerObject.spx.md)
- Error messages include locator per [FR-600 Exceptions](120_600_ExceptionStrategy.spx.md)

---

## constraints

- Locators must be serializable for logging
- Locators must be comparable for equality
- Platform implementations must validate strategy is supported
- Invalid strategy for platform must fail fast with clear error
