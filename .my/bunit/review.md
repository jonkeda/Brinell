# Review: the bUnit head (2026-10-25)

A review of `Bruinell.Html.Bunit` (about 1,100 lines), its guide, AD-030, and how Tomorrow's studio suite uses it.
It reads the code against what bUnit 2.12.5 offers and what a browser does.

## Outcome

The head is sound and earns its place. The interfaces held: no page object or phrase changed, browser-only members
end the call at once, and 92 unit tests cover the driver. The studio suite runs in about 50 s instead of 2 m 40 s.

What is missing falls in three groups: a few click and form behaviours that differ from a browser, bUnit features
that would turn skipped scenarios into run ones, and nothing that watches the two heads drift apart.

## Behaviours that differ from a browser

| Where | What happens | Proposal |
| --- | --- | --- |
| `BunitHtmlElement.SetChecked` | Fixed: `Check` on a radio sent `true`; it now sends the radio's `value`, "on" without one (`Check_Radio_*`, `Check_InputRadio_BindsItsValue`) | Done |
| `BunitHtmlElement.Click` | Fixed: a link with its own `@onclick` is now followed after the handler, unless a `preventDefault` on the way stops it (`Click_Link*`). A click bubbling to every handler until `stopPropagation`: bUnit already does it, so that finding was wrong; two tests keep it so | Done |
| `BunitTestContext` navigation | Fixed: the app's `forceLoad` is a page load; an address no `@page` serves ends the call with `BrowserOnlyException` (`ForceLoad_*`) | Done |
| `BunitTestContext._history` | Fixed: a `replace: true` navigation replaces the last entry, read from bUnit's history (`HistoryBack_AfterAReplace_*`) | Done |
| `BunitHtmlElement.Visible`, `Enabled` | Fixed: the four rules, and a click on a fold's summary opens it, which the studio's PLT.11.5 needed | Done |
| `FallbackServices` | Fixed: such a service fails at once, named, with what to do (`CircuitServiceGuard`) | Done |
| `ThrowIfAppFailed` | It reads only the renderer's latest unhandled exception; a stale-handler refusal after a real failure may hide it | Check bUnit's `UnhandledException` behaviour; keep the first real failure in the context |

## bUnit features not used

- **Component stubs** (`ComponentFactories.AddStub<T>()`): the canvas and WebGL viewers could be stubbed, so a
  scenario runs everything around them instead of being skipped whole. Most of the studio's 18 `@browser`
  scenarios skip for one viewer.
- **Script verification** (`JSInterop.Invocations`, `VerifyInvoke`): "Ruben Smit greets you aloud" could check the
  audio call was made. A Bruinell probe, "the page called script X", would serve page objects on bUnit.
- **Strict mode as a report**: one Strict run lists each page's script calls. Loose mode answers them with
  defaults, which can make a page show a state the browser never would.
- **Fake time** (`FakeTimeProvider`, where the app takes a `TimeProvider`): the sample's login delay and the Html
  suite's 8 s are mostly waits. This is an app change, so optional.

## Nothing watches the heads drift

The gate's browser run is the only check that a scenario passing on bUnit really works. Nothing tells when an
`@browser` tag is no longer needed, either. Two small additions would cover both:

- a run mode that runs the `@browser` scenarios on bUnit and lists the ones that pass;
- a comparison of the two heads' `__result.json` per scenario after a gate, writing each fidelity miss down.

Scenario tags also carry no reason, where `[UiFact(Browser = "why")]` does; the why sits in the feature's prose,
if at all. `@browser` with a reason (for example a line under the tag) would put it in the skip message.
PLT.18.1 (the jobs page) has no visible browser need and looks like a driver gap worth a look.

## Smaller points

- Fixed: the guide's `Fill` bullet moved to "What Behaves Differently".
- `bunit` is pinned in both `Directory.Packages.props` files, so they can drift; one place would do.
- `Settle` waits up to 100 ms per event whose handler awaits work before rendering; a suite with many such clicks
  pays it each time.
- The accessible-name locator throws; aria-label, `<label for>` and a button's text would cover most role lookups.
- The plan still names `BunitHistory` and a configuration-error base class; its results say what was built instead.
- Wall time in Tomorrow is mostly the in-process start (migrations, the geo import); the warm host stays the
  larger win, as Step 6 found.
- Not covered yet: Todo's web UAT suite and, by the owner's choice, Tomorrow's game suite.

## Next

The driver gaps are fixed but one (`ThrowIfAppFailed`). Next, try
stubs on one viewer scenario (PLT.21.3) to see how many skipped scenarios come back.
