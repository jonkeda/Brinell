# Test Migration Patterns: Old Blazor → New Html Architecture

**Date:** February 23, 2026  
**Status:** Research Complete

---

## 1. Summary of Old Test Pattern (`tests/Brinell.Blazor.Tests.ControlObject6/`)

### Architecture

| Aspect | Old Pattern |
|--------|-------------|
| **Project** | `Brinell.Blazor.Tests.ControlObject6` |
| **References** | `src/Brinell.Core`, `src/Brinell.Blazor` |
| **Mocking Target** | Playwright's `IPage` and `ILocator` interfaces (via Moq) |
| **Assertion Style** | FluentAssertions (`.Should().Be(...)`, `.Should().BeTrue()`) |
| **GlobalUsings** | `Xunit`, `Moq`, `FluentAssertions` |
| **Test Style** | Async (`Task`-returning), direct control instantiation |
| **Context** | `BlazorTestContext(mockPage.Object)` — wraps Playwright `IPage` |
| **Mock Factory** | `MockPlaywrightFactory` (static helper class) |

### MockPlaywrightFactory Pattern

The old mocking layer creates Playwright mocks directly:

```csharp
// Creates Mock<IPage>
var mockPage = MockPlaywrightFactory.CreateMockPage();

// Creates Mock<ILocator> with configurable defaults
var mockLocator = MockPlaywrightFactory.CreateMockLocator(
    text: "Test Text", visible: true, enabled: true, count: 1);

// Wires IPage.Locator() → returns mockLocator
MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

// Additional helpers
MockPlaywrightFactory.SetupLocatorTimeout(mockLocator);
MockPlaywrightFactory.SetupLocatorNotFound(mockLocator);
```

The factory pre-configures ~12 Playwright methods:
- `CountAsync`, `IsVisibleAsync`, `IsEnabledAsync`, `InnerTextAsync`, `InputValueAsync`
- `WaitForAsync`, `ClickAsync`, `FocusAsync`, `ClearAsync`, `FillAsync`
- Page locator strategies: `Locator()`, `GetByTestId()`, `GetByText()`, `GetByLabel()`, `GetByRole()`, `GetByPlaceholder()`, `GetByTitle()`

### Typical Old Unit Test (ButtonControlTests.cs)

```csharp
[Fact]
public async Task BC003_ClickAsync_CallsLocatorClick()
{
    // Arrange — 4-step Playwright mock setup
    var mockPage = MockPlaywrightFactory.CreateMockPage();
    var mockLocator = MockPlaywrightFactory.CreateMockLocator();
    MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);
    var context = new BlazorTestContext(mockPage.Object);
    context.DefaultTimeoutMs = 100;

    // Create control with (context, testId, parent)
    var button = new ButtonControl(context, "submitBtn", null);

    // Act
    await button.ClickAsync();

    // Assert — FluentAssertions style not used here, Moq.Verify
    mockLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
}
```

### Old Test Characteristics

1. **Every test** creates `mockPage` + `mockLocator` + `SetupLocator` + `BlazorTestContext`
2. **Async throughout** — all methods return `Task`
3. **Controls constructed with** `(context, testIdOrLocator, parentControl)`
4. **Assertions mix** FluentAssertions (`.Should()`) and Moq `.Verify()`
5. **Platform-coupled** — mocking Playwright's `ILocator` directly means tests know about Playwright
6. **Test IDs** like `BCB001_`, `AC003_`, `BIM001_` — spec-based naming
7. **Blazor-only controls** (AudioControl, ImageControl, VideoControl) use `EvaluateAsync` mocks

---

## 2. Summary of New Test Pattern (`testsnew/`)

### Two Test Layers

The new architecture splits tests into:

