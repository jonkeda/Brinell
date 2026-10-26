# The MauiUnit Head

A MAUI suite can run its page objects, controls and feature files without a device: MauiUnit renders
the app's pages in the test process, and Bruinet drives them through the same `IMauiDriver`. A run picks
its head with `BRUINET_HEAD`; the device heads (FlaUI on Windows, Appium on Android and iOS) stay the
judge at every gate. The decision is
[AD-032](../../.docs/decisions/ad-032-a-maui-suite-runs-without-a-device.md).

## When To Use It

During development and in CI, to run a MAUI suite in seconds with no app built, no window and no
emulator, on Windows, macOS or Linux. Bruinet's device suite runs 328 tests in about 20 s, and the Todo
sample's 42 scenarios in about 40 s.

At a gate, run a device head. MauiUnit has no layout, no pixels and no platform input, so a button a
layout hides, or a control the platform draws, can pass in memory and fail on a device.

## Running It

```bash
BRUINET_HEAD=mauiunit dotnet test testsnew/Bruinet.Maui.UITests
./tools/Scripts/test-maui-on-mauiunit.ps1
```

The script runs the device suite and both Todo suites, a `.trx` each; CI runs it on `windows-latest`
and `macos-latest`. Unset, or any value but `mauiunit`, is the device. Off Windows the suites build for
plain `net10.0` without FlaUI (`BruinetWindowsHead` in `Directory.Build.props`); run
`dotnet workload install maui` once there. More in [Running MAUI tests](../run/MAUI.md).

## Giving A Suite A MauiUnit Head

1. **Build the app for plain `net10.0`**: a project beside the app's own, `UseMaui`, that links the app's
   sources and XAML and leaves out `Platforms\` and `MauiProgram.cs`. The samples' are
   `samples/Bruinet.Samples.Maui.App.MauiUnit` and `samples/Todo/src/Bruinet.Samples.Todo.App.MauiUnit`.
2. **Compose the app once.** Move the registrations out of `MauiProgram` into a method both call, as
   the Todo app's `TodoAppServices.AddTodoApp`, so the in-memory app is the device app.
3. **Override `CreateMauiUnitOptions()`** on the suite's `MauiTestFixtureBase`:

```csharp
protected override MauiTestContextOptions CreateMauiUnitOptions()
{
    // Debug parses XAML at run time and knows only loaded assemblies: load what the builder would.
    _ = typeof(CommunityToolkit.Maui.Layouts.StateContainer).Assembly;
    return MauiUnitHead.Options(
        services => services.GetRequiredService<AppShell>(),           // the first page
        options => options.Application = services => new App(...),     // the app's Application
        context => context.Services.AddMyApp(settings, platform));      // the app's composition
}
```

Page objects, controls and scenarios need no change. The head reports `MauiPlatform.MauiUnit` and
otherwise answers as Windows does: WinUI's ids for the back button and title, off-screen elements in the
tree. A page object that asks `Platform == Windows` for a reason that also holds in memory says
`Platform is MauiPlatform.Windows or MauiPlatform.MauiUnit`.

## What Needs A Device

Mark a test `[DeviceFact("why")]` (or `[WindowsOnlyFact(Device = "why")]`) and a scenario `@device`: on
the MauiUnit head they are skipped with the reason, so the run still lists them. A device-only member
ends its call at once with `DeviceOnlyException` naming the member.

| Needs a device | Why |
| --- | --- |
| Layout: bounds, scroll offsets (`ReadScrollPosition`), what is below the fold | No layout; elements report 1 x 1 bounds |
| Pixels: screenshots, recordings | Nothing is drawn |
| Platform input: hover, right-click, free swipes, the clipboard | No platform |
| Controls the platform draws: a picker's popup, MediaElement's transport controls | Not in MAUI's tree |
| `#if ANDROID`, `#if IOS` code | Not compiled into a plain `net10.0` build |
| Tests of FlaUI, Appium or the UI Automation bridge themselves | Not in the process |

