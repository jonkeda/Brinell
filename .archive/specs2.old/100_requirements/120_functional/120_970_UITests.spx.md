# functional UITests
- **id**: FR-970
- **title**: UI Integration Tests
- **priority**: high
- **status**: draft
- **category**: Testing Infrastructure

The framework must have UI integration tests that verify ControlObject behavior against real sample applications.

## capabilities

### UITestCoverage
- **id**: FR-970.1
- **title**: UI test coverage requirements

Each technology package must have UI tests:

| Package | Test Project | Target App |
|---------|--------------|------------|
| Brinell.Maui | Brinell.Samples.Maui.UITests | Brinell.Samples.Maui.App |
| Brinell.Blazor | Brinell.Samples.Blazor.UITests | Brinell.Samples.Blazor.App |
| Brinell.Wpf | Brinell.Samples.Wpf.UITests | Brinell.Samples.Wpf.App |

### UITestPurpose
- **id**: FR-970.2
- **title**: UI test purpose

UI tests verify:
1. **Real control interaction** — Actual clicks, typing, selections work
2. **Element location** — Locators find correct elements
3. **State verification** — Visibility, enabled state correctly detected
4. **Platform behavior** — Platform-specific quirks are handled

### TestSampleAppRelationship
- **id**: FR-970.3
- **title**: UI tests use sample apps

UI tests:
1. **Target sample apps** — Not production applications
2. **Use known controls** — Controls with documented AutomationIds
3. **Expect known states** — Controls in predictable initial state
4. **Follow sample app structure** — Match page organization

### ControlObjectUITests
- **id**: FR-970.4
- **title**: Each ControlObject must have UI tests

Every ControlObject implementation must have corresponding UI tests:

| Test Category | What to Verify |
|---------------|----------------|
| Location | Control is found by AutomationId |
| State | IsExists, IsVisible, IsEnabled return correct values |
| Action | Click, Enter, Select perform correctly |
| Assertion | AssertText, AssertExists work correctly |

### SampleAppControlRequirement
- **id**: FR-970.5
- **title**: Sample app must contain all ControlObjects

For every ControlObject that exists:
1. Sample app must have at least one corresponding control
2. Control must have unique AutomationId
3. Control must be in testable state

This ensures:
- Every ControlObject can be UI tested
- No ControlObject exists without sample app coverage

### UITestPageObjects
- **id**: FR-970.6
- **title**: UI tests must use PageObjects

UI tests must:
1. Define PageObjects for sample app pages
2. Access controls through PageObject properties
3. Not use raw locators in test methods
4. Follow Page Object pattern defined in [FR-101](120_101_PageObject.spx.md)

Example:
```csharp
// Good - using PageObject
var loginPage = await context.NavigateTo<LoginPage>();
await loginPage.UsernameField.Enter("test@example.com");
await loginPage.SubmitButton.Click();

// Bad - raw locators in test
var field = context.CreateControl<ITextControl>(By.AutomationId("Entry_Username"));
await field.Enter("test@example.com");
```

### UITestIsolation
- **id**: FR-970.7
- **title**: UI test isolation

Each UI test must:
1. **Start from known state** — Navigate to specific page
2. **Not depend on other tests** — Run independently
3. **Clean up after** — Reset state if modified
4. **Handle app restart** — Work if app is restarted between tests

### ControlTestMatrix
- **id**: FR-970.8
- **title**: Control test matrix

Each control type must be tested for:

| Aspect | Tests |
|--------|-------|
| Basic | Find control, verify exists |
| State | Check visible, enabled states |
| Primary Action | Perform main action (click/enter/select) |
| Assertions | Verify text, state assertions work |
| Error Cases | Element not found behavior |

### PlatformSpecificTests
- **id**: FR-970.9
- **title**: Platform-specific UI tests

Some controls require platform-specific tests:

| Platform | Specific Tests |
|----------|----------------|
| MAUI Android | Touch gestures, scroll behavior |
| MAUI iOS | iOS-specific control variants |
| Blazor | Async behavior, DOM updates |
| WPF | Windows automation patterns |

### UITestOrganization
- **id**: FR-970.10
- **title**: UI test organization

UI test projects should be organized:
```
Brinell.Samples.{Tech}.UITests/
├── PageObjects/
│   ├── BasePage.cs
│   ├── LoginPage.cs
│   ├── DashboardPage.cs
│   └── ...
├── Tests/
│   ├── BasicControlTests.cs
│   ├── SelectionControlTests.cs
│   ├── FormTests.cs
│   └── ...
├── Fixtures/
│   └── AppFixture.cs
└── GlobalUsings.cs
```

---

## relationships

- UI tests target [FR-950 Sample Applications](120_950_SampleApplications.spx.md)
- UI tests complement [FR-960 Unit Tests](120_960_UnitTests.spx.md)
- UI tests follow [FR-101 PageObject](120_101_PageObject.spx.md) pattern
- UI tests verify [FR-100 ControlObject](120_100_ControlObject.spx.md) implementations

---

## constraints

- UI tests require sample app running
- UI tests are slower than unit tests (expected)
- UI tests may be platform-specific
- UI tests should be tagged for selective execution

