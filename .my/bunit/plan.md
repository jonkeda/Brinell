# bUnit head: development plan

Agent document. Builds [proposal.md](proposal.md). Status: steps 1 (the spike), 2 (the driver) and 3 (the context) done; step 4 next. Each step ends
green: `dotnet build Brinell.sln -p:NuGetAudit=false`, its own unit tests, and the Blazor suite on
every head the step touches.

## Fixed points

| What | Value |
| --- | --- |
| Contracts implemented | `Brinell.Html.Interfaces`: `IHtmlDriver`, `IHtmlElement`, `IHtmlTestContext` (with `IHtmlElementScope`, `ITestContext`), `IHtmlPageProbe` |
| Reference implementation | `srcnew/Brinell.Html.Playwright` (`PlaywrightHtmlDriver`, `PlaywrightHtmlElement`, `PlaywrightTestContext`, 1,019 lines) |
| Not changed | Page objects, controls, collections, `Brinell.Core.Calls` (`Poller`, `ControlCall`, `Confirmer`), UAT phrases, feature files |
| Package | `bunit`, newest 2.x targeting `net10.0`, in `srcnew/Directory.Packages.props` (and the root one if a root project needs it); not in the NuGet cache yet |
| Target framework | `net10.0` (SDK 10.0.401, `global.json`) |
| Test runner | xUnit 2.9.3, as every suite |
| First app | `samples/Brinell.Samples.Blazor.App` (13 pages, `Routes.razor`, services `IAuthService`, `DataService`, `MediaService`, `ToastService`) |
| First suite | `testsnew/Brinell.Blazor.UITests`: `Tests/Controls` (ButtonClick, ControlState, TextInput), `Tests/Pages` (Counter, UserForm), `Features/counter-basic`, `login-invalid-credentials`; context made in `TestBase/BlazorSampleTestBase.InitializeAsync` |

## New and changed projects

| Project | Kind | Holds |
| --- | --- | --- |
| `srcnew/Brinell.Html.Bunit` | new library | `BunitHtmlDriver`, `BunitHtmlElement`, `BunitTestContext`, `BunitTestContextOptions`, `BunitLocators`, `BunitHistory`, `BrowserOnlyException` |
| `testsnew/Brinell.Html.Bunit.Tests` | new tests | Per-member tests over small test components in the project (`Probe.razor` and friends) |
| `srcnew/Brinell.Html` | changed | `Testing/HtmlHead.cs` (enum `Browser`, `Bunit`), `Testing/HtmlHeads.cs` (the current head: `BRINELL_HEAD`, else `brinell.html.config.json` `head`, else `Browser`) |
| `testsnew/Brinell.Blazor.UITests` | changed | `BlazorSampleTestBase` and `Uat/BlazorUatFixture` make the context by head; references `Brinell.Html.Bunit` and the sample app project |
| `srcnew/Brinell.Bdd` or `Brinell.Uat` | changed | The `@browser` tag: skipped on a non-browser head, with the reason |

`Brinell.Html` and `Brinell.Blazor` take no bUnit reference; only `Brinell.Html.Bunit` does.

## Step 1: spike (time box: one session)

Minimal `BunitHtmlDriver`, `BunitHtmlElement`, `BunitTestContext` in `Brinell.Html.Bunit`: find by
test id, id and CSS; `Click`, `Fill`, `Text`, `Enabled`, `GetAttribute`; navigation by rendering one
page component. `BlazorSampleTestBase` picks the head from `BRINELL_HEAD`.

Answer, with a test each in `Brinell.Html.Bunit.Tests`:

| Question | Test |
| --- | --- |
| Does a handler's `await` finish while Brinell polls on the test thread? | `Click_HandlerAwaitsDelay_PollSeesTheNewText` (handler awaits `Task.Delay(200)`, then sets text) |
| Does an element found before a render go stale, refresh, or throw? | `Find_ThenRerender_ElementReadsTheNewText` |
| Do the sample's `@rendermode` attributes render under bUnit? | `Render_SampleCounterPage_HasItsButton` |
| What does one Brinell call cost on bUnit? | Median of 100 `Click` + `AssertText` calls, written to the test output |

Run `Tests/Controls` and `Tests/Pages` with `BRINELL_HEAD=bunit`. Record in this file, under
"Step 1 results": passed, failed, browser-only, and the four answers.

