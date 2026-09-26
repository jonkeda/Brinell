# Review: Brinell.Html and Brinell.Blazor against Brinell.Maui

Date: 2026-09-26, after the Html plan was finished (`plan.md`, steps 0-9, T1, T2). Every claim
below was checked against the source on `move` at `4e91acc`, not against the plans. Two answers
rest on experiments run for this review:

- **The generator:** a probe template, generated and built inside `Brinell.Html`, then removed.
- **The UAT:** the Html and Blazor UAT suites, both 2/2.

## Short answers

| Question | Answer |
| --- | --- |
| 1. How alike are the architectures? | **The same shape, in two copies.** The layers, the class names and the call model (R0-R9) match one for one. The differences are ones the platforms force (no gesture bridge on the web, a driver with its own timeouts, frames), plus one real gap: Html controls are hand-written, not generated. Blazor is not a third architecture: it is a thin layer on Html of about 540 lines. |
| 2. Can Html and Blazor controls use the generator? | **Yes, with a small change to Html's `ControlBase`, and the generator itself unchanged.** The probe generated and built once two overloads were added (section 2). Get, Set, action and Assert members compiled as generated; only `Wait*` did not. |
| 3. How far can the UAT be used? | **For single-app scenarios on Html and Blazor pages: now,** with the built-in phrase list (minus two verbs) and custom phrase classes. **Not yet:** the Todo `.uat.md` files on the web head, and several apps in one scenario. Both need work that is in the plan's deferred list (section 3). |

## 1. Architecture

### 1.1 Layer by layer

| Layer | Brinell.Maui | Brinell.Html | Same? |
| --- | --- | --- | --- |
| Own interfaces (AD-010, R9) | `IMauiElement`, `IMauiDriver`, `IMauiElementScope`, `IMauiScope`, `IMauiPage`, `IMauiTestContext`, `IMauiContainer` | `IHtmlElement`, `IHtmlDriver`, `IHtmlElementScope`, `IHtmlScope`, `IHtmlPage`, `IHtmlTestContext`, `IHtmlContainer` | Yes, one for one |
| Driver implementation | `Brinell.Maui.FlaUI` (Windows), `Brinell.Maui.Appium` (Android) | `Brinell.Html.Playwright` | Same role. Playwright calls take a timeout, so Html adds `CallBudget` and `PlaywrightErrors.Live` |
| Calls (`Calls/`) | `ControlCall`, `AttemptContext`, `Deadline`, `Poller`, `Observation`, `ObservationLog`, `Confirmer`, `ScopeGate` | The same eight, plus `CallBudget`; `Confirmation` lives in `Calls/` rather than `Controls/Base/` | Yes. The two copies were written to match and differ only where section 1.3 says |
| Scope readiness | `Scopes/ScopeReadiness` | `Scopes/ScopeReadiness`, plus `BusySignal` (Ignore/Optional/Required on `<body data-busy>`) | Same model; the busy signal differs in shape from Core's `BusySignalPolicy` (noted in `move-down.md`) |
| Pages | `PageObjectBase`, `AppRoot` | `HtmlPageObjectBase`, `AppRoot` | Yes |
| Containers | `ContainerObjectBase`, `ComponentObjectBase` | `ContainerBase` (`AsksParent`, `ProbeContentReadiness`), `FrameScope` | Html has no `ComponentObjectBase` and no shortcuts; MAUI has no frames |
| Collections | `CollectionObjectBase` (1,086 lines), `ItemObjectBase`, `ItemKey`, `ItemStrategy`, `ElementMatch`, `ScrollHelper` | `CollectionObjectBase` (124 lines), `ItemObjectBase`, `ItemKey` | Same contract: a row follows its item by key and never acts on another. Html has no scroll-to-find strategies: the DOM holds the rendered rows, and `<Virtualize>` is handled by keys alone |
| Controls | `Controls/Base/*` capability bases (Clickable, Focusable, Toggle, Range, Selector, SelectableItem, ClickableItem), about 60 controls, **generated** (`.tpl.cs` + `.gen.cs`) | The same capability bases (no item bases), 20 controls, **hand-written**, with `ActAndConfirm` for actions that change state | Same rules, different production method (section 2) |
| Exceptions | `MauiExceptions.cs` | `HtmlExceptions.cs`: `StaleElementException`, `ElementNotReadyException`, `ScopeNotReadyException`, `AppUnavailableException`, `RouteUnavailableException` | Same set of signals |
| Testing | `MauiTestFixtureBase`, `NavigationRecovery`, `UatPlatformsAttribute` | `HtmlTestFixtureBase`, `WebAppHost` | MAUI has recovery and platform tags; Html has a host that starts a web app on a free port |
| Near-miss warnings, `BRINELL_CALL_LOG` | Yes | Yes | Yes |

