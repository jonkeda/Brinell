# Control unit tests

`testsnew/Brinell.Maui.Tests` tests control logic without an app. A mocked `IMauiElement` is
the **fixture**; the test still calls the control's **public API** through a test page. This is
the one place a test sets up `IMauiElement`, and it only arranges: the act and the assert go
through the control.

Use it for logic the UI cannot reach cheaply or reliably:

- which platform operation a control asks for (`SetChecked` rather than `Toggle` or `Click`);
- stale-element recovery and root re-resolution;
- idempotence and null-skip;
- error messages naming the locator;
- waiting and timeouts (`Until`, absence tolerance).

Do not use it to assert what the user sees in a real app: that is a UI test.

## Shape

Derive from `Semantic/SemanticControlTestsBase`. It provides a mocked `IMauiTestContext`
(`Context`) with short timeouts, a loaded `TestPage` (`Page`), and element factories:
`CreateElement`, `CreateInvokableElement`, `CreateSelectableElement`, `CreateToggleElement`.
Add the control under test to `TestPage` as a property, like any page object.

```csharp
public class CheckBoxControlTests : SemanticControlTestsBase
{
    [Fact]
    public void CheckBox_SetChecked_AsksForTheStateWhereThePlatformCan()
    {
        // Arrange: the mock is the fixture.
        var checkBox = CreateToggleElement("IncludeProblemReports", 0, 0, 32, 32, initialState: false);
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "IncludeProblemReports")))
            .Returns(checkBox.Object);
        // Actions resolve through FindElement; state reads go through TryFindElement. Stub both.
        Context
            .Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == "IncludeProblemReports")))
            .Returns(checkBox.Object);

        // Act: through the control's public API.
        Page.IncludeProblemReports.Check();

        // Assert: what the control asked the platform for, and what it now reports.
        checkBox.Verify(e => e.SetChecked(true), Times.Once);
        checkBox.Verify(e => e.Toggle(), Times.Never);
        Assert.True(Page.IncludeProblemReports.IsChecked());
    }
}
```

## Rules

- Mocks are set up in the arrange section or in the base; never call a mock's members in the act.
- Act through the control or page (`Page.X.Check()`), never through `IMauiElement`.
- Assert with `Verify` on the mock for "which operation was asked for", and with xUnit `Assert`
  on the control's `Get*`/`Is*` for "what it reports". No FluentAssertions.
- Name: `<Control>_<Action>_<ExpectedResult>`, as for UI tests.
- Run: `dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -v:minimal /nr:false`
  (seconds; no app, no UI).

Generator behaviour (what a template emits) is tested in `testsnew/Brinell.Generator.Tests`,
not here.
