# Review: Brinell.Html and Brinell.Blazor against Brinell.Maui

Date: 2026-09-26, after the Html plan was finished (`plan.md`, steps 0-9, T1, T2). Every claim
below was checked against the source on `move` at `4e91acc`, not against the plans. Two answers
rest on experiments run for this review:

- **The generator:** a probe template, generated and built inside `Brinell.Html`, then removed.
- **The UAT:** the Html and Blazor UAT suites, both 2/2.

**Update, 2026-09-26 (`7711074`):** the calls layer, its exceptions and `ScopeReadiness` moved
into Core, as one merged copy (`../core/review-what-can-move-now.md`, step L1). The rows and
verdicts below that spoke of two copies are updated. Section 4 marks what that closed.

**Update 2, 2026-09-26: the follow-ups are done.** All eight items of section 4 are implemented;
the table there says how, and sections 1-3 are corrected where they changed. Implementing them
found two Html bugs the review had not: a Playwright read of a re-rendered node waited out the
call, and a keyed row was found again by position. Both are fixed (CHANGELOG, Fixed).

## Short answers

| Question | Answer |
| --- | --- |
| 1. How alike are the architectures? | **The same shape.** The call model is now one copy in Core; the interfaces, bases, pages and controls are still two copies, one per stack. The layers, the class names and the rules (R0-R9) match one for one. The differences are ones the platforms force (no gesture bridge on the web, a driver with its own timeouts, frames), plus one real gap: Html controls are hand-written, not generated. Blazor is not a third architecture: it is a thin layer on Html of about 540 lines. |
| 2. Can Html and Blazor controls use the generator? | **Yes, and they do now.** Html's `ControlBase` has MAUI's `Run*` signatures, and every Html and Blazor control is a `.tpl.cs` and a generated `.gen.cs` (`tools/Scripts/CreateHtml.Bat`), with the generator unchanged (section 2). |
| 3. How far can the UAT be used? | **Fully, for one head at a time.** Every built-in phrase works on Html. The Todo `.uat.md` files run on the web head (33/33), and a scenario can use several apps of one head (Alice and Bob on one backend). **Not yet:** apps of different heads (MAUI and web) in one scenario (section 3). |

## 1. Architecture

### 1.1 Layer by layer

| Layer | Brinell.Maui | Brinell.Html | Same? |
| --- | --- | --- | --- |
| Own interfaces (AD-010, R9) | `IMauiElement`, `IMauiDriver`, `IMauiElementScope`, `IMauiScope`, `IMauiPage`, `IMauiTestContext`, `IMauiContainer` | `IHtmlElement`, `IHtmlDriver`, `IHtmlElementScope`, `IHtmlScope`, `IHtmlPage`, `IHtmlTestContext`, `IHtmlContainer` | Yes, one for one |
| Driver implementation | `Brinell.Maui.FlaUI` (Windows), `Brinell.Maui.Appium` (Android) | `Brinell.Html.Playwright` | Same role. Playwright calls take a timeout, so the Playwright driver reads `CallBudget` (now in Core) and adds `PlaywrightErrors.Live`. MAUI's drivers do not read the budget yet |
| Calls | `Brinell.Core.Calls`: `ControlCall`, `AttemptContext`, `Deadline`, `Poller`, `Observation`, `ObservationLog`, `Confirmer`, `ScopeGate`, `Confirmation<T>`, `CallBudget`, `NearMissSettings` | The same Core types | **One copy**, merged from both (the union of their features) |
| Scope readiness | `Brinell.Core.Scopes.ScopeReadiness` | The same, plus Html's own `BusySignal` (Ignore/Optional/Required on `<body data-busy>`) in `Brinell.Html.Scopes` | One model. The busy signal still differs in shape from Core's `BusySignalPolicy` (step L2 of the Core review) |
| Pages | `PageObjectBase`, `AppRoot` | `HtmlPageObjectBase`, `AppRoot` | Yes |
| Containers | `ContainerObjectBase`, `ComponentObjectBase` | `ContainerBase` (`AsksParent`, `ProbeContentReadiness`), `FrameScope` | Html has no `ComponentObjectBase` and no shortcuts; MAUI has no frames |
| Collections | `CollectionObjectBase` (1,086 lines), `ItemObjectBase`, `ItemKey`, `ItemStrategy`, `ElementMatch`, `ScrollHelper` | `CollectionObjectBase` (124 lines), `ItemObjectBase`, `ItemKey` | Same contract: a row follows its item by key and never acts on another. Html has no scroll-to-find strategies: the DOM holds the rendered rows, and `<Virtualize>` is handled by keys alone |
| Controls | `Controls/Base/*` capability bases (Clickable, Focusable, Toggle, Range, Selector, SelectableItem, ClickableItem), about 60 controls, **generated** (`.tpl.cs` + `.gen.cs`) | The same capability bases (no item bases), implementing the same Core interfaces, 20 controls plus Blazor's, **generated** the same way, with `ActAndConfirm` for actions that change state | Yes (section 2) |
| Exceptions | `Brinell.Core.Exceptions`: `StaleElementException`, `ElementNotReadyException`, `ScopeNotReadyException`, `AppUnavailableException`, `RouteUnavailableException` | The same Core types | **One copy**: a catch works for either stack |
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
| Html gives every driver call the call's remaining budget; a read cut off by the budget is not an observation | Yes: Playwright calls wait on their own | Right. **Done:** the cut-off rule is in Core's `Poller`, for both stacks. Inert on MAUI until its drivers take the budget |
| A confirmation runs under its own attempt context | Yes: it matters only when a driver reads the call's budget from the ambient `AttemptContext` | Right. **Done:** Core's `Confirmer` sets the context, for both stacks. MAUI gets the rule before it needs it |
| Busy signal from `<body data-busy>` (Optional/Required) | Partly: the web has no busy sentinel element | Reconcile with Core's `BusySignalPolicy` (Core review, step L2) |
| No gesture bridge, no physical-input rules (AD-005, AD-008) | Yes: Playwright drives the browser over its own protocol | Right |
| `FrameScope` | Yes | Right |
| No `ComponentObjectBase`, no shortcuts | No | Still a gap. The generator supports shortcuts, but Html has no component base to declare them on; the web `TodoStatus` is a container with hand-written members |
| Async members (`Interfaces/Async/*`, `RunWithElementAsync`), used in 9 UI-test files | No: MAUI has none | **Removed.** They wrapped the sync calls and added nothing; the `_Async` duplicate UI tests went with them |
| `RunWaitWithElement` has no `expected` parameter; no `RunGetWithOptionalElement` | No | **Fixed.** Html's `Run*` helpers are MAUI's, name for name and signature for signature (R8) |
| Html's `ControlBase` still implements Core's `IControlObject<TScope>` | No | Harmless. UAT discovery finds control properties through it |