### 1.2 Brinell.Blazor

Blazor adds no layer of its own. It is 25 files, about 540 lines:

- `BlazorTestContext` implements `IHtmlTestContext` over Playwright.
- `BlazorPageObjectBase` is an `HtmlPageObjectBase` that **requires** the busy signal, so a
  prerendered page is never acted on.
- `BlazorTestFixtureBase`.
- 19 controls. Most subclass the Html control and add only constructors (`ButtonControl`,
  `TextInputControl`: 11 lines each). The few with their own behaviour are `NavMenuControl`,
  `MediaControlBase` (audio, video), `ImageControl` and `IFrameControl`.

The Blazor rules are Html's rules, and so are its fixes. Keep it that way. Behaviour that is
Blazor-specific (inputs bind on `input`, so use `Fill`) belongs in the Html control, where it
holds for every page.

### 1.3 Where they differ, and whether that is right

| Difference | Forced by the platform? | Verdict |
| --- | --- | --- |
| Html gives every driver call the call's remaining budget; a read cut off by the budget is not an observation | Yes: Playwright calls wait on their own | Right. It moves to Core with the call model |
| An Html confirmation runs under its own attempt context | Yes: it matters only because the Html driver reads the call's budget from the ambient `AttemptContext` | Right. MAUI's `Confirmer` does not set a context, and nothing below a MAUI call reads `AttemptContext.Current`, so MAUI does not have the bug. It will once its drivers take the call's budget: carry the rule down with the call model |
| Busy signal from `<body data-busy>` (Optional/Required) | Partly: the web has no busy sentinel element | Reconcile with Core's `BusySignalPolicy` in the move down |
| No gesture bridge, no physical-input rules (AD-005, AD-008) | Yes: Playwright drives the browser over its own protocol | Right |
| `FrameScope` | Yes | Right |
| No `ComponentObjectBase`, no shortcuts | No | A gap. It comes with the generator (section 2) |
| Async members (`Interfaces/Async/*`, `RunWithElementAsync`), used in 9 UI-test files | No: MAUI has none | An Html extra with no MAUI counterpart. The async members wrap the sync calls, so they add no behaviour. Decide whether to keep them before generating (the generator writes no async members) |
| `RunWaitWithElement` has no `expected` parameter; no `RunGetWithOptionalElement` | No | **A deviation from R8** ("same `Run*` names and signatures on both hierarchies"). This is what stops the generator (section 2) |
| Html's `ControlBase` still implements Core's `IControlObject<TScope>` | No | Harmless. UAT discovery finds control properties through it |

### 1.4 Verdict

The Html work met its aim: the call model is the same on both stacks, with the same rules and the
same class names. It exists **twice**, in `Brinell.Maui/Calls` and `Brinell.Html/Calls`. That is
AD-010's intent: MAUI and Html go ahead of Core, and the proven shape moves down to Core as one
copy. The two copies should not drift further before then. A fix to one should be checked on the
other in the same change. The two Html budget fixes (the `Poller` cut-off rule, the `Confirmer`
context) do not apply to MAUI today, because MAUI's drivers do not take the call's budget.

## 2. The generator for Html and Blazor controls

### 2.1 What the generator depends on

`tools/Brinell.Generator` is less tied to MAUI than its name and script suggest:

- **The element type is detected, not fixed.** `DetectElementType` takes the first `*Core`
  method's first parameter; a candidate only needs a type name containing `Element`.
  `IHtmlElement` qualifies.
- **The attributes** (`[GenerateComparisons]`, `[AbsenceTolerant]`, `[SkipGeneration]`,
  `[FluentReturn]`) live in `Brinell.Core`, which Html already references.
- **The emitted code calls helpers by name:** `RunDoWithElement`, `RunSetWithElement`,
  `RunGetWithElement`, `RunGetWithOptionalElement`, `RunWaitWithElement`,
  `RunWaitWithOptionalElement`, `RunAssertWithElement`, `RunAssertWithOptionalElement` and
  `TryFindElement`. Whatever base class supplies those names with MAUI's signatures can be
  generated.
- **The only MAUI-specific part is the script:** `tools/Scripts/CreateMaui.Bat` lists the two
  MAUI `Controls` folders. The CLI accepts any folder.

### 2.2 The probe

A template, `ProbeControl.tpl.cs` on `ControlBase<TScope>`, declared one of each shape:

- `GetCaptionCore` with `[GenerateComparisons(Equals | Contains)]`;
- `IsTickedCore`;
- an `[AbsenceTolerant]` `IsMarkedCore`;
- `SetCaptionCore`, whose body uses `ActAndConfirm`;
- `PressCore`.