**Exit:** go if the async question has an answer that keeps `Poller` unchanged (the proposal's
condition); otherwise write the alternative (a pump of bUnit's dispatcher inside
`BunitTestContext.ProbeReadiness`) and ask the owner.

## Step 2: the driver in full

Every `IHtmlElement` member, each with a test in `Brinell.Html.Bunit.Tests`:

| Member | Implementation | Test name pattern |
| --- | --- | --- |
| `TryFindElement`, `FindElements` | `BunitLocators.ToCss`: AutomationId as the context's test-id attribute, Id, Name, ClassName, Css, TagName; Text, LinkText, PartialLinkText as `FindAll("*")` filtered by normalized `TextContent`; XPath throws `BrowserOnlyException` | `Find_By<Kind>_…` |
| `Click`, `DoubleClick`, `Submit`, `RightClick` | bUnit event dispatch (`Click`, `DoubleClick`, `Submit`, `ContextMenu`) | `Click_…` |
| `Fill`, `SendKeys`, `Clear` | `Input(value)` then `Change(value)`; `TextInputMethod.Keys` adds `KeyDown`/`KeyUp` per character | `Fill_…` |
| `Check`, `Uncheck`, `SelectOption(string)`, `SelectOption(string[])` | `Change(true/false)`, `Change(value)`, `Change(values)` | `Check_…`, `Select_…` |
| `PressKey`, `Focus`, `Blur` | `KeyDown`/`KeyUp` with the key name, `Focus`, `Blur` | `Key_…` |
| `Visible` | No `hidden` attribute, no inline `display:none`/`visibility:hidden`, the same for every ancestor | `Visible_…` |
| `Enabled`, `Selected`, `IsChecked`, `InputValue` | Attributes and bound values from the DOM | `State_…` |
| `Text`, `InnerHtml`, `OuterHtml`, `TagName`, `GetAttribute`, `GetDomAttribute`, `GetDomProperty` | AngleSharp | `Read_…` |
| `InstanceKey` | A path from the root by element index, stable across renders | `Key_SameElementAfterRender_SameKey` |
| `Location`, `Size`, `Rect`, `GetCssValue`, `ClickAt`, `Hover`, `LongPress`, `Swipe`, `ScrollIntoView`, `Evaluate`, `Evaluate<T>`, `TryGetFrameDocument` | `BrowserOnlyException(member, scope)`; `ScrollIntoView` is a no-op | `BrowserOnly_<Member>_Throws` |

`BrowserOnlyException` derives from the exception Brinell treats as a configuration error, so a call
ends at once instead of polling to its budget (check the type in `Brinell.Core.Calls` when building).

## Step 3: the context

`BunitTestContext : IHtmlTestContext, IHtmlPageProbe, IScreenshotSource`, made from
`BunitTestContextOptions`:

| Option | Meaning |
| --- | --- |
| `RootComponent` | The component rendered as the app, e.g. the sample's `Routes` |
| `ConfigureServices` | `Action<IServiceCollection>`: the app's services, registered by the suite |
| `User` | Null for signed out, else the name and roles given to bUnit's test authorization |
| `JsInterop` | `Loose` (default) or `Strict`; planned answers through `ConfigureJs` |
| `BaseUrl` | The address the fake `NavigationManager` reports, so page objects' paths resolve |
| `Timeouts`, `Logger`, `NearMiss`, `Name` | As `HtmlTestContextOptions` |
| `Roles` | The signed-in user's roles |

