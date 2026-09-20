---
name: maui-ui-test
description: Write or fix Brinell MAUI UI tests and the page objects, app containers and collections they use, going only through page objects and ControlObjects - never the driver, IMauiElement, or Find* calls. Use when asked to add, write, extend, or fix a UI test, a test page object, or a test for a MAUI or CommunityToolkit control in testsnew/Brinell.Maui.UITests, or a control unit test in testsnew/Brinell.Maui.Tests.
---

# Write MAUI UI tests

A test reads like what a user does and sees. Everything else - finding elements, waiting,
platform routes, failure messages - lives in page objects and ControlObjects. When the test
needs something they do not offer, add it there first; never reach past them.

## Workflow

1. **Find the page object** in `testsnew/Brinell.Maui.UITests/Pages/`. If it is missing, create
   it ([page-objects.md](references/page-objects.md)). If a control, container, or control
   member is missing, stop writing the test and add it with the `maui-control` skill first.
2. **Write the test** (shape below) using only page-object members.
3. **If the sample app lacks what the test needs**, add it to `samples/Brinell.Samples.Maui.App`
   with `AutomationId`s and a status label that shows the result, then rebuild the app:
   `dotnet build srcnew\Brinell.sln -v:minimal /nr:false` builds it, including the Windows exe
   the fixture launches.
4. **Run the smallest tier that can fail**:
   `dotnet test testsnew\Brinell.Maui.UITests\Brinell.Maui.UITests.csproj --filter "Control=Foo"`.
   One UI test process at a time, and never build while it runs. Tier 2b
   (`--filter "Stage=Background"`) also launches the Shell sample app, which `Brinell.sln` does
   not build; in a fresh checkout build it first:
   `dotnet build samples\Brinell.Samples.Maui.ShellApp\Brinell.Samples.Maui.ShellApp.csproj -f net10.0-windows10.0.19041.0 -v:minimal /nr:false`.

## Test shape

```csharp
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.CommunityToolkit;

/// <summary>UI tests for the CommunityToolkit MediaElement, playing a six-second local tone.</summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "MediaElement")]
public class MediaElementTests
{
    private readonly MauiFixture _fixture;

    public MediaElementTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.Open(SamplePage.CommunityToolkitMedia);
    }

    private CommunityToolkitMediaTestPage GetPage() => new(_fixture.Context);

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Stop")]
    public Task MediaElement_Stop_PausesAndReturnsToTheStart()
    {
        GetPage().TestMediaElement.Play()
            .AssertProgressGreaterThan(0)
            .Stop()
            .AssertPlaying(false)
            .AssertProgress(0);
        return Task.CompletedTask;
    }
}
```

- Class `<Control>Tests` in `Tests/<Area>/`, `[Collection("Maui")]`,
  `[Trait("Category", "UITest")]`, `[Trait("Control", "<Control>")]`.
- Each test `[Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]` with
  `[Trait("Method", "<Member>")]`, returning `Task` (`return Task.CompletedTask;`): xUnit
  enforces `Timeout` only on async tests.
- The constructor takes `MauiFixture` and opens the page (`_fixture.Open(SamplePage.X)`). The
  fixture is shared by the collection, so a test that changes state resets it through the UI (a
  Reset button, `Stop()`), not by relaunching.
- Name: `<Control>_<Action>_<ExpectedResult>`.
- Body: a fluent chain of actions and `Assert*` calls, which wait. `Assert*` on a part returns
  to the component or container, so the chain continues
  (`media.Play().PlayPauseButton.AssertPlaying(true).Stop()`).
- Use xUnit `Assert` on values read with `Get*`/`Is*` only when the chain cannot express it
  (`Assert.InRange(duration.Value, 5, 7)`). No FluentAssertions.
- Assert on what the user sees (status label, text, checked state), never the view model.
- On Windows `IsVisible` means "on screen now"; it never scrolls. To prove something is shown,
  `AssertVisibleAfterScroll` may scroll it into view first; to prove something is *not* shown
  (an off-screen carousel card), use `AssertVisible(false)`, never a scrolling check, because
  scrolling would change the state under test.
- In a virtualized collection an off-screen row may not be realized at all, and whether it is
  depends on timing. `Item(i)` then waits out its budget and throws. Check it as
  `collection.TryItem(i)?.Name.AssertVisible(false)`: not realized is not shown.
