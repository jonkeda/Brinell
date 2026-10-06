---
name: html-ui-test
description: Write or fix Brinell Html UI tests and the page objects, containers and collections they use, going only through page objects and ControlObjects - never the driver, IHtmlElement, Playwright types, or Find* calls. Use when asked to add, write, extend, or fix an Html or Blazor UI test, a web page object, or a test in testsnew/Brinell.Html.UITests, testsnew/Brinell.Blazor.UITests or samples/Todo/tests/Brinell.Samples.Todo.Web.UITests, or a control unit test in testsnew/Brinell.Html.Tests.
---

# Write Html UI tests

The same rule as MAUI: a test reads like what a user does and sees. Finding elements, waiting,
readiness and failure messages live in page objects and ControlObjects. When the test needs
something they do not offer, add it there first; never reach past them.

## Workflow

1. **Find the page object** (`PageObjects/` or `Pages/` in the test project). A Blazor page
   derives from `BlazorPageObjectBase<T>`, a plain web page from `HtmlPageObjectBase<T>`. If a
   control member is missing, add it with the `html-control` skill first.
2. **Write the test** with page-object members only (shape below).
3. **If the sample lacks what the test needs**, add it to `samples/Brinell.Samples.Blazor.App`
   with `data-testid`s and a visible effect (a status line). The test project builds the sample
   through a `ReferenceOutputAssembly="false"` reference, and the fixture starts it.
4. **Run the smallest filter that can fail**, from the Brinell root:
   `dotnet test testsnew\Brinell.Html.UITests --filter "FullyQualifiedName~FramesPageTests"`.
   One UI test process at a time. Nothing needs to be running: the fixture starts the app
   on a free port (`WebAppHost`). `BLAZOR_APP_URL` points it at a running one instead.

## Test shape

```csharp
public sealed class FramesPageTests : BlazorSampleTestBase
{
    [Fact]
    public void AControlInTheFrame_IsFoundInTheFrame_AndActsThere()
    {
        NavigateToPage("/frames");
        var page = new FramesPage(Context);

        page.InnerButton.Click();

        page.InnerStatus.AssertText("Clicked in the frame");
    }
}
```

- Navigate through the test base (`NavigateToPage`). It waits until the app is interactive.
- Assert with the controls' `Assert*` members, which wait. Read with `Get*`/`Is*` for xUnit
  `Assert` only when a chain cannot say it. No FluentAssertions.
- Assert on what the user sees (text, value, checked state, a row), never on app state.

## Rules that are Html's own

- **Locate by `data-testid`** in page objects: `Locator.ByDataTestId("TodoEdit_Title")`, or a
  CSS string `"[data-testid='x']"`. `Locator.ByAutomationId` means `data-automation-id` or `id`
  in Playwright.
- **Rows:** use a `CollectionObjectBase` whose rows carry `data-item-key`. A row object then
  follows its item through re-renders and `<Virtualize>` node reuse, and never acts on another.
- **Dialogs over a busy page:** a container for a dialog sets `AsksParent => false`, or its
  buttons wait for a page that stays busy until they are pressed. To check that a dialog is
  gone, use a page-level control; the dialog container cannot answer when it is absent.
- **Frames:** declare a `FrameScope<TPage>` and put the frame's controls on it.
- **Page predicates** use `Try*` reads (`TryGetText`), never a `Get*` call inside another call.

## Never in tests, page objects' public members or test helpers

The list in [forbidden-apis.md](references/forbidden-apis.md), mirrored from MAUI's: no
`Context.Driver`, no `IHtmlElement`, no `Find*`/`TryFindElement`, no `Locator` built in a test
method, no Playwright types (`IPage`, `ILocator`, `IFrame`), and no sleeps or hand-written wait
loops.