## What Behaves Differently

- **Visible** means on the shown page of the driver's window, with no `IsVisible="False"` ancestor. An
  off-screen carousel card is found and is not shown; scrolling succeeds at once.
- **The app runs only when a call lets it.** Each call drains MauiUnit's dispatcher queue, so a control's
  wait (`Assert*`, `WaitArrived`) is what moves the app on. A wait outside the driver must let it run (the
  Todo fixture's reset passes `whileWaiting: IsIdle`), and a fixture lets the last test's work settle
  before the next (`WaitForQuiet`).
- **The virtual clock does not move under the head.** App timers and `DispatchDelayed` work fire only
  when a test advances MauiUnit's clock; a test that needs them is a device test for now.
- **Gestures and toolkit state** go to the app's own verb sinks (`GestureAutomation.Sink` in
  `Bruinet.Maui.AppSupport`), called in process: the half of the Windows bridge that lives in the app,
  without UI Automation.
- **Dialogs**: `DisplayAlertAsync`, `DisplayPromptAsync` and `DisplayActionSheetAsync` show a `TestDialog`
  over the page; its buttons answer the app's await.
- **Shell and navigation chrome** (tab bar, flyout, back button, title) are nodes in the tree, found under
  the Windows ids.
- **Animations** end at once at their final value.
- **A file the app polls** (a reset or network-state file) is read in the test's process: write it with a
  short retry, as Windows refuses to replace a file another handle has open.

## MauiUnit On Its Own

MauiUnit (`srcnew/MauiUnit`) is also a component-testing framework for MAUI without Bruinet, on xUnit,
NUnit or MSTest:

```csharp
using var ctx = new MauiTestContext();
ctx.Services.AddSingleton<ISettingsService, PreferencesSettingsService>();
ctx.Preferences.Set("name", "Ada");

var page = ctx.Render<SettingsPage>();
page.Find<Entry>("Name").Should().HaveText("Ada");
page.Find<Entry>("Name").SetText("Grace");
page.Find<Button>("SaveButton").Click();

ctx.AdvanceTime(TimeSpan.FromSeconds(1));          // timers and delayed work, in order
page.Find<TestDialog>().Choose("OK");               // a DisplayAlertAsync, answered
```

`Render` takes a page type, an instance or a Shell; `Services` is fixed after the first render
(`Replace` swaps a registration for a fake); `Preferences` and `SecureStorage` are in memory;
`ToSnapshot()` gives the tree as indented text. Its examples are `testsnew/MauiUnit.Tests`.

## How MAUI Runs Without A Platform

What MauiUnit relies on, from MAUI 10 on plain `net10.0`:

- Controls, layouts, bindings, XAML, `NavigationPage`, modals and `Shell.GoToAsync` work with no
  handler. Bindings need a dispatcher, so MauiUnit installs one per process.
- Appearing and Disappearing are raised only for a page in a `Window` whose parent is an `Application`:
  each render is a window opened through the internal `Application.AddWindow`.
- `Shell.Current` reads `Application.Current`, which MauiUnit sets to the context's app while its work
  runs, under a lock.
- A Shell's `ContentTemplate` page is built on request (`GetOrCreateContent`) over the nearest handler's
  services, so a Shell root gets a no-op `HeadlessHandler` carrying the test's services. A `NavigationPage`
  must never get one: with a handler, a push waits for the platform.
- `CollectionView` has no item views without a handler: MauiUnit realizes every item through its
  template.
- Taps (`SendTapped`), focus (the `IsFocused` key), bindings and pending dialogs (`_pendingActions`) are
  internal: MauiUnit reaches them by reflection in one place, `MauiInternals`.
- Any animation asks for an `IAnimationManager`; MauiUnit registers MAUI's own over a ticker that reports
  animations off.
- A picker selects by equality, so MauiUnit refuses to pick an item equal to an earlier one, as the
  Windows bridge does.
