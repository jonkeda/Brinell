# AD-032: A MAUI Suite Runs Without A Device, And The Device Judges The Gate

`Bruinet.Maui.MauiUnit` is a third implementation of MAUI's interfaces (`IMauiDriver`, `IMauiElement`,
beside FlaUI and Appium) over MauiUnit: the app's pages, view models and services rendered in the test
process, with MAUI's own bindings, commands, `NavigationPage` and Shell and no platform handlers. Page
objects, controls, collections, the call model and the feature files run on it unchanged; only the
fixture chooses. A run's head is `BRUINET_HEAD` (`Bruinet.Core.Testing.MauiHeads`: `mauiunit`, or the
device, the default), read by `MauiTestFixtureBase`, `DeviceFactAttribute` and the scenario runner
alike. The head reports `MauiPlatform.MauiUnit` and otherwise answers as Windows' UI Automation does.
The device heads run every gate.

MauiUnit itself (`srcnew/MauiUnit`) is a framework of its own, a bUnit for MAUI, on xUnit, NUnit and
MSTest: it references MAUI and nothing of Bruinet's. A fixture names its app in `CreateMauiUnitOptions`,
from the app compiled for plain `net10.0` beside its device heads; the app's composition is one method
the device's `MauiProgram` and the fixture both call.

What the MauiUnit head cannot answer (layout, bounds, pixels, platform input, native pickers and
dialogs, `#if` platform code) throws `DeviceOnlyException`, which carries `IEndsCall`, so the call ends
at once instead of polling to its budget. A test that needs a device says `[DeviceFact("why")]`, or
`[WindowsOnlyFact(Device = "why")]`, a scenario `@device`: skipped on the MauiUnit head with the reason.
Its dispatcher is a queue the head drains at every call, with a virtual clock, so a run is
deterministic; the app's work runs only when a call lets it.

**Why:** a device is most of a MAUI test's cost, and the only reason a suite needs Windows, an emulator
or a Mac with a simulator. In memory the sample device suite ran 325 tests in about 20 s and the Todo
sample its 42 scenarios in about 40 s, with no app built, no window and no emulator, and the same
projects build off Windows (`.my/mUnit/`). MAUI already kept its driver behind its own interfaces (AD-010), so a
third driver costs two projects and no change to a page object.

**Ruled out:**
- tests written against MauiUnit's own API (`Find`, `Click` on a `VisualNode`) for the app: a second
  set of locators beside the page objects, and no feature file runs on them. MauiUnit's API is for
  component tests of a page or a view on their own;
- view-model tests only: the bindings, navigation and dialogs, which break most often, are not in them;
- fake platform handlers for every control: a fake of each platform's behaviour that drifts from it,
  where MAUI's virtual views already hold the state a test reads; only layout would earn fake leaf
  handlers, and only once a test needs them;
- a head per test project: one variable keeps one project and lets a run choose; off Windows the same
  projects build for plain `net10.0` without FlaUI (`BruinetWindowsHead`);
- making the MauiUnit head the gate: it passes a page whose layout hides a button.

**The project earns its place by AD-026's R3:** MauiUnit, and the apps' MauiUnit builds, stay out of every
suite that only drives a device; Bruinet.Maui does not reference them.

**Broken when:** a page object, control or phrase needs a change to run on MauiUnit; `Bruinet.Maui`
references MauiUnit, or MauiUnit references Bruinet; a device-only member waits out its budget on
MauiUnit instead of ending the call; a test asks `Platform == Windows` for a reason that holds in
memory and skips MauiUnit; or a gate passes on the MauiUnit head alone.
