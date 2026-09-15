# Brinell.Maui without mouse, keyboard or window focus

**The question.** Can `Brinell.Maui` stop using the real mouse, the real keyboard and the desktop
foreground entirely, how do we remove the fallback code in `Brinell.Maui.FlaUI` that still allows
it, and does Android need any of it?

**The short answers.**

1. **Feasible: yes, on Windows, for apps that host the Brinell bridge.** The suite already runs
   that way. Physical input is refused by default, and the last full run with nothing set was
   281 passed, 0 failed. A refused call throws, so none of those passes touched the mouse or the
   keyboard. What is left is fallback code that nothing in the suite reaches. Removing it turns
   the bridge from recommended into required, and a small set of operations has no replacement
   yet (section 1.3).
2. **Removal is mostly deleting code, not rewriting it.** One file goes entirely
   (`PhysicalPointer.cs`). About a dozen fallback branches in `FlaUIMauiElement` change from
   "take the foreground and type" to "throw, naming the verb". The policy switch and the test
   attributes built around it go too. The shared control code in `Brinell.Maui` stays, because
   Android still uses it (section 2).
3. **Android needs no physical input from the host.** Appium's UiAutomator2 server runs inside the
   device and injects touch and text there. The PC's mouse, keyboard and window focus play no
   part. Some of `AppiumMauiElement` is desktop leftover that can go in the same cleanup
   (section 3).

---

## 1. Is it feasible?

### 1.1 Where it stands today

Measured, not assumed:

| Claim | State | Evidence |
|---|---|---|
| No synthesized keystrokes | **Holds** | `PhysicalInput` refuses by default (`QuietByDefault.cs`, a module initializer). A refused call throws. |
| No synthesized pointer input | **Holds** | Same gate. `grep "Mouse\."` in `Brinell.Maui.FlaUI` finds only `Windowing/PhysicalPointer.cs`. |
| App does not take the foreground on click | **Holds** | `WS_EX_NOACTIVATE` on the app window took it from two grabs per click to zero (`QuietWindow.RefuseActivation`). |
| App stays behind the user's work | **Holds, confirmed by a person on 2026-09-13** | Watchdog pushes back the one launch grab within milliseconds. `ForegroundWatchdogTests` fails otherwise. |
| Full suite green with nothing set | **Holds** | 2026-09-14: 297 total, 281 passed, 16 skipped, 0 failed, 3 m 31 s. |

The 16 skips are 11 Stepper (step 31), 2 CollectionView recycling, 1 opt-in stress test, and the
**two `AppRootScopeTests.BackToHub_*` tests**. Those two skip because they are
`[PhysicalInputFact]`, meaning they exist to click the toolbar item with the real mouse. They are
the only tests in `Brinell.Maui.UITests` that call a physical API on purpose.

**So removing physical input changes no test result on the Windows suite except those two.**
The fallback code is already dead in practice. The work is making it dead by construction.

### 1.2 What "no physical input" requires

**The app under test must host the bridge.** Every quiet route for focus, text, submit, gestures,
scrolling, navigation, dates, selection, alerts and menus is a `BrinellVerb` answered inside the
app. The bridge is compiled in only under `BRINELL_UIA_BRIDGE` (Debug), switched on only when the
launcher sets `BRINELL_UIA_BRIDGE=1` and the app calls `UseBrinellGestureBridge()` (AD-008).

For an app **without** the bridge, Brinell.Maui on Windows would be limited to what plain UI
Automation patterns give:

