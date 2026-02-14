# functional UnitTests

- **id**: FR-960
- **title**: Unit Tests with Mocks
- **priority**: high
- **status**: draft
- **category**: Testing Infrastructure

The framework must have comprehensive unit tests using mocks to verify ControlObject behavior without requiring actual UI automation.

## capabilities

### UnitTestCoverage

- **id**: FR-960.1
- **title**: Unit test coverage requirements

Each technology package must have unit tests:

| Package        | Test Project         | Location                                   |
| -------------- | -------------------- | ------------------------------------------ |
| Brinell.Core   | Brinell.Core.Tests   | tests/Brinell.Core.Tests.ControlObject6/   |
| Brinell.Maui   | Brinell.Maui.Tests   | tests/Brinell.Maui.Tests.ControlObject6/   |
| Brinell.Blazor | Brinell.Blazor.Tests | tests/Brinell.Blazor.Tests.ControlObject6/ |
| Brinell.Wpf    | Brinell.Wpf.Tests    | tests/Brinell.Wpf.Tests/                   |

### MockingStrategy

- **id**: FR-960.2
- **title**: Mocking strategy

Unit tests must use mocks to isolate behavior:

| Component | Mock Target                             |
| --------- | --------------------------------------- |
| MAUI      | AppiumDriver, AppiumElement, Actions    |
| Blazor    | IPage, ILocator (Playwright interfaces) |
| WPF       | AutomationElement (FlaUI)               |
| Core      | No mocks needed (pure unit tests)       |

### TestCategories

- **id**: FR-960.3
- **title**: Test categories

Tests must cover these categories:

| Category          | What to Test                                 |
| ----------------- | -------------------------------------------- |
| Constructor       | Null handling, property initialization       |
| State Methods     | IsExists, IsVisible, IsEnabled               |
| Wait Methods      | WaitExists, WaitVisible, WaitEnabled         |
| Check Methods     | CheckExists, CheckVisible (throw on timeout) |
| Assert Methods    | AssertExists, AssertText (throw on failure)  |
| Action Methods    | Click, Enter, Clear, Select                  |
| Nullable Handling | Null parameters return early                 |

### CoreUnitTests

- **id**: FR-960.4
- **title**: Core package unit tests

Core tests verify:

1. **Locator System** — ControlLocator, By factory, locator chaining
2. **Interface Contracts** — All interfaces define expected members
3. **Exception Types** — Custom exceptions are defined correctly
4. **Enum Values** — LocatorStrategy values are correct

No mocks required — pure unit tests.

### TechnologyUnitTests

- **id**: FR-960.5
- **title**: Technology package unit tests

Technology package tests verify with mocks:

**ControlObjectBase tests:**

- Constructor validation
- Existence/visibility/enabled state queries
- Wait methods with timeout behavior
- Assert methods with failure messages
- Locator conversion to native format

**Concrete control tests:**

- Click operations call correct driver methods
- Text entry clears and types correctly
- Toggle operations change state
- Selection operations work correctly

**Context tests:**

- Navigation calls driver navigation
- Control creation returns correct types
- Screenshot capture works

### NullableParameterTests

- **id**: FR-960.6
- **title**: Nullable parameter handling tests

Every method with nullable parameters must have tests:

| Test Case        | Expected Behavior                 |
| ---------------- | --------------------------------- |
| Enter(null)      | Returns immediately, no action    |
| AssertText(null) | Returns immediately, no assertion |
| WaitExists(null) | Returns true immediately          |
| Select(null)     | Returns immediately, no selection |

### TimeoutTests

- **id**: FR-960.7
- **title**: Timeout behavior tests

Tests must verify timeout handling:

| Test Case              | Expected Behavior             |
| ---------------------- | ----------------------------- |
| WaitExists times out   | Returns false                 |
| CheckExists times out  | Throws UITestTimeoutException |
| AssertExists times out | Throws AssertionException     |
| Custom timeout used    | Override default timeout      |

### MockSetup

- **id**: FR-960.8
- **title**: Mock setup requirements

Mock setup must:

1. **Be explicit** — Clear what is being mocked
2. **Be minimal** — Only mock what's needed
3. **Be reusable** — Create fixtures/helpers for common mocks
4. **Support verification** — Allow verifying mock calls

Example pattern:

```csharp
// Arrange
var mockDriver = new Mock<IAppiumDriver>();
var mockElement = new Mock<IAppiumElement>();
mockDriver.Setup(d => d.FindElement(It.IsAny<By>()))
    .Returns(mockElement.Object);

// Act
var button = new ButtonControl(context, By.AutomationId("submit"));
button.Click();

// Assert
mockElement.Verify(e => e.Click(), Times.Once);
```

### TestNaming

- **id**: FR-960.9
- **title**: Test naming convention

Tests must follow naming convention:

```
{MethodName}_{Scenario}_{ExpectedResult}
```

Examples:

```
Click_WhenVisible_CallsElementClick
Enter_WithNull_DoesNothing
WaitExists_TimesOut_ReturnsFalse
AssertText_Mismatch_ThrowsAssertionException
```

### TestOrganization

- **id**: FR-960.10
- **title**: Test organization

Test projects should be organized:

```
Brinell.{Tech}.Tests/
├── Context/
│   └── {Tech}TestContextTests.cs
├── Controls/
│   ├── ButtonControlTests.cs
│   ├── EntryControlTests.cs
│   └── ...
├── Mocks/
│   ├── Mock{Driver}Factory.cs
│   └── Mock{Element}Builder.cs
├── Fixtures/
│   └── ControlTestFixture.cs
└── GlobalUsings.cs
```

### ControlObjectTests

- **id**: FR-960.11
- **title**: Each ControlObject must have unit tests

Every ControlObject implementation must have corresponding unit tests:

| ControlObject   | Test File               |
| --------------- | ----------------------- |
| ButtonControl   | ButtonControlTests.cs   |
| EntryControl    | EntryControlTests.cs    |
| LabelControl    | LabelControlTests.cs    |
| CheckBoxControl | CheckBoxControlTests.cs |
| DropdownControl | DropdownControlTests.cs |
| ...             | ...                     |

Minimum test coverage per control:

- Constructor tests
- All state query methods (Is* methods)
- All wait methods (Wait* methods)
- All assert methods (Assert* methods)
- All action methods specific to control type

---

## relationships

- Tests target implementations of [FR-100 ControlObject](120_100_ControlObject.spx.md)
- Tests follow patterns in [FR-103 Interface Hierarchy](120_103_InterfaceHierarchy.spx.md)
- Unit tests complement [FR-970 UI Tests](120_970_UITests.spx.md)

---

## constraints

- Unit tests must not require running sample apps
- Unit tests must run fast (< 1 second per test)
- Unit tests must be deterministic (no flaky tests)
- Unit tests must not have external dependencies