The template was put in `srcnew/Brinell.Html/Controls/_Probe`, generated and built for
`net10.0`:

| Run | Result |
| --- | --- |
| Html's `ControlBase` as it is | Generation succeeded; the build failed on every `Wait*` member (13 errors): the generated `RunWaitWithElement(expected, predicate, timeoutMs)` has no match, because Html's overload has no `expected` |
| With two overloads added in a partial (`RunWaitWithElement<T>(T? expected, …)` and `RunWaitWithOptionalElement<T>(T? expected, …)`, each returning `true` for a null `expected` and otherwise calling the existing helper) | **Build succeeded**, with no change to the generator |

The probe was then deleted, and `Brinell.Html` builds clean again. One thing the probe did not
exercise: an `[AbsenceTolerant]` `Get*Core` would also need `RunGetWithOptionalElement`, which
Html lacks.

### 2.3 What it takes

1. **Close the R8 gap in `Brinell.Html/Controls/ControlBase.cs`:** add the `expected` overloads
   of `RunWaitWithElement` and `RunWaitWithOptionalElement`, and add
   `RunGetWithOptionalElement`. About 30 lines. Keep the existing overloads: hand-written
   members use them.
2. **A generation script:** `CreateHtml.Bat`, or a third and fourth input folder in
   `CreateMaui.Bat` (`Brinell.Html/Controls`, `Brinell.Blazor/Controls`).
3. **Convert control by control** with the `convert-control` procedure. Its steps apply as
   they are; only the element type differs. `Is*Core` takes `IHtmlElement?`, as MAUI's do
   (`IsEnabledCore(IMauiElement? element)`), because the generated `IsX()` passes
   `TryFindElement()`.
4. **Update the docs.** `.docs/contracts/control-object.md` ("Html controls are hand-written")
   and the `html-control` skill ("no generator") must change. `.my/html/skills.md` had planned
   generated Html controls, and the step 9 docs recorded hand-written ones.

What stays hand-written, as on MAUI:

