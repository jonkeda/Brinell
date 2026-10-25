# bUnit head: development plan

Agent document. Builds [proposal.md](proposal.md). Status: plan, not started. Each step ends
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
| `TestIdAttribute`, `Timeouts`, `Logger`, `NearMiss`, `Name` | As `HtmlTestContextOptions` |

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

Not run yet.

## Step 5 results

Not run yet.