| Layer | Project | Purpose | Mocking |
|-------|---------|---------|---------|
| **Unit Tests** | `Brinell.Html.Tests` | Mock-based unit tests for controls | Moq on `IHtmlElement`/`IHtmlTestContext` |
| **UI Tests** | `Brinell.Html.UITests` | Integration tests with real browser | No mocks — real Playwright via `PlaywrightTestContext` |

### testsnew/Brinell.Html.Tests (Unit Test Project)

| Aspect | Value |
|--------|-------|
| **References** | `srcnew/Brinell.Core`, `srcnew/Brinell.Html` |
| **GlobalUsings** | `Xunit`, `Moq`, `Brinell.Core.Abstractions`, `Brinell.Core.Interfaces`, `Brinell.Core.Locators` |
| **Status** | Empty shell — no test files yet, ready for population |
| **No FluentAssertions** | Not referenced — confirms switch to `Assert.*` |

**Key insight:** This project references `Brinell.Html` (not `Brinell.Html.Playwright`), so unit tests mock `IHtmlElement`/`IHtmlTestContext` instead of Playwright interfaces.

### testsnew/Brinell.Blazor.Tests (Blazor Unit Test Project)

| Aspect | Value |
|--------|-------|
| **References** | `srcnew/Brinell.Core`, `srcnew/Brinell.Blazor` |
| **GlobalUsings** | `Xunit`, `Moq`, `Brinell.Core.Abstractions`, `Brinell.Core.Interfaces`, `Brinell.Core.Locators` |
| **Status** | Empty shell — no test files yet |
| **Commented usings** | `// global using Brinell.Blazor.Context;` `// global using Brinell.Blazor.Controls;` |

### testsnew/Brinell.Html.UITests (Integration Test Project)

| Aspect | Value |
|--------|-------|
| **References** | `srcnew/Brinell.Core`, `srcnew/Brinell.Html`, `srcnew/Brinell.Html.Playwright` |
| **GlobalUsings** | `Xunit`, `Brinell.Core.*`, `Brinell.Html.Context`, `Brinell.Html.Controls`, `Brinell.Html.Interfaces`, `Brinell.Html.Pages`, `Brinell.Html.Playwright` |
| **Base class** | `BlazorSampleTestBase` (creates `PlaywrightTestContext`, handles `IAsyncLifetime`) |
| **No Moq** | Integration tests — no mocking |

---

## 3. New Control Architecture in Tests

### Control Construction (New)

Controls are now **generic** with a **scope parameter** and take `(scope, selectorOrLocator)`:

```csharp
// Page objects define controls with CSS selectors
public ButtonControl<CounterPage> IncrementButton => new(this, "[data-testid='increment-btn']");
public CheckBoxControl<FormControlsPage> TermsCheckBox => new(this, "#terms-checkbox");
public TextInputControl<LoginPage> EmailInput => new(this, "[data-testid='email-input']");
```

Controls are `ButtonControl<TScope>`, `CheckBoxControl<TScope>`, etc. — parameterized by their parent scope for fluent chaining.

### Page Object Pattern (New)

```csharp
public sealed class CounterPage : HtmlPageObjectBase<CounterPage>
{
    public CounterPage(IHtmlTestContext context) : base(context) { }

    public LabelControl<CounterPage> CountDisplay => new(this, "[data-testid='count-display']");
    public ButtonControl<CounterPage> IncrementButton => new(this, "[data-testid='increment-btn']");
    public ButtonControl<CounterPage> ResetButton => new(this, "[data-testid='reset-btn']");
}
```

### Synchronous API (New)

All control methods are **synchronous** (no `async`/`await`):

```csharp
page.IncrementButton.Click();           // Not ClickAsync()
page.CountDisplay.AssertText("...");     // Not AssertTextAsync()
page.TermsCheckBox.Check();             // Not CheckAsync()
page.EmailInput.SetText("...");         // Not SetTextAsync()
var val = page.EmailInput.GetValue();   // Not GetValueAsync()
```

### Assertion Style (New)