### 1.4 Verdict

The Html work met its aim: the call model is the same on both stacks, with the same rules and the
same class names. The calls layer, its exceptions and scope readiness now exist **once**, in
Core, which is AD-010's move down for the leaf layer. The two Html budget fixes (the `Poller`
cut-off rule, the `Confirmer` context) came with the merge, so MAUI has them too.

What is still in two copies is the layer above: the interfaces, `ViewBase` and `ControlBase`,
the containers, pages and collections. They are typed on each stack's element and wait for the
`ICore*` interfaces. Until then the rule stands for them: a fix to one is checked on the other
in the same change.

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

**Done (update 2):** the R8 gap is closed, the async members are removed, and all Html and Blazor
controls are generated. The public surface changed where the generator's names differ from the
hand-written ones (CHANGELOG, Changed): `AssertTextContaining` is `AssertTextContains`,
`LinkControl.Href` is `GetHref()`, range inputs read `double?`, date and time inputs read
`DateOnly?` / `TimeOnly?`, and so on. Components and shortcuts on Html are still missing.

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
| **I enter {value} into {control}** | `Enter` | **none** (update 2: `TextInputControl`, `TextAreaControl`) |
| **{control} should have selected {value}** | `AssertSelectedText` | **none** (update 2: `SelectControl`, `RadioGroupControl`) |

So 16 of the 18 phrases work on Html. The two that do not fail with the runtime's "available
methods" message, not silently. Adding `Enter` and `AssertSelectedText` is small; with the
generator (section 2), `AssertSelectedText` comes from a `GetSelectedTextCore`.

**Update 2:** all 18 work. The Html capability bases implement MAUI's Core interfaces, and the
Html UAT's `form-controls-basic` scenario uses both new phrases.

### 3.3 What is not ready

**Update 2:** items 1, 3 and 4 are done, and item 2 is done for apps of one head. See section 4.

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

| # | What | Size | Why | Status |
| --- | --- | --- | --- | --- |
| 1 | Add the `expected` overloads of `RunWait*` and `RunGetWithOptionalElement` to Html's `ControlBase` | S | Restores R8; unblocks the generator, and the shared base the Core review's later steps need | **Done.** Html's `Run*` helpers are MAUI's; the old overloads and `RunWithElement` / `RunAssert(Action)` are gone |
| 2 | Keep the call-model copies from drifting on the confirmation context | S | – | **Closed by the move to Core:** one `Confirmer`, which sets its own context. What is left: `.docs/contracts/call-model.md` lists the budget-to-driver and confirmation-budget rules under "Where Playwright differs". They are now Core rules for every stack; move those two bullets to the general rules. **Done:** they are general rules in `call-model.md`, with a third one, "a read is about now" |
| 3 | Decide whether to keep the Html async interfaces | S | Needed before generating the control families | **Done: removed**, with the `_Async` duplicate UI tests (Html UI suite 40 to 22 tests, none lost) |
| 4 | Pilot generated Html controls (`LabelControl`, `TextInputControl`), then convert the rest | M | One public surface, components, UAT verbs | **Done** for every Html and Blazor control, and the bases (`CreateHtml.Bat`). Components and shortcuts are still missing |
| 5 | `Enter` and `AssertSelectedText` on Html | S | The full built-in UAT phrase list | **Done**, through the Core interfaces; used in the Html UAT |
| 6 | Move `HtmlSampleHost` to `WebAppHost` | S | Removes empty catches in test code | **Done** for both UAT fixtures; `WebAppHost.FindBuiltApp` replaces the three copies of the sample-path search |
| 7 | Todo UAT on the web head (?T2) | M | Same journeys, both heads | **Done.** `Brinell.Samples.Todo.Web.Uat` runs the MAUI suite's scenario files (linked, not copied) through web page objects with the MAUI member names, 33/33; the MAUI suite stays 32/32. Head tags (`Head` in `uat.config.md`, `@maui` / `@web`) replace the platform-tag gap of 3.3 item 3. `TodoDataPhrases` is shared (`ITodoArrangement`); `TodoScreenPhrases` is one per head |
| 8 | Several apps in one scenario (`todo-blazor.md` section 4) | L | The backend and multi-user tests. The shared exception types already help here: one `AppUnavailableException` for both heads | **Done for one head:** an `Apps` table, `IUatSessionHost`, `{app} uses the app`, failures that name the app, and a two-device web scenario. **Open:** MAUI and web apps in one scenario (needs a page catalog per session), users (?T3), and a `Name` on `IMauiTestContext` (MAUI's `ControlCall` still logs "Test") |