- pages, app containers, frames and collections (a `.cs` with no Core methods);
- the async members, unless they are dropped;
- members whose semantics the generator cannot express. Html's `IsTextContaining(substring,
  timeoutMs)` is one: it answers now at a timeout of 0 and waits otherwise. Better to replace
  it with a generated `WaitTextContains`.

### 2.4 Is it worth it?

Yes, and mainly for consistency, not size. Html has 20 controls (about 900 lines), and Blazor's
are mostly constructors. What generation buys:

- **The same public surface on both stacks.** `AssertTextContains`, `WaitX`, and the comparison
  variants come from one contract, instead of Html approximating them by hand
  (`AssertTextContaining` versus MAUI's `AssertTextContains`).
- **UAT verbs for free** (section 3.2): a `Get*Core` yields the `Assert*` a phrase binds to.
- **Components and shortcuts** on Html, which are missing now.
- **One `control-source-builder` flow** for third-party web component libraries, as it already
  exists for CommunityToolkit.

Suggested order: close the R8 gap first (it is right in any case). Then pilot the conversion on
`LabelControl` and `TextInputControl`, diffing the public surface as `convert-control` step 9
says. Decide about the async interfaces before converting the families.

## 3. The UAT on Html and Blazor

### 3.1 What runs today

| Project | Target | Scenarios | Result (2026-09-26) |
| --- | --- | --- | --- |
| `testsnew/Brinell.Html.Uat.Tests` | HTML | `counter-basic`, `form-controls-basic` | 2/2 |
| `testsnew/Brinell.Blazor.Uat.Tests` | BLAZOR | `counter-basic`, `login-invalid-credentials` | 2/2 |
| `testsnew/Brinell.Maui.Uat.Tests` (for comparison) | MAUI | 3, plus `ExpectedFailures` | – |
| `samples/Todo/tests/Brinell.Samples.Todo.Uat` (for comparison) | MAUI | TOD.01-TOD.10 (341 steps) | MAUI only |

The UAT engine (`Brinell.Uat`) depends only on `Brinell.Core`, and it is stack-neutral where it
counts:

- **Targets are named by fixture base class** (`UatTargetConventions`): `MauiTestFixtureBase`
  is MAUI, `HtmlTestFixtureBase` is HTML and `BlazorTestFixtureBase` is BLAZOR.
- **Pages** are found by `[TestPage]`, `IPageObject` or the name suffix `Page`; **controls** by
  `IControlObject<>` (which Html's `ControlBase` implements), a UAT attribute, or the name
  suffix.
- **Built-in phrases** are declared with `[UatStep]` on Core's interfaces, and bound to the
  control **by method name**, so a control does not need to implement those interfaces. Html
  controls do not, and the steps still work.
- **Custom phrases** (`UatPhraseClassBase`), typed test data (AD-012) and settings are
  stack-neutral.

### 3.2 The built-in phrase list on Html controls

| Phrase | Method | Html control that has it |
| --- | --- | --- |
| I tap {control} | `Click` | every clickable control |
| I set {control} to {value} | `SetText` | `TextInputControl` (not `TextAreaControl`) |
| I clear {control} | `Clear` | `Control` |
| I select {value} from {control} | `SelectByText` | `SelectControl`, `RadioGroupControl` |
| I check / uncheck {control}; should be checked / unchecked | `Check`, `Uncheck`, `AssertChecked` | `CheckBoxControl` |
| should exist / be visible / be enabled (and their negations) | `AssertExists`, `AssertVisible`, `AssertEnabled` | `ControlBase` |
| should equal / contain {value} | `AssertText`, `AssertTextContains` | `ControlBase` |
| **I enter {value} into {control}** | `Enter` | **none** |
| **{control} should have selected {value}** | `AssertSelectedText` | **none** |

So 16 of the 18 phrases work on Html. The two that do not fail with the runtime's "available
methods" message, not silently. Adding `Enter` and `AssertSelectedText` is small; with the
generator (section 2), `AssertSelectedText` comes from a `GetSelectedTextCore`.

### 3.3 What is not ready

1. **The Todo scenarios on the web head.** `Brinell.Samples.Todo.Uat` is MAUI-bound in three
   places:
   - its fixture (`TodoUatFixture`: `IMauiTestContext`, `[UatPlatforms(Windows, Android)]`);
   - its phrases (`TodoScreenPhrases` uses `Brinell.Maui.Pages` and dialogs);
   - its page objects, taken from `Brinell.Samples.Todo.UITests` (the MAUI tests).

   The scenario text is not MAUI-bound: the web head uses the MAUI AutomationIds as
   `data-testid`s. What it needs:
   - an Html fixture (`WebAppHost` and `TodoWebFixture` already exist in the web UI tests);
   - web page classes with the MAUI page names;
   - a head-neutral version of `TodoScreenPhrases`, or one per head.

   This is plan deferral ?T2.
2. **Several apps in one scenario** (MAUI and web, or two users). The engine has one fixture
   per scenario class and a static `_currentFile` in `UatScenarioTestBase`. `todo-blazor.md`
   section 4 has the design: an `Apps` table in `uat.config.md`, and a step that switches the
   current session. The Html side is ready for it: a named context, `WebAppHost`, and
   `X-Todo-Client` with the API's request log.
3. **Platform tags for Html.** `UatPlatformsAttribute` takes `MauiPlatform` values. There is no
   way to say "this scenario runs on Chromium only" or "on the web head only". That matters
   once one scenario file runs on both heads.
4. **The Html UAT fixture's own host.** `HtmlSampleHost` runs `dotnet run` with empty catches in
   test code. Moving it to `WebAppHost` is the small follow-up from plan step 7.

### 3.4 Verdict

A team can write `.uat.md` scenarios for a Blazor or plain web app today, for one app per
scenario. The route is the one the two sample projects show: an `HtmlTestFixtureBase` or
`BlazorTestFixtureBase` fixture, page objects with `data-testid` locators, the built-in phrases
and custom phrase classes. The two MAUI-first ambitions, running the Todo journeys on the web
and one scenario across apps, need the ?T2 and section 4 work first. In order:
1. `WebAppHost` in the UAT fixtures;
2. `Enter` and `AssertSelectedText`;
3. the Todo web fixture and phrases;
4. Html platform tags;
5. then the multi-app engine changes.

## 4. Recommended follow-ups

| # | What | Size | Why |
| --- | --- | --- | --- |
| 1 | Add the `expected` overloads of `RunWait*` and `RunGetWithOptionalElement` to Html's `ControlBase` | S | Restores R8; unblocks the generator |
| 2 | Write down in `.docs/contracts/call-model.md` that a confirmation gets its own attempt context once a driver reads the ambient budget (Html today; MAUI after the move down) | S | The two call-model copies should not drift |
| 3 | Decide whether to keep the Html async interfaces | S | Needed before generating the control families |
| 4 | Pilot generated Html controls (`LabelControl`, `TextInputControl`), then convert the rest | M | One public surface, components, UAT verbs |
| 5 | `Enter` and `AssertSelectedText` on Html | S | The full built-in UAT phrase list |
| 6 | Move `HtmlSampleHost` to `WebAppHost` | S | Removes empty catches in test code |
| 7 | Todo UAT on the web head (?T2) | M | Same journeys, both heads |
| 8 | Several apps in one scenario (`todo-blazor.md` section 4) | L | The backend and multi-user tests |