| Style | Example |
|-------|---------|
| **Control built-in assertions** | `page.CountDisplay.AssertText("Current count: 1")` |
| **Control state assertions** | `page.IncrementButton.AssertEnabled(true)` |
| **Contains assertion** | `page.SuccessMessage.AssertTextContaining("Login successful")` |
| **Wait + assert** | `page.SuccessMessage.WaitVisible(true)` then assert |
| **xUnit Assert** | `Assert.True(...)`, `Assert.Equal(...)`, `Assert.NotEmpty(...)` |

**No FluentAssertions** — assertions use either xUnit `Assert.*` or control built-in `Assert*` methods.

---

## 4. Mocking Pattern Comparison

### Old: Mock Playwright Directly

```csharp
// Old: 12+ Playwright method setups per test
var mockPage = MockPlaywrightFactory.CreateMockPage();
var mockLocator = MockPlaywrightFactory.CreateMockLocator(text: "Click Me");
MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);
var context = new BlazorTestContext(mockPage.Object);

var button = new ButtonControl(context, "btn", null);
await button.ClickAsync();
mockLocator.Verify(l => l.ClickAsync(...));
```

### New: Mock IHtmlElement + IHtmlTestContext (Projected)

Based on the interfaces and `Brinell.Maui.Tests.FluentChainingTests` which shows the established pattern:

```csharp
// New: Mock the framework's own interfaces, not Playwright
var mockContext = new Mock<IHtmlTestContext>();
var mockElement = new Mock<IHtmlElement>();

mockContext.Setup(c => c.Timeouts).Returns(new TimeoutSettings { ... });
mockContext.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.Css);

// FindElement returns the mock element
mockContext.Setup(c => c.FindElement(It.IsAny<Locator>()))
    .Returns(mockElement.Object);

mockElement.Setup(e => e.IsVisible).Returns(true);
mockElement.Setup(e => e.GetText()).Returns("Click Me");

// Controls use scope, not context directly
var page = new TestPage(mockContext.Object);
page.TestButton.Click();  // synchronous
```

### Key Differences

| Aspect | Old | New |
|--------|-----|-----|
| **Mock target** | `Mock<IPage>`, `Mock<ILocator>` (Playwright) | `Mock<IHtmlTestContext>`, `Mock<IHtmlElement>` (framework) |
| **Setup complexity** | 12+ methods per Locator mock | Fewer: `GetText()`, `IsVisible`, `Click()`, etc. |
| **Async** | All `async Task` | All synchronous |
| **Assertions** | FluentAssertions `.Should()` | xUnit `Assert.*` + control `Assert*()` |
| **Control constructor** | `(context, testId, parent)` | `(scope, selectorOrLocator)` |
| **Context** | `BlazorTestContext(IPage)` | `IHtmlTestContext` (platform-agnostic) |
| **Fluent chaining** | Not present | Action methods return `TScope` |

---

## 5. Concrete Migration Examples

### Example 1: Button Click Test

**Old:**
```csharp
[Fact]
public async Task BC003_ClickAsync_CallsLocatorClick()
{
    var mockPage = MockPlaywrightFactory.CreateMockPage();
    var mockLocator = MockPlaywrightFactory.CreateMockLocator();
    MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);
    var context = new BlazorTestContext(mockPage.Object);
    context.DefaultTimeoutMs = 100;

    var button = new ButtonControl(context, "submitBtn", null);
    await button.ClickAsync();

    mockLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
}
```

**New (Unit Test — projected):**
```csharp
[Fact]
public void Click_CallsElementClick()
{
    var mockContext = new Mock<IHtmlTestContext>();
    var mockElement = new Mock<IHtmlElement>();
    mockContext.Setup(c => c.FindElement(It.IsAny<Locator>())).Returns(mockElement.Object);
    mockContext.Setup(c => c.Timeouts).Returns(new TimeoutSettings());

    var page = new TestPage(mockContext.Object);
    page.TestButton.Click();

    mockElement.Verify(e => e.Click(), Times.Once);
}
```