| Works without the bridge | Stops working without the bridge |
|---|---|
| Invoke, Toggle, SelectionItem (buttons, switches, radio buttons) | Gestures (tap, long press, swipe, pan, pinch) |
| ValuePattern text writes (Entry, Editor, SearchBar via nested Edit) | `Submit` where the app uses an event handler rather than a command |
| RangeValue (Slider) | Focus and blur (today's fallback is foreground + `SetFocus`) |
| ExpandCollapse dropdowns (Picker as ComboBox) | Reading focus while the window is behind (`HasKeyboardFocus` is false for a background window) |
| ScrollPattern and ScrollItem | Scroll to an element by id, navigation, flyout, alerts, menu items |
| All reads (text, enabled, visible, checked, bounds) | ToolbarItem activation (see below) |

**This is the real decision here, and it is a product decision rather than a technical one.** If
Brinell.Maui is only used against apps we build, requiring the bridge costs nothing. If it is meant
to drive third-party MAUI apps on Windows, those apps lose the right-hand column. Throwing a clear
`NotSupportedException` is better than quietly taking the machine, but it is still a loss.

### 1.3 Operations with no quiet route today

Each of these is currently served only by `PhysicalPointer` or the keyboard:

| Operation | Today | Proposed |
|---|---|---|
| `ToolbarButton.Click` (any ToolbarItem) | Real click. UIA Invoke reports success and does nothing (measured 4/4 failures). | **Decided: new verb `InvokeToolbarItem` (903)**, see step 1b. |
| `IElement.DoubleClick` | Real double-click | Route to the `DoubleTap` verb (101) when declared, else throw. The verb already exists, but `DoubleClick` does not use it. |
| `IElement.RightClick` | Real right-click | Throw. To reach a menu *item*, use `InvokeMenuItem`. A test about the context menu *appearing* cannot be quiet on Windows. |
| `IElement.Hover` | Real mouse move | Throw. No pointer-enter verb is planned. Add one only when a real app needs it. |
| `Swipe` with no declared direction verb | Real drag | Throw `GestureUnavailableException`. `Pan` (107) exists for free-form drags if needed. |
| `LongPress` with no declared verb | Real press-and-hold | Throw `GestureUnavailableException`. |
| `SendKeys(text, Keys)`, per-keystroke typing | Foreground + `Keyboard.Type` | Throw on Windows. **AD-008 rule 2 ("is the physical path the thing under test?") becomes an Android-only answer.** A test of per-character `TextChanged`, `MaxLength` or numeric-keyboard refusal belongs on the mobile head. |
| `ClickableControlBase.PressCore` (Space key) | Foreground + Space | Throw on Windows. It exists for WinUI buttons whose Invoke does nothing, the same family as ToolbarItem. |

None of these is reached by the current Windows suite. The risk is to *other* suites and future
tests, not to the green run.

### 1.4 Window focus

"Not using the window's focus" has two parts, and they need different handling:

- **Taking the foreground in order to type or click.** `PhysicalPointer.BringAppToFront` does
  this. It goes with the pointer code.
- **Keeping the app from taking the foreground by itself.** `QuietWindow` (the watchdog,
  `WS_EX_NOACTIVATE`, `SendBehind`) does this. It is **not** physical input. It is what keeps the
  app out of the user's way. **Keep it, and make it unconditional.** Today it only runs when the
  policy is not `Allowed`.

One grab still happens: at launch, Windows gives the new process the foreground before anything
can intervene, and the watchdog pushes it back within milliseconds. Removing that grab completely
would need a non-activating `STARTUPINFO` show (step 47's idea). It is optional, because a person
has confirmed the current behaviour is not noticeable.

### 1.5 Verdict

| | Feasible? |
|---|---|
| Windows, app hosts the bridge | **Yes, and already true in behaviour.** Remaining work is deletion plus one new verb for ToolbarItem. |
| Windows, app without the bridge | **Partly.** Pattern-only operations work, the rest throw with a clear message. |
| Tests *of* physical input behaviour | **Not on Windows.** Move them to the Android head or drop them. |

---

## 2. Removing the fallback code

Ordered so the suite stays green after every step. Run the Background tier after each code step
and a full run at the end.

### Step 1: Decisions (decided 2026-09-14)

1. **The bridge is a hard requirement for Brinell.Maui on Windows.** An app without
   `UseBrinellGestureBridge()` gets only what plain UI Automation patterns offer, and every other
   operation throws, naming the verb the app would need to declare. Documentation says
   "required", not "recommended".
2. **ToolbarItem gets a new verb, `InvokeToolbarItem`.** It is designed in step 1b and built
   before the pointer code is deleted, so `ToolbarButton` always has a working route on Windows.

### Step 1b: The `InvokeToolbarItem` verb

> **Done 2026-09-14.** Full Windows suite with nothing set: **301 total, 287 passed, 14 skipped,
> 0 failed**, 2 m 57 s (was 297 / 281 / 16). Both `BackToHub_*` tests now run as plain `[Fact]`s
> and pass, with `BackToHub.Click()` wrapped in the refused policy. `ContractTests` 43/43.
> The Android head builds but was not run.
>
> **Differences from the design below:**
> - **The app answers only from the page on screen.** `ExchangeAnywhere` takes the first target
>   that answers, and a popped page keeps answering until its handler goes. Its Back item's
>   command acts on the live stack, so a stale page could pop the page the test is on. The page
>   on screen means the top of its navigation stack with no modal above it, or `Shell.CurrentPage`
>   in a Shell app. Other pages answer `UIA_E_ELEMENTNOTAVAILABLE`, and the walk moves on.
> - **The sample reuses the demo page's existing `PageToolbarRefresh` and `PageToolbarAbout`
>   items**, which had no handlers, instead of adding a counter. It adds `PageToolbarDelete`,
>   disabled through a command whose `CanExecute` is false. Actions record `PageToolbar/<item>`
>   into the page's `LastAction` label.
> - **Four tests, not three plus one:** `SupportsToolbarVerb_IsTrue_WhenThePageDeclaresIt`,
>   `InvokeToolbarItem_RaisesTheItem_WithoutThePointer`,
>   `InvokeToolbarItem_WhenTheItemIsDisabled_IsRefusedAndDoesNothing`,
>   `InvokeToolbarItem_WithNoSuchItem_IsRefusedWithTheReason`. The "click uses the verb" test is
>   `AppRootScopeTests.BackToHub_Click_ReturnsToTheHub`, now run with input refused.
> - **`MauiFixture.ReturnToHub`** no longer records a physical-input use before `BackToHub.Click()`,
>   because on Windows that click is now the verb. (Pulled forward from step 6.)
> - **Open, not a regression:** `ToolbarVerbTests` is linked into the Android head, like
>   `MenuVerbTests`. Both are bridge-only and will fail there unless the Android run filters them
>   out.

**Why a new verb is small.** In MAUI, `ToolbarItem` derives from `MenuItem` and implements
`IMenuItemController`. `InvokeMenuItem` already calls `IMenuItemController.Activate()`, which is
what each platform's handler calls when a user presses the item, so the same `Command` runs and
the same `Clicked` fires. The new verb is the same call over a different collection:
`page.ToolbarItems` instead of `MenuBarItems` and context flyouts.

**Why a separate verb rather than widening `InvokeMenuItem`.** A verb's meaning is part of the
frozen contract, and an app that declared `InvokeMenuItem` agreed to have its menus reachable, not
its toolbar. A separate number also lets `SupportsToolbarVerb` give an exact answer, where a
widened verb could not tell an old app from a new one.

**Contract** (`srcnew/Brinell.Uia.Contracts/BrinellVerb.cs`):

| | |
|---|---|
| Number | `InvokeToolbarItem = 903` (range 900-999, dialogs and menus) |
| Transport | `Exchange`, because the outcome travels in the payload, as it does for `InvokeMenuItem` |
| Kind | `AppAction`. The page is the address, and the toolbar item is not in the page's tree. |
| Argument | The toolbar item's `AutomationId` |
| Answers | `S_OK` with `""` when raised. `S_OK` with `"disabled"` when found but not enabled. `UIA_E_ELEMENTNOTAVAILABLE` when no item has that id. `E_INVALIDARG` for an empty id. |

It uses `S_OK` plus a payload for "disabled", not `BRINELL_E_DECLINED`, for the reason recorded
in step 43: a failing `Exchange` loses its payload, and "disabled" needs to reach the client.

**App side** (`samples/Brinell.Maui.AppSupport/Uia/`):

- `MauiCapabilities.InvokeToolbarItem(VisualElement element, string automationId, out string outcome)`
  - Search `PageOf(element).ToolbarItems`, then the toolbar items of each ancestor page
    (`NavigationPage`, `TabbedPage`, `FlyoutPage`, `Shell`), because on Windows the visible
    toolbar merges items from those levels.
  - Answer `"disabled"` when `IsEnabled` is false. `IsEnabled` follows `Command.CanExecute`,
    as measured for `MenuItem`.
  - Otherwise call `((IMenuItemController)item).Activate()`.
  - Share the disabled constant with `InvokeMenuItem` (`MenuItemDisabled`).
- `MauiVerbDispatcher`: add a `case BrinellVerb.InvokeToolbarItem`, like the `InvokeMenuItem` case.
- `VerbBindings` and classification: add it wherever `InvokeMenuItem` is listed.

**Client side:**

| File | Change |
|---|---|
| `srcnew/Brinell.Maui/Interfaces/IMauiDriver.cs` | `void InvokeToolbarItem(string automationId)`, with a default that throws `NotSupportedException`. `bool SupportsToolbarVerb`, with a default of `false`. |
| `srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs` | Implement both through `BridgeVerbRunner.ExchangeAnywhere`, and map the answers to messages as `InvokeMenuItem` does. `SupportsToolbarVerb` asks the bridge targets whether any declares the verb. |
| `srcnew/Brinell.Maui/Controls/Buttons/ToolbarButton.cs` | `ClickCore`: if `Context.Driver.SupportsToolbarVerb`, check enabled and call `InvokeToolbarItem` with the locator's id. Otherwise `element.Click()`, which is the Appium path. The locator is normally `ByAccessibilityId`, and its value is the `AutomationId` the app wrote. Rewrite the type remarks: the Invoke-pattern measurement stays as history, and the click is no longer the Windows route. |
| `srcnew/Brinell.Maui.Appium` | Nothing. The default `SupportsToolbarVerb => false` keeps the tap. |

**Sample app:** add `InvokeToolbarItem` to the verb list declared in
`HubPage.AddBackToHub` (`samples/Brinell.Samples.Maui.App/Pages/HubPage.xaml.cs:135`), next to
`InvokeMenuItem`. Add a second, non-navigating toolbar item to one page, for example a counter
that updates a label. Without it, the only thing the verb is ever tested against also navigates,
and a test cannot tell "raised" from "raised and the page happened to change".

**Tests:**

| Test | Where | Asserts |
|---|---|---|
| `InvokeToolbarItem_RaisesTheItemsCommand` | `Tests/Navigation/ToolbarVerbTests.cs` (new) | The counter label changes. Run under the refused policy while that policy still exists. |
| `InvokeToolbarItem_WhenDisabled_IsRefusedAndDoesNothing` | same | A `BrinellException` that says "disabled", and the label does not change |
| `InvokeToolbarItem_WithNoSuchItem_IsRefusedWithTheReason` | same | The message names the id and says ids are matched by `AutomationId` |
| `ToolbarButton_Click_UsesTheVerbOnWindows` | same | `BackToHub.Click()` returns to the hub with physical input refused |
| Contract classification | `testsnew/Brinell.Uia.Tests/ContractTests.cs:264` | Add 903 to the `AppAction` list. The test fails until the verb is classified. |

**Order:** contract, then app side, then client, then tests. Run `ContractTests`, then
`--filter "FullyQualifiedName~Tests.Navigation"`, then the Background tier. **Done when** a
toolbar item runs with physical input refused and both `BackToHub_*` tests pass as plain `[Fact]`s.

### Step 2: `Brinell.Maui.FlaUI`, the code that touches the desktop

> **Done 2026-09-14.** Full Windows suite with nothing set: **296 total, 281 passed, 14 skipped,
> 1 failed**. The failure is `OccludedScreenshotTests.Screenshot_OfOccludedWindow_ShowsTheApp`,
> whose own check ("the occluding window did not cover the screen") fails **on the committed
> code too**, measured by stashing and rebuilding. Other topmost windows were on the desktop at
> the time, so it is environmental, not this step. Background and Navigation tiers: 87/87.
>
> **As written:** `PhysicalPointer.cs` and `QuietByDefault.cs` deleted, `Pointer` removed from
> the driver, `QuietWindow` unconditional, and every physical branch in `FlaUIMauiElement` now
> throws `NotSupportedException` (or `GestureUnavailableException` for gestures) naming the
> quiet route. `Click` uses the Tap verb, `DoubleClick` DoubleTap, `SupportsFocus` asks the
> bridge. No csproj change was needed: WinForms comes in through FlaUI.Core, not the project.
>
> **Pulled forward from step 6, because deleting `QuietByDefault` makes the policy read
> `Allowed` for MAUI again**, which would have failed two tests and quietly serialised the
> collections and disabled the watchdog and parallelism checks:
> - Deleted `DesktopLease.cs`, `DesktopGateTests.cs` (4 tests), `QuietDefaultTests.cs` (1 test)
>   and `PhysicalInputTrait.cs` (unused after step 1b). Removed the leases from `MauiFixture` and
>   `ShellFixture`, and the two entries from `timing-baseline.json`. Updated `AssemblyInfo.cs`.
> - `CollectionParallelismTests` and `ForegroundWatchdogTests` no longer branch on the policy.
> - `FocusVerbTests.RefusedPolicy_StillRefuses` became
>   `ActionsWithNoSemanticRoute_ThrowInsteadOfUsingRealInput`: `Click`, `SendKeys(Keys)`,
>   `RightClick` and `Hover` throw `NotSupportedException`.
>
> **Still open for step 6:** the `PhysicalInput.OverridePolicy(Refused)` wrappers in the verb
> tests. They are harmless but now prove nothing.

| File | Change |
|---|---|
| `Windowing/PhysicalPointer.cs` | **Delete** (160 lines). |
| `FlaUIMauiDriver.cs` | Remove the `Pointer` property and its construction. |
| `FlaUIMauiElement.cs` | Rewrite each physical branch as described below, and remove `FocusForKeyboardInput`. Drop the `using` directives for `FlaUI.Core.Input` and `FlaUI.Core.WindowsAPI` and the `System.Windows.Forms.Clipboard` call. |
| `QuietByDefault.cs` | **Delete.** Nothing governs input any more, so there is no default to declare. |
| `Windowing/QuietWindow.cs` | Remove the two `PhysicalInput.Policy != Allowed` checks. Always watch, refuse activation and send behind. |
| `Brinell.Maui.FlaUI.csproj` | Check whether `UseWindowsForms` (or the WinForms reference) was there only for the clipboard. If so, remove it. |

`FlaUIMauiElement` member by member:

| Member | Now | After |
|---|---|---|
| `Click()` | `Pointer.Click` | Tap verb if declared, else `NotSupportedException` naming `Invoke()` and the Tap verb |
| `DoubleClick()` | `Pointer.DoubleClick` | DoubleTap verb if declared, else throw |
| `RightClick()` | `Pointer.RightClick` | Throw, naming `IMauiDriver.InvokeMenuItem` |
| `Hover()` | `Pointer.Hover` | Throw |
| `LongPress()` | verb, else `Pointer.LongPress` | verb, else `GestureUnavailableException` |
| `Swipe()` | verb, else `Pointer.Drag` | verb, else `GestureUnavailableException` |
| `SendKeys(Keys)` | foreground + `Keyboard.Type` | Throw, naming `SetValue` and the SetText verb |
| `SendKeys(Paste)` | SetText verb, else clipboard + Ctrl+V | SetText verb, else throw |
| `SendKeys(SetValue)` | ValuePattern, SetText verb, else typing | ValuePattern, SetText verb, else throw |
| `Clear()` | ValuePattern, ClearText verb, else Ctrl+A, Delete | ValuePattern, ClearText verb, else throw |
| `Submit()` | Submit verb, else Enter | Submit verb, else throw |
| `Focus()` | Focus verb, else foreground + `SetFocus` | Focus verb, else throw. `SupportsFocus` should then report whether the verb is declared, not `true`. |

**Why throw instead of removing the members:** `Click`, `SendKeys`, `Hover` and the rest come
from `Brinell.Core.IElement`, which WPF, WinForms and HTML also implement. Splitting the interface
is a larger change and belongs with `.my/RefactorExtensions`. Throwing now, with the message naming
the quiet route, keeps the change local.

### Step 3: `Brinell.Core`

> **Done 2026-09-14.** `PhysicalInput.QuietByDefault()` and the `_defaultPolicy` field behind it
> are gone; `Policy` now falls back to `Allowed` directly. The remarks on `PhysicalInput`,
> `PhysicalInputPolicy.Allowed` and `ReadExplicitPolicy` say the gate serves WPF and WinForms and
> that MAUI no longer passes through it. `PhysicalInputRefusedException`'s message no longer
> mentions "a quiet stack refuses by default". `PhysicalInputTests` uses a WPF call site in its
> message test. `Brinell.Core.Tests` 16/16; WPF, WinForms and the MAUI UI test project build. No
> MAUI UI run: nothing on the MAUI path reads the default any more.

| Item | Change |
|---|---|
| `PhysicalInput.QuietByDefault()` | **Delete.** Its only caller is the file deleted in step 2. |
| `PhysicalInput`, `PhysicalInputPolicy`, `PhysicalInputRefusedException` | **Keep.** WPF (`FlaUIWpfElement`, `FlaUIWpfDriver`) and WinForms (`FlaUIWinFormsElement`, `FlaUIWinFormsDriver`, `DateTimePicker`) still record through them. Update the remarks so they no longer describe MAUI as the quiet stack. |

### Step 4: `Brinell.Maui` controls, shared with Appium

> **Done 2026-09-14.** `Slider.TrySetValueWithWindowsClick` deleted; `SetValueCore` is now
> RangeValue, then keyboard. The class summary changed, so `Slider.gen.cs` was regenerated with
> the generator CLI (only the summary moved). Remarks updated, no behaviour change, on
> `ClickableControlBase.PressCore`, `FocusableControlBase.FocusCore`/`BlurCore`,
> `Entry.AppendCore`, `RangeControlBase.SetValueCore` and the `SelectorControlBase` click
> fallback: each says the physical route is Android and iOS only and throws on Windows. No other
> `.gen.cs` changed. `Brinell.Maui`, the Windows and mobile UI test projects build; Range tier
> 11 passed, 11 skipped (Stepper), 0 failed.

These call physical-looking APIs on `IMauiElement`, but on Android that means an Appium tap or
`setText`, which is correct. **Do not delete them.** After step 2 they throw on Windows with a
clear message, and nothing in the Windows suite reaches them.

| Site | Fallback | Action |
|---|---|---|
| `Controls/Buttons/ToolbarButton.cs` `ClickCore` | `element.Click()` | Already done in step 1b: the verb on Windows, the click on Appium. |
| `Controls/Base/ClickableControlBase.tpl.cs` `PressCore` | `SendKeys(Keys.Space)` | Keep, and update the remarks to say it is Windows-unsupported. |
| `Controls/Base/FocusableControlBase.tpl.cs` | `Click()` if focus unsupported, `SendKeys(Tab)` if blur unsupported | Keep. |
| `Controls/Text/Entry.tpl.cs` `AppendCore` | `SendKeys(text)` | Keep. |
| `Controls/Base/RangeControlBase.tpl.cs` `SetValueCore` | `Clear()` + `SendKeys` | Keep. |
| `Controls/Base/SelectorControlBase.tpl.cs` | `Click()` on picker, then item | Keep. |
| `Controls/Range/Slider.tpl.cs` `TrySetValueWithWindowsClick` | `windows: click` Appium script | **Delete.** It targets Appium's Windows driver, which Brinell.Maui no longer uses (Windows is FlaUI). On Android it always throws and falls through. `SetValueWithKeyboard` stays for Android. |

Regenerate the `.gen.cs` files for any edited `.tpl.cs` (see the `convert-control` skill).

### Step 5: `Brinell.Maui.Appium`, desktop leftovers

> **Done 2026-09-14.** `ScrollIntoViewWindows` and `PerformSwipeWithActions` deleted; the
> `default:` branches of `ScrollIntoView`, `Swipe`, `LongPress` and `SetValueDirectly` now throw
> `PlatformNotSupportedException` through one helper, `NotAMobilePlatform`. **Added beyond the
> plan:** `AppiumMauiDriver`'s constructor refuses any platform but Android and iOS, so those
> branches are guards rather than routes. Nothing constructs it directly (the factory already
> sends Windows to FlaUI). The read-only `_ => null` branches for `AutomationId`, `Hint`, `Name`
> and `Focused` were left alone. Appium project builds with 0 warnings; both UI test heads build;
> `Brinell.Maui.Tests` 123 passed, 1 skipped. Not run on an Android device.

`AppiumMauiElement` says in its own remarks that "anything here shaped for the desktop is a
mistake rather than a fallback". These `default:` branches only run for a platform other than
Android or iOS, which this driver never gets:

- `ScrollIntoViewWindows()` (`windows: scroll`, JavaScript `scrollIntoView`)
- `PerformSwipeWithActions()` (W3C mouse actions, `window.scrollBy`)
- the `default:` branch of `LongPress` (`Actions.ClickAndHold`)
- the `default:` branch of `SetValueDirectly` (clear and type)

Replace each with `throw new NotSupportedException(...)` naming the platform. This step is
independent of the rest and can be done at any time.

### Step 6: Tests

> **Done 2026-09-14.** Full Windows suite with nothing set: **296 total, 282 passed, 14 skipped,
> 0 failed**, 4 m 5 s. `OccludedScreenshotTests` passed this time, consistent with its step-2
> failure being the desktop, not the code.
>
> Most of the table below was already done in steps 1b and 2. What this step added:
> - **Removed all 26 `PhysicalInput.OverridePolicy(Refused)` scopes** from `DateTimeVerbTests`,
>   `FocusVerbTests`, `NavigationVerbTests`, `ScrollVerbTests`, `SelectionVerbTests`,
>   `TextVerbTests`, `AppRootScopeTests`, `MenuVerbTests`, `ToolbarVerbTests` and
>   `ShellFlyoutVerbTests`, with a script that dropped the `using` line and its braces and
>   dedented the body. No `Brinell.Core.Diagnostics` import remains in the MAUI UI tests.
> - **Rewrote the remarks** that claimed the refused policy as evidence (`FocusVerbTests`,
>   `TextVerbTests` - which also said typing was kept and pointed at a deleted test -
>   `DateTimeVerbTests`, `AppRootScopeTests`, and the one-line summaries elsewhere). They now say
>   passing is the evidence, because the driver has no fallback, and point at
>   `FocusVerbTests.ActionsWithNoSemanticRoute_ThrowInsteadOfUsingRealInput`.
> - **Renamed two tests** whose names claimed the policy:
>   `SelectIndex_WorksWithPhysicalInputRefused` → `SelectIndex_WorksWithoutPhysicalInput`,
>   `Fixture_NavigatesBetweenPagesWithPhysicalInputRefused` →
>   `Fixture_NavigatesBetweenPagesWithoutPhysicalInput`.
> - `PhysicalInputTests` in `Brinell.Core.Tests` needed nothing more; it never referenced
>   `QuietByDefault`.

| File | Change |
|---|---|
| `PhysicalInputTrait.cs` (`PhysicalInputTrait`, `PhysicalInputFactAttribute`) | **Delete.** |
| `Tests/Navigation/AppRootScopeTests.cs` | Change `BackToHub_IsAbsentAtTheHub` and `BackToHub_Click_ReturnsToTheHub` from `[PhysicalInputFact]` to plain `[Fact]`, and remove their `PhysicalInput=Deliberate` traits. After step 1b, `BackToHub.Click()` goes through `InvokeToolbarItem` on Windows. Update the remarks that say it clicks. |
| `MauiFixture.ReturnToHub` | Remove the `PhysicalInput.Used("MauiFixture.ReturnToHub", ...)` marker. Keep `BackToHub.Click()` **only on the Appium path**, because Android and iOS reach it (the class remarks say so). On Windows, a bridge refusal should fail with the collected reasons. |
| `Tests/Background/QuietDefaultTests.cs` | Delete. Replace it with a test that `FlaUIMauiElement.Click` and `SendKeys(Keys)` throw `NotSupportedException` on an element with no declared verb. |
| `Tests/Background/FocusVerbTests.cs:197` | Expect `NotSupportedException` instead of `PhysicalInputRefusedException`. |
| `using (PhysicalInput.OverridePolicy(Refused))` wrappers in `TextVerbTests`, `ScrollVerbTests`, `SelectionVerbTests`, `NavigationVerbTests`, `FocusVerbTests`, `DateTimeVerbTests`, `MenuVerbTests`, `ShellFlyoutVerbTests` | Remove. The guarantee is now structural, so the scope proves nothing. |
| `DesktopLease.cs` | Remove the policy branch. MAUI collections never need the exclusive desktop lease. |
| `Tests/Background/DesktopGateTests.cs` | Delete the three `Allowed_*` tests. Keep or fold `Refused_LetsBothCollectionsInAtOnce` as the only behaviour. |
| `Tests/Background/CollectionParallelismTests.cs:69`, `Tests/Diagnostics/ForegroundWatchdogTests.cs:45` | Remove the `Policy == Allowed` early-outs. |
| `testsnew/Brinell.Core.Tests/PhysicalInputTests.cs` | Keep (it tests the Core type, still used by WPF and WinForms). Remove anything about `QuietByDefault`. |

Expected full run afterwards: **about 298 total, 284 passed, 14 skipped, 0 failed**. From today's
297 / 281 / 16: the two Back-to-Hub tests run instead of skipping (+2 passed), step 1b adds four
toolbar tests (+4), the three `Allowed_*` gate tests are deleted (−3), and `QuietDefaultTests` is
replaced one for one. Re-count after the run rather than trusting this figure.

### Step 7: Guard against regression

A refusal policy used to be the guard. Replace it with a compile-time one: add
`Microsoft.CodeAnalysis.BannedApiAnalyzers` to `Brinell.Maui.FlaUI` with a `BannedSymbols.txt`
listing:

```
T:FlaUI.Core.Input.Mouse
T:FlaUI.Core.Input.Keyboard
T:System.Windows.Forms.Clipboard
M:FlaUI.Core.AutomationElements.AutomationElement.Click(System.Boolean)
M:FlaUI.Core.AutomationElements.AutomationElement.DoubleClick(System.Boolean)
M:FlaUI.Core.AutomationElements.AutomationElement.RightClick(System.Boolean)
M:FlaUI.Core.AutomationElements.AutomationElement.SetForeground
M:FlaUI.Core.AutomationElements.AutomationElement.Focus
```

Check the exact signatures against the FlaUI version in use. `SetForegroundWindow` in
`NativeMethods` stays, because `QuietWindow` uses it to hand the foreground **back** to the user.

### Step 8: Documentation

> **Done 2026-09-14** (step 7, the banned-API analyzer, was skipped and is still open).
> - `docs/architecture/decisions.md`: AD-005 now says MAUI on Windows has no physical input and
>   no switch for it, and keeps the `BRINELL_BACKGROUND_MODE` table for WPF and WinForms only.
>   The heading is unchanged so its anchor links still work. AD-008 names toolbar items, states
>   the bridge is required on Windows, and rule 2 says tests of real input run on the mobile head.
> - `docs/platform-guides/maui.md`: Appium is Android and iOS only; the background section says
>   there is no other mode; the gestures section lists the bridge as required, the toolbar and
>   menu verbs, and the actions with no Windows route (`RightClick`, `Hover`, `Keys`).
> - `docs/run/MAUI.md`: dropped the Appium-for-Windows setup, which the Appium driver now refuses.
> - `docs/guides/troubleshooting.md`: "Physical Input Problems" replaced by a section on the new
>   `NotSupportedException` message; the Appium checklist no longer lists `windows`. No links
>   pointed at the old heading.
> - `docs/architecture/stack.md`: `BRINELL_BACKGROUND_MODE` and `BRINELL_PHYSICAL_INPUT_LOG`
>   marked WPF and WinForms only; `APPIUM_PLATFORM=windows` noted as selecting FlaUI.
> - `AGENTS.md`: the core rule forbids adding physical input to the MAUI FlaUI driver; the
>   expected-skips note drops "tests of real input" and warns about `OccludedScreenshotTests`.
> - `samples/Brinell.Maui.AppSupport/README.md`: the bridge section says it is required on Windows.
> - `.my/extension/plan-the-quiet-run.md` and `physical-input-inventory.md`: historical banners
>   pointing here.

- `AGENTS.md`: remove the "tests of real input (skipped while input is refused)" skip note and any
  `BRINELL_BACKGROUND_MODE` guidance for MAUI.
- `docs/platform-guides/maui.md`, `docs/guides/troubleshooting.md`, `docs/architecture/stack.md`:
  `BRINELL_BACKGROUND_MODE` no longer affects MAUI on Windows. The bridge is required.
- `docs/architecture/decisions.md` AD-008: rule 2 ("is the physical path the thing under test?")
  is answered on the mobile head only. Add a line that the Windows MAUI stack has no physical path.
- `samples/Brinell.Maui.AppSupport/README.md`: say that the bridge is required, not optional.
- `.my/extension/physical-input-inventory.md`, `.my/extension/plan-the-quiet-run.md`: mark as
  historical and point to this document.

### What a user notices

- `BRINELL_BACKGROUND_MODE=0` no longer makes a MAUI Windows run use the mouse and keyboard. It
  still affects WPF and WinForms.
- Nobody can watch a run by bringing the app forward. The watchdog in `QuietWindow` pushes the
  app back whenever its process owns the foreground, whoever caused it, and `WS_EX_NOACTIVATE`
  stops a click from activating it at all. If watching is still wanted, add an explicit opt-out,
  for example `BRINELL_AUT_WATCH=1`, that skips `QuietWindow`. That is a window setting, not
  physical input.
- Calling `Click()`, `Hover()` or `SendKeys()` directly on a Windows element without a declared
  verb throws a message naming the quiet alternative, instead of moving the mouse.

---

## 3. Does Android need physical controls?

**No. Appium handles all of it on the device, and the host's mouse, keyboard and focus are never
involved.**

How the Android path works:

- The test process sends WebDriver HTTP requests to the Appium server on the host.
- Appium forwards them over adb to the **UiAutomator2 server running inside the emulator or
  device**.
- That server injects touch events through Android's own `UiAutomation` API and writes text with
  `setText` or `mobile: replaceElementValue`. `AppiumMauiElement.SetValueDirectly` uses
  `replaceElementValue`, and `LongPress` and `Swipe` use `mobile: longClickGesture` and
  `mobile: swipeGesture`.

None of this goes through Windows input. The emulator window can be covered, minimized or on
another desktop, and the PC stays fully usable during a run. Running the emulator with
`-no-window` should also work, but it has **not been measured on this machine**. Try it before
relying on it in CI.

**Physical-looking calls are fine there.** On Android, `Click()` is an Appium tap and
`SendKeys(Keys)` is text injected into the focused view. They are the normal route, not a
fallback. That is why step 4 keeps the shared control code.

**What Android does need is inside the device, not on the host:**

| Need | Why | Existing handling |
|---|---|---|
| Screen awake and unlocked | UiAutomator2 cannot act on a locked screen | Emulator default. On real devices, keep "stay awake" on. |
| Nothing covering the target | The soft keyboard and the navigation bar swallow taps | `AppiumMauiElement.NudgeClearOfBottomEdge` |
| Native dialogs reachable | A Picker opens an `AlertDialog` outside the page | Not handled. This is the known 0/8 Picker baseline on Android. |
| One run per device | Two sessions fight over the same UI | Same rule as on Windows |
| Healthy emulator | A stuck-offline emulator fails every test | Relaunch with `-no-snapshot` |

**iOS works the same way:** XCUITest runs on the simulator or device, and the Mac's input is not
used.

**One asymmetry to know about.** On Windows the quiet route requires the app to host the bridge.
On Android there is no bridge. Gestures are real touch injected by UiAutomator2, so an
uninstrumented Android app is still fully drivable. `AppiumMauiElement.SupportsGesture` lists Tap
and the four swipes. DoubleTap, Pan and Pinch would need W3C pointer action sequences, which that
driver does not write yet.
