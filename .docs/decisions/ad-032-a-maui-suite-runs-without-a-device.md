# AD-032: A MAUI Suite Runs Without A Device, And The Device Judges The Gate

`Bruinet.Maui.Headless` is a third implementation of MAUI's interfaces (`IMauiDriver`, `IMauiElement`,
beside FlaUI and Appium) over MauiHeadless: the app's pages, view models and services rendered in the test
process, with MAUI's own bindings, commands, `NavigationPage` and Shell and no platform handlers. Page
objects, controls, collections, the call model and the feature files run on it unchanged; only the
fixture chooses. A run's head is `BRUINET_HEAD` (`Bruinet.Core.Testing.MauiHeads`: `headless`, or the
device, the default), read by `MauiTestFixtureBase`, `DeviceFactAttribute` and the scenario runner
alike. The head reports `MauiPlatform.Headless` and otherwise answers as Windows' UI Automation does.
The device heads run every gate.

MauiHeadless itself (`src/MauiHeadless`) is a framework of its own, a bUnit for MAUI, on xUnit, NUnit and
MSTest: it references MAUI and nothing of Bruinet's. A fixture names its app in `CreateHeadlessOptions`,
from the app's own plain `net10.0` target beside its device targets (one project, the test
project's reference pinned to `net10.0`); the app's composition is one method
the device's `MauiProgram` and the fixture both call.

What the MauiHeadless head cannot answer (layout, bounds, pixels, platform input, native pickers and
dialogs, `#if` platform code) throws `DeviceOnlyException`, which carries `IEndsCall`, so the call ends
at once instead of polling to its budget. A test that needs a device says `[DeviceFact("why")]`, or
`[WindowsOnlyFact(Device = "why")]`, a scenario `@device`: skipped on the MauiHeadless head with the reason.
Its dispatcher is a queue the head drains at every call, with a virtual clock, so a run is
deterministic; the app's work runs only when a call lets it.

**Why:** a device is most of a MAUI test's cost, and the only reason a suite needs Windows, an emulator
or a Mac with a simulator. Headless the sample device suite ran 325 tests in about 20 s and the Todo
sample its 42 scenarios in about 40 s, with no app built, no window and no emulator, and the same
projects build off Windows ([the guide](../../docs/guides/maui-headless.md)). MAUI already kept its driver behind its own interfaces (AD-010), so a
third driver costs two projects and no change to a page object.

**Name:** the head was MauiUnit (91211700), chosen over Headless because "headless" is the browser's word. It is MauiHeadless since 2026-10-27: Headless says what the head is, no device and no window, to someone who does not know bUnit, where MauiUnit reads as a unit-test framework; the `MauiHeadless` prefix keeps it apart from the browser's `Headless` option.

**Ruled out:**
- tests written against MauiHeadless's own API (`Find`, `Click` on a `VisualNode`) for the app: a second
  set of locators beside the page objects, and no feature file runs on them. MauiHeadless's API is for
  component tests of a page or a view on their own;
- view-model tests only: the bindings, navigation and dialogs, which break most often, are not in them;
- fake platform handlers for every control: a fake of each platform's behaviour that drifts from it,
  where MAUI's virtual views already hold the state a test reads; only layout would earn fake leaf
  handlers, and only once a test needs them;
- a head per test project: one variable keeps one project and lets a run choose; off Windows the same
  projects build for plain `net10.0` without FlaUI (`BruinetWindowsHead`);
- making the MauiHeadless head the gate: it passes a page whose layout hides a button;
- a second project per app that links its sources for `net10.0` (the samples' `*.App.Headless`
  until 2026-10-29): two project files to keep in step and three more in the solution, for keeping
  one target out of the app's own build.

**The project earns its place by AD-026's R3:** MauiHeadless stays out of every suite that only drives
a device, and Bruinet.Maui does not reference it. A device suite launches the built app and does not
reference it; building the app project whole builds its `net10.0` target too.

**Broken when:** a page object, control or phrase needs a change to run on MauiHeadless; `Bruinet.Maui`
references MauiHeadless, or MauiHeadless references Bruinet; a device-only member waits out its budget on
MauiHeadless instead of ending the call; a test asks `Platform == Windows` for a reason that holds in
memory and skips MauiHeadless; or a gate passes on the MauiHeadless head alone.