**New (UI Test — actual):**
```csharp
[Fact]
public void Button_Click_IncrementsCounter()
{
    NavigateToPage("/counter");
    var page = new CounterPage(Context);

    page.CountDisplay.AssertText("Current count: 0");
    page.IncrementButton.Click();
    page.CountDisplay.AssertText("Current count: 1");
}
```

### Example 2: CheckBox State Test

**Old:**
```csharp
[Fact]
public async Task BCB003_IsCheckedAsync_WhenChecked_ReturnsTrue()
{
    var mockPage = MockPlaywrightFactory.CreateMockPage();
    var mockLocator = MockPlaywrightFactory.CreateMockLocator();
    mockLocator.Setup(l => l.IsCheckedAsync(It.IsAny<LocatorIsCheckedOptions?>()))
        .ReturnsAsync(true);
    MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);
    var context = new BlazorTestContext(mockPage.Object);
    context.DefaultTimeoutMs = 100;

    var checkBox = new CheckBoxControl(context, "checkbox", null);
    var isChecked = await checkBox.IsCheckedAsync();

    isChecked.Should().BeTrue();
}
```

**New (UI Test — actual):**
```csharp
[Fact]
public void CheckBox_Check_SetsCheckedTrue()
{
    NavigateToPage("/form-controls");
    var page = new FormControlsPage(Context);

    page.TermsCheckBox.Uncheck();
    page.TermsCheckBox.Check();

    Assert.True(page.TermsCheckBox.IsChecked());
}
```

### Example 3: Image/Audio (Blazor-Only Controls)

**Old:** These used Playwright `EvaluateAsync` for JavaScript calls:
```csharp
[Fact]
public async Task AC003_PlayAsync_CallsPlayOnElement()
{
    var mockLocator = MockPlaywrightFactory.CreateMockLocator();
    mockLocator.Setup(l => l.EvaluateAsync(It.IsAny<string>(), It.IsAny<object?>()))
        .ReturnsAsync((System.Text.Json.JsonElement?)null);
    // ...
    await audio.PlayAsync();
    mockLocator.Verify(l => l.EvaluateAsync("audio => audio.play()", ...), Times.Once);
}
```

**New:** These Blazor-only controls would either:
1. Move to `Brinell.Blazor` package as Blazor-specific HTML controls
2. Use `IHtmlElement.GetDomProperty()` or `ExecuteScript()` abstractions instead of Playwright's `EvaluateAsync`

---

## 6. Migration Checklist

### For Each Old Test File

- [ ] **Remove Playwright imports** (`using Microsoft.Playwright`)
- [ ] **Remove `MockPlaywrightFactory`** — replace with `IHtmlElement`/`IHtmlTestContext` mocks
- [ ] **Remove FluentAssertions** — use xUnit `Assert.*` or control `Assert*()` methods
- [ ] **Convert `async Task` → sync `void`** for all test methods
- [ ] **Convert `await x.MethodAsync()` → `x.Method()`** — sync API
- [ ] **Update control constructors** — `new ButtonControl(context, "id", null)` → `new ButtonControl<TestPage>(scope, "selector")`
- [ ] **Update assertion style** — `.Should().BeTrue()` → `Assert.True(...)` 
- [ ] **Update Moq verifications** — `mockLocator.Verify(...)` → `mockElement.Verify(...)`
- [ ] **Update namespace** — `Brinell.Blazor.Tests.ControlObject6` → `Brinell.Html.Tests` or `Brinell.Blazor.Tests`
- [ ] **Add Trait attributes** — `[Trait("Category", "...")]`, `[Trait("Platform", "...")]`

### New MockFactory Pattern Needed

Create `MockHtmlFactory.cs` in `testsnew/Brinell.Html.Tests/Mocks/`:

