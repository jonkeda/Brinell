# bUnit head: development plan

Agent document. Builds [proposal.md](proposal.md). Status: steps 1 to 6 done; the fights bench runs on both heads in Tomorrow. Follow-ups under "Step 6 results". Each step ends
green: `dotnet build Bruinell.sln -p:NuGetAudit=false`, its own unit tests, and the Blazor suite on
every head the step touches.

## Fixed points

| What | Value |
| --- | --- |
| Contracts implemented | `Bruinell.Html.Interfaces`: `IHtmlDriver`, `IHtmlElement`, `IHtmlTestContext` (with `IHtmlElementScope`, `ITestContext`), `IHtmlPageProbe` |
| Reference implementation | `srcnew/Bruinell.Html.Playwright` (`PlaywrightHtmlDriver`, `PlaywrightHtmlElement`, `PlaywrightTestContext`, 1,019 lines) |
| Not changed | Page objects, controls, collections, `Bruinell.Core.Calls` (`Poller`, `ControlCall`, `Confirmer`), UAT phrases, feature files |
| Package | `bunit`, newest 2.x targeting `net10.0`, in `srcnew/Directory.Packages.props` (and the root one if a root project needs it); not in the NuGet cache yet |
| Target framework | `net10.0` (SDK 10.0.401, `global.json`) |
| Test runner | xUnit 2.9.3, as every suite |
| First app | `samples/Bruinell.Samples.Blazor.App` (13 pages, `Routes.razor`, services `IAuthService`, `DataService`, `MediaService`, `ToastService`) |
| First suite | `testsnew/Bruinell.Blazor.UITests`: `Tests/Controls` (ButtonClick, ControlState, TextInput), `Tests/Pages` (Counter, UserForm), `Features/counter-basic`, `login-invalid-credentials`; context made in `TestBase/BlazorSampleTestBase.InitializeAsync` |

## New and changed projects

| Project | Kind | Holds |
| --- | --- | --- |
| `srcnew/Bruinell.Html.Bunit` | new library | `BunitHtmlDriver`, `BunitHtmlElement`, `BunitTestContext`, `BunitTestContextOptions`, `BunitLocators`, `BunitHistory`, `BrowserOnlyException` |
| `testsnew/Bruinell.Html.Bunit.Tests` | new tests | Per-member tests over small test components in the project (`Probe.razor` and friends) |
| `srcnew/Bruinell.Html` | changed | `Testing/HtmlHead.cs` (enum `Browser`, `Bunit`), `Testing/HtmlHeads.cs` (the current head: `BRUINELL_HEAD`, else `bruinell.html.config.json` `head`, else `Browser`) |
| `testsnew/Bruinell.Blazor.UITests` | changed | `BlazorSampleTestBase` and `Uat/BlazorUatFixture` make the context by head; references `Bruinell.Html.Bunit` and the sample app project |
| `srcnew/Bruinell.Bdd` or `Bruinell.Uat` | changed | The `@browser` tag: skipped on a non-browser head, with the reason |

`Bruinell.Html` and `Bruinell.Blazor` take no bUnit reference; only `Bruinell.Html.Bunit` does.

## Step 1: spike (time box: one session)

Minimal `BunitHtmlDriver`, `BunitHtmlElement`, `BunitTestContext` in `Bruinell.Html.Bunit`: find by
test id, id and CSS; `Click`, `Fill`, `Text`, `Enabled`, `GetAttribute`; navigation by rendering one
page component. `BlazorSampleTestBase` picks the head from `BRUINELL_HEAD`.

Answer, with a test each in `Bruinell.Html.Bunit.Tests`:

| Question | Test |
| --- | --- |
| Does a handler's `await` finish while Bruinell polls on the test thread? | `Click_HandlerAwaitsDelay_PollSeesTheNewText` (handler awaits `Task.Delay(200)`, then sets text) |
| Does an element found before a render go stale, refresh, or throw? | `Find_ThenRerender_ElementReadsTheNewText` |
| Do the sample's `@rendermode` attributes render under bUnit? | `Render_SampleCounterPage_HasItsButton` |
| What does one Bruinell call cost on bUnit? | Median of 100 `Click` + `AssertText` calls, written to the test output |

Run `Tests/Controls` and `Tests/Pages` with `BRUINELL_HEAD=bunit`. Record in this file, under
"Step 1 results": passed, failed, browser-only, and the four answers.

**Exit:** go if the async question has an answer that keeps `Poller` unchanged (the proposal's
condition); otherwise write the alternative (a pump of bUnit's dispatcher inside
`BunitTestContext.ProbeReadiness`) and ask the owner.

## Step 2: the driver in full

