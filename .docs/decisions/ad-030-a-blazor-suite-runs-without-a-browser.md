# AD-030: A Blazor Suite Runs Without A Browser, And The Browser Judges The Gate

`Bruinet.Html.Bunit` is a second implementation of Html's interfaces (`IHtmlDriver`, `IHtmlElement`,
`IHtmlTestContext`, AD-010) over bUnit: the app's components rendered in the test process, its own
services, its router. Page objects, controls, collections, the call model and the feature files run
on it unchanged; only the fixture chooses. A run's head is `BRUINET_HEAD` (`Bruinet.Core.Testing.WebHeads`:
`browser`, the default, or `bunit`), read by the fixtures, `BrowserFactAttribute` and the scenario
runner alike. The browser head runs every gate.

What the bUnit head cannot answer (layout, style sheets, script, frames, pixels) throws
`BrowserOnlyException`, which carries `IEndsCall`, so the call ends at once instead of polling to its
budget; `Poller.IsFatal` knows the marker, not the driver's type. A test that needs a browser says
`[BrowserFact("why")]`, a scenario `@browser`: skipped on the bUnit head with the reason. A handler
that throws is `AppUnavailableException`, as a browser's circuit ends, so a click is never retried.

**Why:** the browser is most of a UI test's cost. Bruinet's Blazor suite ran 35 tests in 2 s on bUnit
and 39 s in a browser; a call costs about 0.2 ms against about 40 ms ([the bUnit plan](https://github.com/exact-mobile-apps/test-tooling/blob/37f180a7/.my/bunit/plan.md), steps 1 to 5).
Html already kept its driver behind its own interfaces, so a second driver costs one project and no
change to a page object. Tomorrow's pyramid paper found about half of its UI scenarios check a rule's
value or a page's state, which bUnit can show without a browser.

**Ruled out:**
- bUnit tests written against bUnit's own API (`cut.Find`, CSS selectors): a second set of locators
  beside the page objects, and no feature file runs on them;
- view models for every page instead: the pages' logic moves out of the markup, but the scenarios and
  page objects still need a browser to run at all;
- a head per test project: one project per head doubles the suites; one variable keeps one project and
  lets a run choose;
- making the bUnit head the gate: it passes a page a style sheet hides.

**The project earns its place by AD-026's R3:** bUnit, and with it AngleSharp and the components'
test renderer, stays out of every suite that only drives a browser.

**Broken when:** a page object, control or phrase needs a change to run on bUnit; `Bruinet.Html` or
`Bruinet.Blazor` references bUnit; a browser-only member waits out its budget on bUnit instead of ending
the call; or a gate passes on the bUnit head alone.
