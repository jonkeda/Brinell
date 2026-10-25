# MUnit: where it stands

MUnit (a temporary name) is a headless component-testing framework for .NET MAUI, a bUnit for
MAUI. `Brinell.Maui.Munit` is a Brinell head on top of it. This folder tracks the work while it is
under way. Started 2026-10-25.

## Files here

- [README.md](README.md): this page, with the status and the log.
- [proposal.md](proposal.md): the architecture proposal, how bUnit maps onto MAUI, the feasibility and the roadmap. It comes after the core is green.
- [findings.md](findings.md): what the spikes proved about MAUI without a platform, each point with the test that showed it.

## Status

| Piece | Where | State |
| --- | --- | --- |
| MUnit core | `srcnew/MUnit` | Written; green under its tests |
| Sample app (XAML on plain net10.0) | `samples/MUnit.Samples.App` | Settings (XAML, DI, async command), Greeting view, Products (CollectionView, toolbar, push), Shell, Countdown (timer) |
| xUnit tests | `testsnew/MUnit.Tests` | 30 of 30 green in 0.2 s |
| NUnit and MSTest tests | `testsnew/MUnit.Tests.NUnit`, `.MSTest` | 2 and 2 green (MSTest in parallel by method) |
| Brinell head | `srcnew/Brinell.Maui.Munit` | `MunitMauiDriver`, `MunitMauiElement`, `MunitHead.AsBrinellContext()`, `DeviceOnlyException` |
| Head's tests | `testsnew/Brinell.Maui.Munit.Tests` | 3 of 3 green: the sample app's XAML Buttons and Toggle pages, driven by the device suite's own page objects, linked unchanged |
| Proposal and design | `.my/mUnit/proposal.md`, `design.md` | Written |

The work is committed on `move` as 35a78463 and not pushed: Brinell's `move` stays local until a
push is asked for. Tomorrow's submodule pointer has not been moved to it, because another session is
committing Brinell work at the same time.

## Next

Run every page object of the sample suite on MUnit and count what passes, what is device-only and
what breaks, as the bUnit head's step 1 did. Then the head choice goes in the fixtures.

## Log

- **Spike.** Plain `net10.0` xUnit can make MAUI controls and run bindings, `NavigationPage`, modals and Shell with no handler. Findings in [findings.md](findings.md).
- **Host decided.** Each render goes into a `Window` that is opened in an in-memory `Application` (internal `AddWindow`). That is enough for MAUI to run its own page lifecycle.
- **Dispatcher decided.** One queue per context, drained at every MUnit call, with a virtual clock for timers and delayed work. Async continuations post back to it, so results are deterministic.
- **Core written.** Hosting, rendering, the tree (`VisualNode`), queries, interactions, assertions and the tree snapshot formatter.
- **XAML confirmed.** A MAUI library with `UseMaui` on plain `net10.0` compiles XAML, so tests can render an app's real XAML pages.
- **`Application.Current`.** `Shell.Current` reads it, so while a context's work runs it is set to that context's app, under one process-wide lock. This is on by default and can be turned off with `SetApplicationCurrent = false`, which lets contexts run fully in parallel.
- **xUnit suite green (30).** Fixes found by it:
  - Shell makes a `ContentTemplate` page only when asked, so MUnit asks.
  - Shell builds that page from the nearest handler's services, so a Shell root gets a no-op `HeadlessHandler` carrying the context's services. A `NavigationPage` never gets one, because with a handler a push waits for the platform.
  - Rendering an instance now also fixes the services.
- **NUnit and MSTest green.** MUnit's plain-exception assertions fail the test with their message in both frameworks.
- **Brinell head green.** It took no change to Brinell.Maui. The element reports nominal 1x1 bounds when visible, so `PageObjectBase`'s usable-bounds rule holds.