```csharp
public static class MockHtmlFactory
{
    public static Mock<IHtmlTestContext> CreateMockContext()
    {
        var mock = new Mock<IHtmlTestContext>();
        mock.Setup(c => c.Timeouts).Returns(new TimeoutSettings());
        mock.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.Css);
        return mock;
    }

    public static Mock<IHtmlElement> CreateMockElement(
        string text = "Test",
        bool visible = true,
        bool enabled = true,
        bool exists = true)
    {
        var mock = new Mock<IHtmlElement>();
        mock.Setup(e => e.GetText()).Returns(text);
        mock.Setup(e => e.IsVisible).Returns(visible);
        mock.Setup(e => e.IsEnabled).Returns(enabled);
        mock.Setup(e => e.IsChecked).Returns(false);
        mock.Setup(e => e.InputValue).Returns(text);
        return mock;
    }
}
```

---

## 7. Key Findings

1. **`Brinell.Html.Tests` and `Brinell.Blazor.Tests` are empty shells** — ready for unit tests but contain no test files yet
2. **`Brinell.Html.UITests` has working integration tests** — 9 test files showing the complete new pattern
3. **No mock helpers exist yet in testsnew/** — `MockHtmlFactory` needs to be created
4. **The MAUI test project** (`Brinell.Maui.Tests/FluentChainingTests.cs`) shows the established mock pattern for `IMauiTestContext`/`IMauiElement` — the HTML mock pattern should mirror this
5. **FluentAssertions removed** — new tests use xUnit `Assert.*` exclusively
6. **Sync API** — all new control methods are synchronous; no `async`/`await`
7. **Generic scoped controls** — `ButtonControl<TScope>` pattern enables fluent chaining (`button.Click()` returns `TScope`)
8. **IHtmlElement** is the new mock target — replaces Playwright's `ILocator` with a platform-agnostic interface

---

## 8. Files Reviewed

### Old Tests (tests/Brinell.Blazor.Tests.ControlObject6/)
- `Mocks/MockPlaywrightFactory.cs` — Full mock factory for Playwright
- `Controls/ButtonControlTests.cs` — 10 tests, standard control pattern
- `Controls/CheckBoxControlTests.cs` — 18+ tests, toggle control with regions
- `Controls/AudioControlTests.cs` — Blazor-only media control, uses EvaluateAsync
- `Controls/ImageControlTests.cs` — Blazor-only, GetAttribute mocking

### New Tests (testsnew/)
- `Brinell.Html.Tests/Brinell.Html.Tests.csproj` + `GlobalUsings.cs` — Empty shell
- `Brinell.Blazor.Tests/Brinell.Blazor.Tests.csproj` + `GlobalUsings.cs` — Empty shell
- `Brinell.Html.UITests/TestBase/BlazorSampleTestBase.cs` — Integration test base
- `Brinell.Html.UITests/PageObjects/CounterPage.cs` — New page object pattern
- `Brinell.Html.UITests/PageObjects/FormControlsPage.cs` — Multi-control page
- `Brinell.Html.UITests/PageObjects/LoginPage.cs` — Text input + button page
- `Brinell.Html.UITests/Tests/Controls/ButtonControlTests.cs` — 3 integration tests
- `Brinell.Html.UITests/Tests/Controls/CheckBoxControlTests.cs` — 3 integration tests
- `Brinell.Html.UITests/Tests/Controls/SelectControlTests.cs` — 3 integration tests
- `Brinell.Html.UITests/Tests/Controls/TextInputControlTests.cs` — 3 integration tests
- `Brinell.Html.UITests/Tests/Pages/CounterPageTests.cs` — Page-level tests
- `Brinell.Html.UITests/Tests/Pages/LoginPageTests.cs` — Flow tests
- `Brinell.Html.UITests/Tests/Scenarios/LoginFlowTests.cs` — E2E scenario
- `Brinell.Maui.Tests/FluentChainingTests.cs` — Reference mock pattern