- Negative cases use the API: `AssertExists(false)`, `AssertOpen(false)`, `TryItem(i)` returning
  null, `Assert.Throws<ElementNotFoundException>(...)`. A lookup expected to fail waits its whole
  budget first (`Item(i)` and `ItemWhere` wait), so give it a short one:
  `Assert.Throws<ElementNotFoundException>(() => list.Item(9999, timeoutMs: 500))`.
- **No** `Thread.Sleep`, `Task.Delay`, or try/catch around control calls. A flaky wait is a
  control bug: fix the control. A raised `timeoutMs` is right only for an operation that is
  long by nature - scrolling a long list, a search through it - and gets a comment saying so.
  Never raise one to hide a flake.
- `timeoutMs` bounds the **wait for the control**, not the whole call. An action that confirms its
  own effect (`Toggle`, `SetChecked`, `CarouselView.Next`, `Stepper.SetValue`) waits again
  afterwards, on a budget of its own, so `Toggle(timeoutMs: 300)` can take about 600 ms. Size a
  budget by how long the control may take to become ready, not by how long the test may block.
- What a failure means: `ScopeNotReadyException` - a page, container or row never became
  ready (it names which, and what it saw); `ElementNotReadyException` - found, but disabled or
  not visible; `StaleElementException` - replaced after an action that ran once;
  `WaitTimeoutException` naming an exception type and "N of M attempts raised it" - every
  attempt threw something unexpected (the real exception is its `InnerException`);
  `RouteUnavailableException` - this element, driver or platform has no route for what was asked,
  reported at once rather than waited out; `AppUnavailableException` - the app exited or the
  session was lost, reported at once. A near-miss
  warning in the call log means the call passed only after trouble; set `BRINELL_CALL_LOG` to a
  folder to write every call to CSV.
- Comments follow the policy in
  [.github/instructions/csharp-comments.instructions.md](../../instructions/csharp-comments.instructions.md):
  one `<summary>` line on the test class; no `<remarks>` restating what a test does.

## The never-list

In tests, page objects' public members, app containers and test helpers, do not use:

| Forbidden | Instead |
| --- | --- |
| `Context.Driver`, any `IMauiDriver` member | a control or page member; add one if missing |
| a variable, parameter or return of type `IMauiElement`, or its members (`.Text`, `.Invoke()`, `.ReadState()`, `.PerformGesture()`) | the control's `Get*`/`Is*`/action |
| `FindElement`, `FindElements`, `TryFindElement` on a page, container, context or `AppRoot` | a named control property, `Child<T>(id)`, or a collection's `Item`/`Items`/`ItemWhere` |
| `ContainerRoot`, `AppElement`, `TryGetItemRoot`, `ScrollingRoot` | a container, collection, or `AppRoot`-scoped control |
| `new Locator(...)` / `Locator.By*` in a test method | a page-object property |

One structural exception: a collection row's constructor takes `IMauiElement itemRoot` and
passes it to `ItemObjectBase` untouched; the collection's item factory does the same. Never
read or call it.

Ids (strings) are not locators: `Child<T>(id)`, `Label(id)`, `Button(id)` on a container are
allowed in a test. Prefer a named property for anything the page or container owns; a one-off id
is right when naming it would mislead, such as proving a control outside a container is *not*
found through it.

Why, and the one exception (framework tests of the driver and bridge themselves):
[forbidden-apis.md](references/forbidden-apis.md). Existing tests that break these rules are not
a model; do not copy them.

## Control unit tests

Logic the UI cannot reach cheaply (stale-element recovery, idempotence, error messages,
null-skip) is tested in `testsnew/Brinell.Maui.Tests` with mocked elements behind the public
API: [unit-tests.md](references/unit-tests.md).

## Failures

Read the screenshot, runner output and app log before changing anything. Check the known
baselines (environment-dependent tests such as `OccludedScreenshotTests`; platform tests known
to fail before any change) before calling a failure a regression. Rebuild the sample app
whenever `samples/` changed: `dotnet test` builds the test project, not the app it launches.

## Report

Finish with: files changed, the tests added (name and what each proves), members added to
controls and pages, test commands run with exact pass/fail counts, and anything skipped and why.
