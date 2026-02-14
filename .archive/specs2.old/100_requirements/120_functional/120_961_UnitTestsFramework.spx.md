# functional UnitTestsFramework
- **id**: FR-961
- **title**: Unit Tests for Framework Infrastructure
- **priority**: high
- **status**: draft
- **category**: Testing Infrastructure

Beyond ControlObject unit tests (FR-960), the framework must have comprehensive unit tests for all infrastructure components.

## capabilities

### InfrastructureTestCoverage
- **id**: FR-961.1
- **title**: Infrastructure test coverage

Unit tests must cover all non-ControlObject framework code:

| Component | Test Project | What to Test |
|-----------|--------------|--------------|
| Locators | Brinell.Core.Tests | ControlLocator, By factory, locator chaining |
| Configuration | Brinell.Core.Tests | Settings, timeout defaults, configuration loading |
| Exceptions | Brinell.Core.Tests | Custom exception types, messages, inner exceptions |
| Logging | Brinell.Core.Tests | Log formatting, log levels, output targets |
| Interfaces | Brinell.Core.Tests | Interface contracts, member definitions |

### LocatorSystemTests
- **id**: FR-961.2
- **title**: Locator system tests

Locator tests must verify:

| Component | Tests |
|-----------|-------|
| ControlLocator | Constructor, properties, ToString, equality |
| By Factory | All static factory methods (AutomationId, Id, XPath, Css, etc.) |
| Locator Chaining | Then(), parent chain, ToString with chain |
| Index Selection | First(), Last(), Nth(), WithIndex() |
| Implicit Conversion | String to ControlLocator conversion |

### LocatorConversionTests
- **id**: FR-961.3
- **title**: Locator conversion tests

Each technology package must test locator-to-native conversion:

| Technology | Conversion Tests |
|------------|-----------------|
| MAUI/Appium | ControlLocator → Appium By (AccessibilityId, XPath, etc.) |
| Blazor/Playwright | ControlLocator → Playwright locator string |
| WPF/FlaUI | ControlLocator → FlaUI conditions |

### ExceptionTests
- **id**: FR-961.4
- **title**: Exception type tests

Exception tests must verify:

| Exception | Tests |
|-----------|-------|
| ElementNotFoundException | Message format, locator info included |
| UITestTimeoutException | Timeout value in message, operation name |
| AssertionException | Expected vs actual in message |
| ConfigurationException | Setting name, invalid value info |

### ConfigurationTests
- **id**: FR-961.5
- **title**: Configuration tests

Configuration tests must verify:

| Aspect | Tests |
|--------|-------|
| Defaults | Default timeout values, polling intervals |
| Override | Per-method timeout override works |
| Loading | Configuration from file/environment |
| Validation | Invalid configuration rejected |

### TestContextTests
- **id**: FR-961.6
- **title**: Test context tests

TestContext tests must verify:

| Aspect | Tests |
|--------|-------|
| Initialization | Context created with driver |
| Properties | DefaultTimeout, PollingInterval get/set |
| Navigation | NavigateTo creates page, sets CurrentPage |
| Control Factory | CreateControl returns correct type |
| Screenshot | TakeScreenshot calls driver |
| Logging | Log, LogError write to output |

### PageObjectBaseTests
- **id**: FR-961.7
- **title**: Page object base tests

PageObjectBase tests must verify:

| Aspect | Tests |
|--------|-------|
| Constructor | Context required, locator optional |
| IsLoaded | Uses page locator for visibility |
| WaitLoaded | Waits for page locator |
| GetControl | Returns control with correct scope |
| Screenshot | Delegates to context |

### InterfaceContractTests
- **id**: FR-961.8
- **title**: Interface contract tests

Interface tests must verify each interface defines expected members:

| Interface | Verify Members |
|-----------|---------------|
| IControlObject | Locator, Page, IsExists, WaitExists, AssertExists, etc. |
| IClickableControl | Click, DoubleClick, RightClick, Hover |
| ITextControl | GetText, AssertText, AssertTextContains |
| IEditableTextControl | Enter, Clear, ClearAndEnter, Append |
| IToggleControl | IsChecked, Toggle, SetChecked |
| ISelectorControl | GetSelectedItem, Select, GetItems |
| IRangeControl | GetValue, SetValue, GetMinimum, GetMaximum |
| IContainerControl | GetControl, ControlExists |

### PureUnitTests
- **id**: FR-961.9
- **title**: Pure unit test requirements

Infrastructure tests must be pure unit tests:
1. **No mocks needed for Core** — Test actual implementation
2. **No external dependencies** — No files, network, database
3. **Fast execution** — < 100ms per test
4. **Deterministic** — Same result every run

### TestOrganization
- **id**: FR-961.10
- **title**: Infrastructure test organization

```
Brinell.Core.Tests/
├── Locators/
│   ├── ControlLocatorTests.cs
│   ├── ByFactoryTests.cs
│   └── LocatorChainingTests.cs
├── Interfaces/
│   ├── IControlObjectContractTests.cs
│   ├── IClickableControlContractTests.cs
│   └── ...
├── Exceptions/
│   ├── ElementNotFoundExceptionTests.cs
│   └── UITestTimeoutExceptionTests.cs
├── Configuration/
│   └── ConfigurationTests.cs
└── GlobalUsings.cs

Brinell.{Tech}.Tests/
├── Context/
│   └── {Tech}TestContextTests.cs
├── Locators/
│   └── LocatorConversionTests.cs
├── Pages/
│   └── PageObjectBaseTests.cs
└── ...
```

---

## relationships

- Complements [FR-960 ControlObject Unit Tests](120_960_UnitTests.spx.md)
- Tests interfaces from [FR-103 Interface Hierarchy](120_103_InterfaceHierarchy.spx.md)
- Tests locators from [FR-200 Element Location](120_200_ElementLocation.spx.md)
- Tests exceptions from [FR-600 Exception Strategy](120_600_ExceptionStrategy.spx.md)

---

## constraints

- Core tests must not require mocks (pure unit tests)
- Technology tests may mock driver/element for isolation
- Tests must run without sample apps
- Tests must not have timing dependencies