| Member | Implementation |
| --- | --- |
| `NavigateTo`, `NavigateBack`, `GoForward`, `Refresh` | bUnit's `NavigationManager.NavigateTo`; `BunitHistory` keeps back and forward; `Refresh` renders the root again |
| `CurrentUrl`, `PageTitle` | The navigation manager's URI; the `PageTitle` component's text, else empty |
| `ProbeReadiness`, `WaitReady`, `IsIdle`, `ProbePage` | The renderer has no pending render and the body has no busy attribute (`BlazorPageObjectBase`'s `data-busy`) |
| `TakeScreenshot`, `SaveScreenshot` | The rendered markup as UTF-8 `.html` bytes; the path's extension changed to `.html` |
| `ResetAppState` | Dispose the bUnit context and make a new one from the same options |
| `RecordingTarget` | Null: nothing records on this head |

Tests in `Brinell.Html.Bunit.Tests`: `Navigate_ToRoute_RendersThePage`, `Back_AfterTwoPages_ShowsTheFirst`,
`SignedOut_AuthorizedPage_ShowsNotAuthorized`, `Busy_WhileHandlerRuns_NotReady`,
`Screenshot_WritesMarkup`, `Reset_StartsFromTheRoot`.

## Step 4: heads in the fixtures and scenarios

- `HtmlHeads.Current` in `Brinell.Html.Testing`; `HtmlTestFixtureBase` gets
  `protected Task<IHtmlTestContext> CreateContextAsync()` that builds Playwright or bUnit by head;
  bUnit needs the suite's `BunitTestContextOptions`, given by an overridable `BunitOptions()` that
  throws "this suite has no bUnit head" by default.
- `BlazorSampleTestBase` and `BlazorUatFixture` move to `CreateContextAsync`; their `BunitOptions()`
  renders the sample's `Routes` with the sample's four services.
- `@browser` on a scenario, `[Trait("Head", "Browser")]` on a test: skipped on the bUnit head with
  "needs a browser"; find where scenario tags are read (`Brinell.Bdd/BddScenarioSource.cs` first)
  and add the check there; for xUnit tests a `BrowserFact` attribute beside `WindowsOnlyFact`.
- The run's results name the head: the UAT `__result.json` gets `head`, the call log a `Head` column.

## Step 5: Brinell's suites on both heads

- Run `Brinell.Blazor.UITests` and `Brinell.Html.UITests` (where its sample is Blazor) on both heads;
  every test that is browser-only gets `@browser` or `BrowserFact`, with the member that needs it in a
  comment.
- Table under "Step 5 results": per suite, tests per head, passed, skipped as browser-only, wall time.
- `docs/guides/bunit.md`: what the head is, how to give a suite one (`BunitOptions`), `@browser`, the
  fidelity rules (visible without style sheets, no layout, no JS), and that the browser head is the gate.
- An AD in `.docs/` beside AD-010: a second Html driver over bUnit; browser-only members fail at once;
  the browser head judges every gate.

## Step 6: Tomorrow's trial (Tomorrow's work, the owner schedules it)

Not part of this plan's build; listed so the shape is known. In Tomorrow, a `BunitOptions()` for the
studio suite: the studio's service composition as `TomorrowApp` and `InProcessArrangement` build it
(test database, the fake Foundry, Studio.Core), the admin user through test authorization; the
fights bench (PLT.35, 16 scenarios) run on both heads and timed.

## Open questions, with proposals

| Question | Proposal | Why |
| --- | --- | --- |
| One context type for both heads, or two? | Two (`PlaywrightTestContext`, `BunitTestContext`), chosen by the fixture | Page objects only see `IHtmlTestContext`; a merged type would carry Playwright into bUnit runs |
| Head by environment variable or by test project? | Variable, with the config file as default | One project, both heads; the selection papers want the cheap head in the inner loop |
| Does `Fill` send key events by default? | No: `Input` and `Change` only | Matches what a bound `@bind` sees; per-key only when a test asks for `Keys` |
| What does `Visible` say about CSS-class hiding? | Visible | No style sheets; the gate's browser run catches the rest, and `@browser` marks known cases |
| Record videos on the bUnit head? | Never | No pixels; narrated recordings stay on the browser head |

## Step 1 results

2026-10-25, bUnit 2.12.5, one run each on a busy machine. **Go:** `Poller` stays unchanged.

| Run | Tests | Result | Time |
| --- | --- | --- | --- |
| `Brinell.Html.Bunit.Tests` (the spike's own) | 5 | 5 passed | 1.4 s |
| `Brinell.Blazor.UITests` `Tests/Controls` + `Tests/Pages`, `BRINELL_HEAD=bunit` | 33 | 33 passed, none browser-only | 0.99 s (wall 2 s) |
| The same 33 on the browser head | 33 | 33 passed | 38 s (wall 40 s) |
| The whole Blazor suite on the browser head, after the change | 35 | 35 passed | 36 s |

| Question | Answer |
| --- | --- |
| Does a handler's `await` finish while Brinell polls? | Yes. `Click` returned after 10 ms; `AssertText("Done")` passed at 228 ms, after the handler's 200 ms delay. bUnit's sync `Click` returns at the handler's first await, as a browser click does, and the render after it reaches the DOM while the poll runs. |
| Does an element found before a render go stale? | bUnit's own node, held across a render, read the new text but was not the object a fresh `Find` returned. The driver finds again on every read (`BunitHtmlElement` holds a resolver, not a node), so it reads the new text either way. |
| Do the sample's `@rendermode` attributes render? | Yes: the sample's `Routes` rendered `/counter` through its router and layout (3,025 characters of markup); every page the 33 tests open rendered. |
| What does one call cost? | `Click` + `AssertText`: median 0.23 ms, slowest 2.13 ms, over 100. The browser head's median call is 38 ms (`.my/reviews/baseline.md`, C2), so about 150 times less. |

What the spike built: `srcnew/Brinell.Html.Bunit` (`BunitTestContext`, `BunitHtmlDriver`, `BunitHtmlElement`,
`BunitLocators`, `BunitTestContextOptions`, `BrowserOnlyException`), `testsnew/Brinell.Html.Bunit.Tests`
(`SpikeTests`, the `Probe` component, `ProbePage`), and in `Brinell.Blazor.UITests` the head switch in
`TestBase/BlazorSampleTestBase` with `TestBase/BlazorSampleBunit` (the sample's `Routes` and four services);
the suite now references the sample app's assembly. Most of step 2's member table is already in
`BunitHtmlElement`; its tests are not.

Found, for the next steps:

- **`BrowserOnlyException` is not fatal yet.** `Poller.IsFatal` knows `AppUnavailableException`,
  `RouteUnavailableException` and configuration errors only, so a browser-only member polls to its budget
  before failing. Step 2: make it fatal in `Brinell.Core.Calls` (a marker interface rather than a bUnit type).
- **A handler that throws before its first await throws out of `Click`,** where a browser would not see
  it; the poll could then click twice. Step 2: catch bUnit's handler exceptions in `Click` and surface them
  as the app's error, not a retry.
- **Links and submit buttons** are followed by `Click` itself (no Blazor script here): an `href` navigates
  through bUnit's `NavigationManager`, a submit button without its own handler submits its form.
- **`PageTitle` is empty:** the title renders into `App.razor`'s head, which the root does not include.
- **The UAT scenarios did not run on bUnit:** `Uat/BlazorUatFixture` still makes a browser; step 4.
- **The two `Directory.Packages.props`** (root, `srcnew`) both carry `bunit`; `Brinell.Html.Bunit`
  targets `net10.0` only.

## Step 2 results

2026-10-25, one run each.

| Run | Result |
| --- | --- |
| `Brinell.Html.Bunit.Tests` | 54 of 54 in 0.54 s: `MemberTests` (49, a test per member or pattern of the step 2 table) and `SpikeTests` (5) |
| `Brinell.Blazor.UITests` controls and pages, `BRINELL_HEAD=bunit` | 33 of 33 in 0.43 s |
| `Brinell.Blazor.UITests`, the browser head, whole suite | 35 of 35 in 44 s |
| Unit suites over the changed `Poller` | Core 51, Html 74, Blazor 173, MAUI 210 (1 skipped), native Android 69: all green |
| `dotnet build Brinell.sln -p:NuGetAudit=false` | Every project compiled (7 m 32 s); the only errors were copies into `tools/Brinell.Cli/bin`, locked by nine running `brinell` MCP server processes, left running |

What changed:

- **Browser-only ends the call at once.** `Brinell.Core.Exceptions.IEndsCall`, a marker a driver puts on
  its own errors; `Poller.IsFatal` (and so `Confirmer`) treats it as it treats `AppUnavailableException`.
  `BrowserOnlyException` carries it. `BrowserOnly_InAControlCall_EndsTheCallAtOnce`: a `Hover` with a 3 s
  budget fails under 1 s.
- **A handler that throws is the app failing.** Every event goes through `BunitHtmlElement.Raise`: only
  where the element listens (`blazor:on…`), bUnit's exception turned into `AppUnavailableException`
  ("the page's onclick threw …; in a browser the circuit ends here"), so the call ends instead of
  clicking again. A throw after the handler's first await reaches bUnit's renderer
  (`Renderer.UnhandledException`); `BunitTestContext.ThrowIfAppFailed` reports it on the next lookup or
  readiness probe.
- **A disabled control gets no click,** nor a check, as in a browser.
- **`SendKeys` with `TextInputMethod.Keys`** raises `onkeydown`/`onkeyup` per character, then sets the value
  as `Fill`; `PressKey("Enter")` in a form field submits the form.
- **Locators:** test id, id, name, class, CSS, tag, automation id, text (innermost, any case), link text,
  scoped to a parent; XPath and the accessible name throw `LocatorNotSupportedException`.

Found, for the next steps:

- **A component without a router does not re-render on navigation:** `Click` on a link changes
  `CurrentUrl`, but only a `Routes` root shows the new page. Suites render their `Routes` (step 3).
- **A button with its own `onclick` inside a form** raises only the click; a browser would submit the
  form after it unless the button is `type="button"`. Not met in the sample; left for step 3's tests.
- **`Read_AfterAReset_FindsTheElementAgain`:** an element found before `ResetAppState` is found again
  in the new render, as a Playwright locator is. A truly gone element is `StaleElementException`.

## Step 3 results

2026-10-25, one run each.

| Run | Result |
| --- | --- |
| `Brinell.Html.Bunit.Tests` | 77 of 77 in 0.72 s: `ContextTests` (21), `MemberTests` (51), `SpikeTests` (5) |
| `Brinell.Blazor.UITests` controls and pages, `BRINELL_HEAD=bunit` | 33 of 33 in 0.31 s |
| `Brinell.Blazor.UITests`, the browser head, whole suite | 35 of 35 in 37 s |

What changed in `BunitTestContext` and its options:

| Part | Now |
| --- | --- |
| Navigation | `NavigateTo`, `NavigateBack`, `GoForward` through the root's router with a history of its own; `Refresh` renders the root again at the same address; a link in the layout navigates. Tests: `Navigate_*`, `Back_*`, `Forward_AfterBack_ShowsTheSecond`, `Refresh_RendersThePageAgain_FromItsStart` |
| `PageTitle` | The last `PageTitle` the page rendered, its content rendered on its own (`Counter - Brinell Blazor Sample`); empty without one |
| Readiness | `ProbePage` reads the page's own busy signal: the last `brinellBusy.set` call its layout made, recorded by bUnit's JS interop (planned in every mode). A root without the script is interactive from its first render. `BlazorPageObjectBase`'s required busy signal works unchanged; `Busy_FollowsTheLayoutsBusyScript` sees busy while a handler runs and idle after |
| Sign-in | bUnit's test authorization is always registered: signed out by default, `User` and `Roles` sign in. `AuthorizeView` and roles tested (`SignedOut_*`, `SignedIn_*`) |
| Scripts | `JsInterop`: `Loose` (default) or `Strict`; `ConfigureJs` plans answers with bUnit's `Setup`/`SetupVoid`. Strict with an unplanned call fails the render, naming the call |
| Screenshot | The markup as `.html`, beside the path asked for (`Screenshot_WritesMarkup`) |
| Reset | `ResetAppState` renders a fresh root at `BaseUrl` with an empty history (`Reset_StartsFromTheRoot`) |
| Services | `Services`: the app's own provider, for a test that arranges through it |

Found, for the next steps:

- **bUnit submits the form itself** after a submit button's own click handler (and not for `type="button"`);
  the driver submits only where the button has no handler, so a form is submitted once. Tests:
  `Click_ButtonWithItsOwnHandlerInAForm_AlsoSubmits`, `Click_TypeButtonInAForm_DoesNotSubmit`.
- **Test authorization replaces the app's own:** it is registered after `ConfigureServices`, so a suite's
  `AuthenticationStateProvider` (Tomorrow's Identity one) does not reach the bUnit head. That is the
  proposal's intent (arrange the account, don't sign in); step 6 relies on it.
- **No `TestIdAttribute` option:** Brinell's `DataTestId` strategy is `data-testid` on both heads, so there is
  nothing to set; dropped from the options table.
- **`RecordingTarget`:** the context is not `IRecordable`, so nothing records on this head, as planned.

## Step 5 results

Not run yet.