Every `IHtmlElement` member, each with a test in `Bruinell.Html.Bunit.Tests`:

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

`BrowserOnlyException` derives from the exception Bruinell treats as a configuration error, so a call
ends at once instead of polling to its budget (check the type in `Bruinell.Core.Calls` when building).

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

Tests in `Bruinell.Html.Bunit.Tests`: `Navigate_ToRoute_RendersThePage`, `Back_AfterTwoPages_ShowsTheFirst`,
`SignedOut_AuthorizedPage_ShowsNotAuthorized`, `Busy_WhileHandlerRuns_NotReady`,
`Screenshot_WritesMarkup`, `Reset_StartsFromTheRoot`.

## Step 4: heads in the fixtures and scenarios

- `HtmlHeads.Current` in `Bruinell.Html.Testing`; `HtmlTestFixtureBase` gets
  `protected Task<IHtmlTestContext> CreateContextAsync()` that builds Playwright or bUnit by head;
  bUnit needs the suite's `BunitTestContextOptions`, given by an overridable `BunitOptions()` that
  throws "this suite has no bUnit head" by default.
- `BlazorSampleTestBase` and `BlazorUatFixture` move to `CreateContextAsync`; their `BunitOptions()`
  renders the sample's `Routes` with the sample's four services.
- `@browser` on a scenario, `[Trait("Head", "Browser")]` on a test: skipped on the bUnit head with
  "needs a browser"; find where scenario tags are read (`Bruinell.Bdd/BddScenarioSource.cs` first)
  and add the check there; for xUnit tests a `BrowserFact` attribute beside `WindowsOnlyFact`.
- The run's results name the head: the UAT `__result.json` gets `head`, the call log a `Head` column.

## Step 5: Bruinell's suites on both heads

