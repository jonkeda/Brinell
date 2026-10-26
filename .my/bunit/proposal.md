# A bUnit head for Bruinet: the same page objects and scenarios, without a browser

A proposal, 2026-10-25. bUnit renders a Blazor component inside a unit test: no browser, no
server, milliseconds per test. Bruinet's page objects, controls and BDD scenarios today need a
browser through Playwright. This proposes a second Html driver over bUnit, so the same page
objects and the same feature files run on either, and asks what that would take.

## Outcome

It fits Bruinet's shape well. `Bruinet.Html` already keeps the driver behind its own interfaces
(`IHtmlDriver`, `IHtmlElement`, `IHtmlTestContext`), and `Bruinet.Html.Playwright` is one
implementation of about 1,000 lines. Page objects take an `IHtmlTestContext` and nothing else
(Tomorrow's `AboutAiPage(IHtmlTestContext context)`). So a `Bruinet.Html.Bunit` driver of similar
size would run every page object, control, collection and UAT phrase unchanged; only the fixture
chooses the head.

For Tomorrow this pushes tests down without changing its architecture. The scenarios that check
a rule's value or a page's state (about half, from the pyramid paper's sample) would run in
milliseconds during development and in the browser at the gate. Screen models stay worth having
for pages with heavy logic, but they are no longer the price of a faster suite.

## How the driver maps onto bUnit

bUnit renders into an AngleSharp DOM and fires Blazor's own event handlers. Each member of
`IHtmlElement` either has a direct counterpart or is browser-only:

| Bruinet member | On bUnit | Fidelity |
| --- | --- | --- |
| `FindElements` by test id, id, CSS, class, tag, text | `FindAll(css)`; text as a filter on `TextContent` | Same; XPath not supported |
| `Click`, `DoubleClick`, `Submit` | bUnit's `Click()`, `DoubleClick()`, `Submit()` events | Handler runs; no hit-testing |
| `Fill`, `SendKeys`, `Clear` | `Input()` then `Change()` | Bound value set; no per-key events unless asked |
| `Check`, `Uncheck`, `SelectOption` | `Change(bool)`, `Change(value)` | Same |
| `PressKey`, `Focus`, `Blur` | `KeyDown`/`KeyUp`, `Focus()`, `Blur()` events | No real focus |
| `Text`, `InnerHtml`, attributes, `IsChecked`, `InputValue` | From the DOM | Same |
| `Enabled` | No `disabled` attribute | Same |
| `Visible` | Not `hidden`, not `display:none` inline, no ancestor hidden | Approximate: no style sheets |
| `Location`, `Size`, `Rect`, `GetCssValue`, `Hover`, `Swipe`, `ClickAt`, `Evaluate` | none | Browser-only |
| `TakeScreenshot` | The rendered markup saved as `.html` | Not a picture |

A browser-only member throws a `BrowserOnlyException` naming the member and the page object, so
a test that needs the browser says so at once rather than passing wrongly.

## The context and the host

`BunitTestContext : IHtmlTestContext` holds one bUnit test context and renders the app's own
`Routes` component, so pages are reached by their address as in the browser:

- **Navigation.** `NavigateTo(url)` goes through bUnit's fake `NavigationManager`; the app's router
  renders the page with its layout. `GoBack` keeps a history of its own.
- **Services.** The fixture registers the app's real composition (use cases, stores against the
  test database, the fake Foundry), as the in-process host does today, minus what needs HTTP.
- **Sign-in.** bUnit's test authorization stands in for the cookie. Identity's pages are Razor
  Pages, not components, so "I sign in" is browser-only and the bUnit head arranges the account
  instead (Tomorrow's `Put*` arrangement already does this in process).
- **JavaScript.** bUnit's JS interop in loose mode, with planned answers per module where a page
  needs one; the board's canvas stays browser-only.
- **Readiness.** Bruinet's poll and budgets stay as they are. A page is idle when bUnit's renderer
  has no pending render and the body carries no `data-busy`, so `BlazorPageObjectBase`'s busy signal
  works unchanged.

## Scenarios on two heads

The Todo sample already runs one feature file on several heads (Windows, Android, web). A bUnit
head joins the same way:

- The UAT fixture takes a head (`Browser` or `Bunit`) from configuration or the run's filter.
- A scenario that needs the browser is tagged `@browser`; on the bUnit head it is skipped with
  that reason, never failed.
- During development the bUnit head runs the touched scenarios; at a gate the browser head runs
  them all. This joins the test-selection papers' plan: select on the cheap head, run in full on
  the dear one.
- A scenario that passes on bUnit and fails in the browser is a fidelity miss, recorded at the
  gate like a selection miss; it gets `@browser` or a fix in the driver.

Phrases, page objects and feature files are written once. What doubles is the fixture, not the
tests.

## What stays in the browser

Anything that depends on layout, style sheets, real JavaScript or the connection: Tomorrow's board
canvas and its keys, phone layout, sign-in and lockout, the circuit's pushes from the server,
prerendering, recorded and narrated videos. The browser head also stays the judge at every gate,
because the bUnit head can pass a page that a style sheet hides.

## What Bruinet gets besides Tomorrow

- **Unit tests for Bruinet's own Blazor controls** without starting the sample app: `Bruinet.Blazor.Tests`
  could render the sample's components and drive them through the real control objects.
- **A cheap first check for the Scraper's drafted page objects**: render the page, verify the ids
  resolve, before the browser run.
- **Faster recording drafts**: `run_test` on the bUnit head where a draft needs no browser.

## Risks

- **False passes.** A page hidden by CSS, or a handler bound in JavaScript, works on bUnit and not
  in the browser. The gate's browser run catches it; the `Visible` rule and `@browser` keep it rare.
- **Async.** Bruinet's poll runs on the test thread while bUnit's renderer runs on its dispatcher;
  whether a handler's awaits finish while Bruinet polls needs a spike, not an assumption.
- **The host.** Tomorrow's pages inject use cases built over SQL Server and the fake Foundry; the
  bUnit context has to compose them as the in-process host does, without Kestrel.
- **bUnit's own releases.** Its API was renamed between major versions; Bruinet pins one version in
  `Directory.Packages.props` and wraps it in one project.

## Proposed steps

1. **Spike** on `Bruinet.Blazor.UITests`: a `BunitHtmlDriver` with find, click, fill, text and
   attributes; run the existing suite on it and count what passes, what is browser-only, what
   breaks. Settle the async question here.
2. **The driver in full**: every member mapped or browser-only, navigation through the router,
   readiness, test authorization, JS in loose mode; `Bruinet.Html.Bunit` with its unit tests.
3. **Heads in the fixtures**: `HtmlTestFixtureBase` and the UAT fixture take a head; `@browser`
   honoured; the head in the run's report.
4. **Tomorrow's studio first**: the fights bench (PLT.35, 16 scenarios) on the bUnit head, timed
   against the browser.
5. **A decision record** in `.docs/` (an AD beside AD-010) once the spike has numbers: a second
   Html driver, its fidelity rules, and the browser as the gate.

## What is unknown

- How many of Bruinet's Blazor scenarios and Tomorrow's pass on bUnit unchanged; the spike gives
  the number.
- What a bUnit test costs in Tomorrow with the real use cases and database behind it: likely tens of
  milliseconds rather than one, still a hundredfold under the browser.
- Whether the owner wants scenario videos only from the browser head (likely yes), so the bUnit head
  never records.