- Run `Bruinell.Blazor.UITests` and `Bruinell.Html.UITests` (where its sample is Blazor) on both heads;
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
| `Bruinell.Html.Bunit.Tests` (the spike's own) | 5 | 5 passed | 1.4 s |
| `Bruinell.Blazor.UITests` `Tests/Controls` + `Tests/Pages`, `BRUINELL_HEAD=bunit` | 33 | 33 passed, none browser-only | 0.99 s (wall 2 s) |
| The same 33 on the browser head | 33 | 33 passed | 38 s (wall 40 s) |
| The whole Blazor suite on the browser head, after the change | 35 | 35 passed | 36 s |

| Question | Answer |
| --- | --- |
| Does a handler's `await` finish while Bruinell polls? | Yes. `Click` returned after 10 ms; `AssertText("Done")` passed at 228 ms, after the handler's 200 ms delay. bUnit's sync `Click` returns at the handler's first await, as a browser click does, and the render after it reaches the DOM while the poll runs. |
| Does an element found before a render go stale? | bUnit's own node, held across a render, read the new text but was not the object a fresh `Find` returned. The driver finds again on every read (`BunitHtmlElement` holds a resolver, not a node), so it reads the new text either way. |
| Do the sample's `@rendermode` attributes render? | Yes: the sample's `Routes` rendered `/counter` through its router and layout (3,025 characters of markup); every page the 33 tests open rendered. |
| What does one call cost? | `Click` + `AssertText`: median 0.23 ms, slowest 2.13 ms, over 100. The browser head's median call is 38 ms (`.my/reviews/baseline.md`, C2), so about 150 times less. |

What the spike built: `srcnew/Bruinell.Html.Bunit` (`BunitTestContext`, `BunitHtmlDriver`, `BunitHtmlElement`,
`BunitLocators`, `BunitTestContextOptions`, `BrowserOnlyException`), `testsnew/Bruinell.Html.Bunit.Tests`
(`SpikeTests`, the `Probe` component, `ProbePage`), and in `Bruinell.Blazor.UITests` the head switch in
`TestBase/BlazorSampleTestBase` with `TestBase/BlazorSampleBunit` (the sample's `Routes` and four services);
the suite now references the sample app's assembly. Most of step 2's member table is already in
`BunitHtmlElement`; its tests are not.

Found, for the next steps:

- **`BrowserOnlyException` is not fatal yet.** `Poller.IsFatal` knows `AppUnavailableException`,
  `RouteUnavailableException` and configuration errors only, so a browser-only member polls to its budget
  before failing. Step 2: make it fatal in `Bruinell.Core.Calls` (a marker interface rather than a bUnit type).
- **A handler that throws before its first await throws out of `Click`,** where a browser would not see
  it; the poll could then click twice. Step 2: catch bUnit's handler exceptions in `Click` and surface them
  as the app's error, not a retry.
- **Links and submit buttons** are followed by `Click` itself (no Blazor script here): an `href` navigates
  through bUnit's `NavigationManager`, a submit button without its own handler submits its form.
- **`PageTitle` is empty:** the title renders into `App.razor`'s head, which the root does not include.
- **The UAT scenarios did not run on bUnit:** `Uat/BlazorUatFixture` still makes a browser; step 4.
- **The two `Directory.Packages.props`** (root, `srcnew`) both carry `bunit`; `Bruinell.Html.Bunit`
  targets `net10.0` only.

## Step 2 results

2026-10-25, one run each.

| Run | Result |
| --- | --- |
| `Bruinell.Html.Bunit.Tests` | 54 of 54 in 0.54 s: `MemberTests` (49, a test per member or pattern of the step 2 table) and `SpikeTests` (5) |
| `Bruinell.Blazor.UITests` controls and pages, `BRUINELL_HEAD=bunit` | 33 of 33 in 0.43 s |
| `Bruinell.Blazor.UITests`, the browser head, whole suite | 35 of 35 in 44 s |
| Unit suites over the changed `Poller` | Core 51, Html 74, Blazor 173, MAUI 210 (1 skipped), native Android 69: all green |
| `dotnet build Bruinell.sln -p:NuGetAudit=false` | Every project compiled (7 m 32 s); the only errors were copies into `tools/Bruinell.Cli/bin`, locked by nine running `bruinell` MCP server processes, left running |

What changed:

- **Browser-only ends the call at once.** `Bruinell.Core.Exceptions.IEndsCall`, a marker a driver puts on
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
| `Bruinell.Html.Bunit.Tests` | 77 of 77 in 0.72 s: `ContextTests` (21), `MemberTests` (51), `SpikeTests` (5) |
| `Bruinell.Blazor.UITests` controls and pages, `BRUINELL_HEAD=bunit` | 33 of 33 in 0.31 s |
| `Bruinell.Blazor.UITests`, the browser head, whole suite | 35 of 35 in 37 s |

What changed in `BunitTestContext` and its options:

| Part | Now |
| --- | --- |
| Navigation | `NavigateTo`, `NavigateBack`, `GoForward` through the root's router with a history of its own; `Refresh` renders the root again at the same address; a link in the layout navigates. Tests: `Navigate_*`, `Back_*`, `Forward_AfterBack_ShowsTheSecond`, `Refresh_RendersThePageAgain_FromItsStart` |
| `PageTitle` | The last `PageTitle` the page rendered, its content rendered on its own (`Counter - Bruinell Blazor Sample`); empty without one |
| Readiness | `ProbePage` reads the page's own busy signal: the last `bruinellBusy.set` call its layout made, recorded by bUnit's JS interop (planned in every mode). A root without the script is interactive from its first render. `BlazorPageObjectBase`'s required busy signal works unchanged; `Busy_FollowsTheLayoutsBusyScript` sees busy while a handler runs and idle after |
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
- **No `TestIdAttribute` option:** Bruinell's `DataTestId` strategy is `data-testid` on both heads, so there is
  nothing to set; dropped from the options table.
- **`RecordingTarget`:** the context is not `IRecordable`, so nothing records on this head, as planned.

## Step 4 results

2026-10-25, one run each.

| Run | Result |
| --- | --- |
| `Bruinell.Blazor.UITests`, whole suite, `BRUINELL_HEAD=bunit` | 35 of 35 in 2 s, the two UAT scenarios included (the login one 1.6 s, the sample's own delay) |
| `Bruinell.Blazor.UITests`, whole suite, the browser head | 35 of 35 in 38 s |
| `Bruinell.Core.Tests`, `Bruinell.Bdd.Tests`, `Bruinell.Uat.Tests` | 58, 251, 225: all green |
| `Bruinell.Html.Tests`, `Bruinell.Html.Bunit.Tests`, `Bruinell.Blazor.Tests` | 74, 77, 173: all green |
| `Bruinell.Core.Tests` `WebHeads*`, `BRUINELL_HEAD=bunit` | `BrowserFact_RunsOnlyOnTheBrowserHead` skipped: "Browser only: … this run uses the bUnit head (BRUINELL_HEAD=bunit)." |

What changed, and where it differs from the step's text:

- **The head lives in Core, not Html:** `Bruinell.Core.Testing.WebHead` (`Browser`, `Bunit`) and `WebHeads`
  (`Current`, `IsBunit`, `Parse`, `Variable = "BRUINELL_HEAD"`), so the scenario runner (`Bruinell.Bdd`, which does not
  reference Html) and the fixtures read the same thing. **The variable only:** no `bruinell.html.config.json` fallback,
  since Bdd cannot read that file and one source keeps a fixture and a skip in agreement. It is a separate axis from
  a UAT suite's `Head` (maui, web, mixed, service), which says which app's pages a suite uses.
- **`HtmlTestFixtureBase`:** `InitializeAsync` makes `CreateBunitContext()` on the bUnit head, `CreateContextAsync(options)`
  otherwise. `CreateBunitContext()` is virtual and by default says the suite has no bUnit head; it returns a context,
  not `BunitTestContextOptions`, because `Bruinell.Html` does not reference `Bruinell.Html.Bunit`. No suite derives from
  this base today (`BlazorTestFixtureBase` does, unused); the Blazor suite's own `BlazorSampleTestBase` and
  `Uat/BlazorUatFixture` choose by `WebHeads` themselves, the UAT fixture without starting the sample's host.
- **`@browser`:** `BddTagConventions.Browser`; `BddConfig.EvaluateSkip` skips a scenario with it when
  `BRUINELL_HEAD` is `bunit`, read through the method's `getEnvironmentVariable` so it is testable
  (`EvaluateSkip_BrowserTagOnTheBunitHead_Skips`, `…OnTheBrowserHead_Runs`, `EvaluateSkip_UntaggedScenarioOnTheBunitHead_Runs`).
  Neither of the sample's two scenarios needs it.
- **`BrowserFact`:** `Bruinell.Core.Testing.BrowserFactAttribute(reason)`, in Core because Core already references xUnit;
  skipped on the bUnit head with the reason.
- **Results name the head:** every UAT `__result.json` has `"head": "bunit"` or `"browser"` (checked in both runs).
  **The call log keeps its columns:** the bUnit head writes `calls-bunit-….csv` where the browser writes
  `calls-html-….csv`, so the file names the head and readers of the CSV need no change.

Not run: Tomorrow's UI suites, which reference `Bruinell.Uat` (the report's new field) but no changed fixture base;
building them in place would rebuild the studio this checkout runs on 5219.

## Step 5 results

2026-10-25, one run each, the same build, one after the other on a busy machine.

| Suite | Head | Passed | Skipped as browser-only | Duration (wall) |
| --- | --- | --- | --- | --- |
| `Bruinell.Blazor.UITests` | bUnit | 35 | 0 | 2 s (4 s) |
| `Bruinell.Blazor.UITests` | browser | 35 | 0 | 39 s (41 s) |
| `Bruinell.Html.UITests` | bUnit | 25 | 2 | 8 s (9 s) |
| `Bruinell.Html.UITests` | browser | 27 | 0 | 25 s (26 s) |
| `Bruinell.Html.Bunit.Tests` | none | 78 | 0 | 0.67 s |

- **`Bruinell.Html.UITests` got a bUnit head** as the Blazor suite has: `TestBase/BlazorSampleBunit.cs`, the head
  switch in `TestBase/BlazorSampleTestBase.cs` and `Uat/HtmlUatFixture.cs`, references to `Bruinell.Html.Bunit`
  and the sample's assembly.
- **Browser-only, marked `[BrowserFact]` with the reason:** `FramesPageTests.AControlInTheFrame_IsFoundInTheFrame_AndActsThere`
  (an iframe is its own document) and `RecordedInteractionTests.AClick_IsLogged_WithItsButtonsBounds_InTheViewport`
  (a click's bounds need layout). No scenario needed `@browser`.
- **`TryGetFrameDocument` is browser-only now:** it returned null, so a `FrameScope` waited out its 5 s budget as
  "missing root"; it throws `BrowserOnlyException` and ends the call (`BrowserOnly_Member_Throws`).
- **Html's 8 s on bUnit is the sample, not the head:** `LoginFlow_WhenLoginNeverReenables_…` waits out its 5 s
  budget by design (a negative test) and the login tests wait for the sample's own delay, on either head.
- **`ResetAppStateTests` always run in a browser:** they make their own Playwright context to test it, so they ran
  in Chromium on the bUnit head too.
- **The guide** is `docs/guides/bunit.md` (in `docs/README.md`); **the decision** is
  `.docs/decisions/ad-030-a-blazor-suite-runs-without-a-browser.md` (in `.docs/README.md`), earning the project's
  place by AD-026's R3.

Not done here: the Todo sample's web suites (`Todo.Web.UITests`, `Todo.Web.Uat`) have no bUnit head yet; the step
named Bruinell's Blazor and Html suites only.

## Step 6 results

2026-10-25, Tomorrow `game` at df570032 plus the work below, built in a worktree (`../tomorrow-bunit`, branch
`wip/bunit`) while the studio ran on 5219. One machine, busy.

| Run | Result | Scenario time summed (median) | `dotnet test` (wall) |
| --- | --- | --- | --- |
| PLT.35 (16 scenarios), bUnit, three runs | 16 of 16 each | 28 to 32 s (0.9 s) | 28 to 32 s (42 to 47 s) |
| PLT.35, browser, two runs | 16 of 16 each | 56 s (2.6 to 2.7 s) | 36 s (56 s) |
| Tomorrow's game suite, `Gate=Smoke`, browser | 4 of 4 | | 5 s |
| `Tomorrow.Architecture.Tests` | 140 of 140 | | 3 s |
| The whole studio suite, bUnit, once | 119 passed, 44 failed, the serial collection cut at 10 min | | stopped |

- **About three times faster per scenario, not twenty.** The studio's own work dominates: PLT.35.4 runs 100 fights
  and takes 15 s on both heads; without it the scenarios sum to about 14 s on bUnit and 41 s in the browser. The
  in-process start (migrations, the geo import, the area generated) is most of the wall time on either head, so a
  warm host between runs (`BddWarmRunners`, the selection paper) would save more in the inner loop than the head.
- **How Tomorrow got a bUnit head** (studio suite only):
  - `TomorrowApp.StartAsync` builds one settings map; on the bUnit head `UiHost.StartInProcess` hosts the studio
    in this process with `WebApplicationFactory<StudioHost>` (its own `Program`, migrations, workers; Development,
    content root `src/Tomorrow.Studio`), the browser head starts the built app as before.
  - The fixture makes `UiHost.CreateBunitContext(services, baseUrl)`: the studio's `Routes`, `FallbackServices` the
    in-process host's provider, `StudioTrail` registered again in bUnit's own services (it follows navigation).
  - Sign-in: the account is arranged as before, then `UiHost.SignInInProcess` signs it in through the test
    authorization (name and id); `StartTest` starts signed out on no page, since Identity's pages are Razor Pages.
  - The game's `UiHost` says it has no bUnit head; its suite is unchanged in the browser.
  - `Tomorrow.Studio.UITests` references the studio's assembly, `Bruinell.Html.Bunit` and `Microsoft.AspNetCore.Mvc.Testing`.
- **What the driver needed, found by the fights bench** (each with tests in `Bruinell.Html.Bunit.Tests`, 92 now):
  - `FallbackServices`: an app's own provider behind bUnit's, a scope of it per render of the root, as each circuit
    has one, so no scoped state outlives a reset.
  - `SignIn`/`SignOut` while a page is rendered (through bUnit's authentication provider), kept across page loads.
  - The renderer says Interactive Server (`SetRendererInfo`), as the app's circuit.
  - `NavigateTo` is a page load, as Playwright's `GoTo`: a fresh root and scope, still signed in; a link clicked in
    the page stays in its circuit.
  - A click bubbles to the nearest element that handles it (a marker inside a grid button).
  - A value the user entered stays until a render changes that attribute, as Blazor's diff leaves the DOM, and only
    until the next action (an element is known by its place, which cannot tell a field rendered anew).
  - Events are raised asynchronously (waited up to 100 ms): an event on a handler a newer render dropped is a stale
    element, retried by the call, never the app failing.
  - Blazor's error banner (`#blazor-error-ui`) is never visible: its style sheet hides it, its script shows it.
- **The whole studio suite on bUnit, what failed** (follow-up, not this step):
  - needs a browser and should say so (`@browser`, `[BrowserFact]`): the canvas and WebGL viewers (Street, Region,
    Area, the world map, model views), `Hover`, audio (Ruben's greeting), the window fit, details folds kept in the
    browser's storage;
  - the sign-in page tests (12, `AccountTests` and the like): browser-only, or signed in in process;
  - the rest to look at one by one: `Interactions_Building` not found (4), a job row changing under the poll, two
    values.

Next, for the owner to choose: mark the studio's browser-only scenarios and tests so the whole suite is green on
bUnit, then measure it; a warm in-process host between runs; the game suite's bUnit head.

## Where it stands (2026-10-25)

- The studio suite runs on the bUnit head: 143 passed, 22 browser-only skipped (`@browser`, `[BrowserFact]`), about
  50 s against about 2 m 40 s in the browser. Bruinell's Blazor, Html and Todo web suites run on both heads.
- **The game suite's bUnit head waits:** the owner, 2026-10-25, studio first; `Tomorrow.App.UITests` stays in the browser.
- Todo's web UAT suite was never made to run on bUnit; the warm host waits on reruns without a build being common
  (`.my/test-selection/next-plan.md`, item 5).
