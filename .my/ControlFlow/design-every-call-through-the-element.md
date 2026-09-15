# Control flow: every call goes through `IMauiElement`

**The intended flow.**

```
control object  ──calls──▶  IMauiElement  ──implemented by──▶  FlaUIMauiElement  ──▶  UIA / Brinell bridge
                                                           └──▶  AppiumMauiElement ──▶  UiAutomator2 / XCUITest
```

1. A control object calls an `IMauiElement` member.
2. `FlaUIMauiElement` or `AppiumMauiElement` implements it, using its own driver.
3. That driver automates the app.

The control says **what**. The element decides **how**. The control has no platform branches.

**The question.** Where does the code not follow this flow, and what has to change in
`IMauiElement` so that a control makes the same calls on both backends?

**Short answer.** There are three kinds of exception:

- **A. Controls calling the driver directly: 15 call sites in 6 files.** `ToolbarButton` is one.
  `ShellFlyout`, `Stepper`, `ContentDialog`, `ViewBase` and `Shell` are the others.
- **B. Platform switches in a control: 1 file.** `ShellChrome` switches on `MauiPlatform`, and
  its Android branch makes a 16th driver call.
- **C. `Supports*` branches whose two sides run on different backends.** There are four:
  `SupportsSelectByText`, `SupportsSelectIndex` and `SupportsScrollContent`, plus
  `SupportsDropdown` where the selector base asks it. Every other question a control asks is a
  real route choice and stays.

The fix has two parts:

- **The app itself becomes an element.** A new `IMauiTestContext.AppElement` represents the
  app's window or hierarchy root. Operations on the whole app (flyout, alerts, dialogs, targets
  reached by id) move from `IMauiDriver` to `IMauiElement`, and are called on that element.
- **Four disguised platform switches move into the element.** Selection, selector item reads,
  scroll stepping and the toolbar route become one call each, and every one either performs or
  throws.

Measured by reading the code on 2026-09-15 (`srcnew/Brinell.Maui`, `Brinell.Maui.Extensions`,
`Brinell.Maui.FlaUI`, `Brinell.Maui.Appium`). Nothing was run. This builds on
[../bridge/supports-properties.md](../bridge/supports-properties.md), which already removed 10
`Supports*` members.

---

## 0. The rule this document checks against

A control object may:

- **find elements through its scope.** `MauiScope.FindElement`, `Context.FindElement` and
  `element.FindElements` are finds, and a scope is allowed to reach the driver to perform one;
- **call any `IMauiElement` member** on an element it found.

A control object may not:

- **call `Context.Driver`.**
- **read `MauiPlatform`.**
- **ask a `Supports*` question just to choose a platform.** A question is allowed when it can
  answer both true and false *on the same backend*, and both branches work on that backend. It is
  a platform switch in disguise when the false branch works on only one backend and throws on the
  other, or when one backend always gives the same answer and so only ever runs one branch.

---

## 1. Controls calling the driver (A)

| File | Call | What it does | Why it bypasses the element |
|---|---|---|---|
| [ToolbarButton.cs:83](../../srcnew/Brinell.Maui/Controls/Buttons/ToolbarButton.cs) | `Driver.InvokeToolbarItem(Locator.Value)` | FlaUI asks the app to raise the item through the bridge verb. Appium finds the item again by accessibility id and taps it. | On Windows the item's Invoke pattern reports success and does nothing, so `element.Invoke()` cannot be used. The found element is passed to `EnsureClickableCore` and then ignored. |
| [ShellFlyout.tpl.cs:69-145](../../srcnew/Brinell.Maui/Controls/Navigation/ShellFlyout.tpl.cs) | `Driver.SupportsFlyoutVerbs`, `OpenFlyout`, `CloseFlyout`, `IsFlyoutOpen` | Uses the app's flyout verbs if they are declared, otherwise the chrome route. | A flyout belongs to the app, not to an element. The pane is recreated each time it opens, so there is no stable element to call. |
| [ShellChrome.cs:106](../../srcnew/Brinell.Maui/Controls/Navigation/ShellChrome.cs) | `Driver.NavigateBack()` | Closes the flyout on Android. | It is inside a platform switch, see section 2. |
| [Stepper.tpl.cs:117,127](../../srcnew/Brinell.Maui/Controls/Range/Stepper.tpl.cs) | `Driver.SupportsStateReads(id)`, `Driver.ReadState(id, prop)` | Reads Value, Minimum, Maximum and Increment from the app. | On Windows the Stepper has no tree node. The element the control holds is the `{id}Minus` button, which does not answer for the Stepper. |
| [ContentDialog.cs:30](../../srcnew/Brinell.Maui/Controls/Dialogs/ContentDialog.cs) | `Driver.TryFindActiveDialogRoot()` | Finds the dialog. | A find with no locator: the root is found differently on each platform. |
| [ContentDialog.cs:99](../../srcnew/Brinell.Maui/Controls/Dialogs/ContentDialog.cs) | `Driver.CurrentAlert()` | Reads the alert message from the app. | The alert belongs to the app, not to an element. |
| [ViewBase.tpl.cs:476,530](../../srcnew/Brinell.Maui/Controls/Base/ViewBase.tpl.cs) | `Driver.TryFindByScrollingWithin(ScrollingRoot, Locator)` | On Android, scrolls to find an element; FlaUI returns null. | A find that scrolls. The container can be null, meaning "let the driver pick". |
| [Shell.cs:52,54](../../srcnew/Brinell.Maui/Controls/Navigation/Shell.cs) | `Driver.Platform` | Passes the platform to `ShellTabs` and `ShellFlyout`. | Feeds `ShellChrome`, see section 2. |

**What these have in common.** Only `ToolbarButton` has an element and could have used it. The
others need something **that has no element in the tree**:

- the app itself: the flyout, the alert, the active dialog, going back;
- a target the bridge addresses by id: the Stepper (the same problem already blocks `SwipeView`,
  see step 19 in `.my/extension/steps.md`);
- a find that scrolls.

That gap is the reason for these driver calls. Section 4 closes it by making the app an element.

---

## 2. Platform switches in a control (B)

[ShellChrome.cs](../../srcnew/Brinell.Maui/Controls/Navigation/ShellChrome.cs) is the only
`MauiPlatform` switch in the control objects. It holds two different kinds of knowledge:

| Member | Kind | Where it belongs |
|---|---|---|
| `TabHost`, `Tab`, `FlyoutHost`, `FlyoutItem` | Locators: data for a find | A chrome map supplied by the backend. Not `IMauiElement`, see 4.7. |
| `FlyoutOpener` | A locator used only by `Open` | Inside the backend's `OpenFlyout`. |
| `DismissFlyout` | An operation: Invoke `LightDismiss` on Windows, go back on Android | Inside the backend's `CloseFlyout`. |

`Stepper` also contains knowledge of the Windows tree: the `{id}Minus` and `{id}Plus` buttons,
and the `RepeatButton` class name. It does not branch on platform, and its presses already go
through `element.Invoke()`. It is listed here, but moving it is **not** needed to make the
backends match (see 5.4).

---

## 3. `Supports*` branches still in the controls (C)

These are all the `Supports*` questions that controls still ask, on the element and on the
driver. For each backend, the table shows which branch actually runs.

| Question | Caller | FlaUI | Appium | Verdict |
|---|---|---|---|---|
| `SupportsStateReads` | `Image`, `ProgressBar`, `Picker` ×4, `CollectionObjectBase` | Per element. False reads the UIA tree, which works. | Always false, so it always reads the tree. | **Real.** Keep. |
| Driver `SupportsStateReads(id)` | `Stepper` | Per id | Always false | **Real, but called on the wrong object.** Move to the element (4.4). |
| `SupportsScrollToIndex` | `CollectionObjectBase.TryMaterializeMore` | Per element. False uses ScrollItem, then a Scroll step, both of which work. | Always false: scroll-into-view, then a swipe. | **Real.** Keep. |
| `SupportsInvoke`, `SupportsSelect` | Extensions: `SelectionList`, `GenericBrowser`, `EditableField` | Per pattern. Each branch works where the pattern is present. | Always true, and every branch is a tap. | **Real.** Keep. The callers try each candidate and catch the failure, as they should. |
| `SupportsGesture` | `FlaUIMauiElement.Click` only | n/a | n/a | Not asked by any control. |
| Driver `SupportsFlyoutVerbs` | `ShellFlyout` ×3 | Per app. False takes the chrome route, which works. | Always false: the chrome route. | **Real, but called on the wrong object.** Move into the element, where the choice disappears (4.3). |
| `SupportsSelectByText`, `SupportsSelectIndex` | `SelectorControlBase` | Verb, else dropdown. The third branch, a click, **throws**. | Always false, so it always takes the third branch: a click. | **Disguised platform switch.** Its last branch runs only on Appium (4.5). |
| `SupportsDropdown` in `SelectByText`/`SelectByIndex` | `SelectorControlBase` | A ComboBox is true | **Always false** | **Disguised platform switch** (4.5). |
| `SupportsDropdown` in `GetSelectedText`, `GetItemTexts`, `GetItemElements` | `SelectorControlBase` | True reads the selection pattern and the dropdown; false reads `Text`. | Always false: reads `Text`, or gets `null` for items. | **Disguised platform switch** (4.5). |
| `SupportsDropdown` in the flyout members | `Picker.OpenFlyout`, `CloseFlyout`, `IsFlyoutOpen`, `GetDropdownItemTexts` | Per element | Always false | **Real**: these members are about the dropdown itself. Keep. |
| `SupportsScrollContent` | `ScrollHelper.Step`, `CollectionObjectBase.ScrollToTop` | Scroll pattern, else a swipe, which is the bridge's swipe verb and throws when that is not declared. | Always false: a swipe calculated in `ScrollHelper`. | **Disguised platform switch.** The swipe geometry is Appium's route, but it lives in the shared code (4.6). |

### Found along the way: the selector's third branch cannot work

On Appium, `SelectByTextCore` and `SelectByIndexCore` reach this code:

```csharp
element.Click();
var defaultItems = GetItemElementsCore(element);   // base returns null; Picker does not override
```

`GetItemElementsCore` returns `null` when `SupportsDropdown` is false, and Picker does not
override it. So `Picker.SelectByText` and `SelectByIndex` on Android **always** throw "not found"
or "out of range", after tapping the picker open.

This is a likely cause of the recorded baseline "Picker tests are 0/8 on Android, one root
failure". That has not been verified. Hiding exactly this kind of failure is what a disguised
`Supports*` branch does: the route looks shared, but only one backend runs it, and nobody checked
that it works there.

---

## 4. Design: what changes in `IMauiElement`

### 4.1 Prerequisite: the app is an element

```csharp
public interface IMauiTestContext
{
    /// The app's root: FlaUI's application window, Appium's hierarchy root. Found, like any element.
    IMauiElement AppElement { get; }
}
```

- **FlaUI** already has `FlaUIMauiDriver.RootElement`. `AppElement` wraps it in a
  `FlaUIMauiElement`.
- **Appium** returns the root of the hierarchy.

Getting `AppElement` counts as a find, so the rule in section 0 allows it. Every app-level
operation below is then an element call made on it. What a control would write:

```csharp
Context.AppElement.OpenFlyout();
```

The members in 4.3 and 4.4 are **only meaningful on the app element**. On any other element they
throw `NotSupportedException`, which is the interface default. The alternative would be a second
interface, such as `IMauiAppElement`. That is tidier, but it is a second route for a control to
learn, and the goal here is one route.

### 4.2 Toolbar items

```csharp
/// Raises this element as a toolbar item. Performs or throws.
/// <param name="automationId">The id from the app's markup: on Windows, the element does not carry it reliably.</param>
void InvokeToolbarItem(string automationId);
```

| FlaUI | Appium |
|---|---|
| `_driver.InvokeToolbarItem(automationId)`, which is today's bridge verb | `Click()` on this element, which has already been found |

`ToolbarButton.ClickCore` becomes `element.InvokeToolbarItem(Locator.Value)`.

`AppiumMauiDriver.InvokeToolbarItem` repeated a find that the control had already done, so it can
be deleted. `IMauiDriver.InvokeToolbarItem` becomes internal to FlaUI once
`ToolbarVerbTests` (3 uses) calls the element instead.

### 4.3 Shell flyout, on `AppElement`

```csharp
void OpenFlyout();      // performs or throws; already open is success
void CloseFlyout();     // performs or throws; already shut is success
bool? IsFlyoutOpen { get; }   // null: this platform cannot say without counting items
```

| Member | FlaUI | Appium |
|---|---|---|
| `OpenFlyout` | The `OpenFlyout` verb when declared. Otherwise find `Name="Open Navigation"` and Invoke it. | Find accessibility id `"Open navigation drawer"` and tap it. |
| `CloseFlyout` | The `CloseFlyout` verb when declared. Otherwise find `LightDismiss` and Invoke it. | `_driver.Navigate().Back()` |
| `IsFlyoutOpen` | The `GetState("FlyoutIsPresented")` answer when declared, otherwise null. | null |

`ShellFlyout` then has one path on every backend:

```csharp
public ShellFlyout<TParent> Open(int? timeoutMs = null)
{
    if (IsOpen() == true) return Self;
    Context.AppElement.OpenFlyout();
    InvalidateCache();
    WaitOpen(true, timeoutMs);
    return Self;
}

protected virtual bool? IsOpenCore(IMauiElement? element)
    => Context.AppElement.IsFlyoutOpen
       ?? (element != null && element.Visible && TryGetItemRoots().Count > 0);
```

This removes `SupportsFlyoutVerbs`, `ShellChrome.FlyoutOpener`, `ShellChrome.DismissFlyout`, the
`Activate` helper and `_platform` from the flyout.

Using null for "cannot say" follows the existing pattern of `Checked` and `RangeValue`. It is
**not** a hidden `Supports` question: the fallback, counting the items, works on both backends.

### 4.4 Targets reached by id, alerts and dialogs, on `AppElement`

```csharp
/// An element the app declared by AutomationId, whether or not the platform's tree shows it.
IMauiElement? TryFindDeclared(string automationId);

/// What the alert on screen is asking, or null when none is open or the app does not report it.
AlertContents? ReadAlert();

/// The root of the dialog on screen, or null.
IMauiElement? TryFindActiveDialog();
```

| Member | FlaUI | Appium |
|---|---|---|
| `TryFindDeclared` | A bridge-addressed element: `SupportsStateReads`, `ReadState`, `SupportsGesture` and `PerformGesture` route by id through `BridgeVerbRunner`; everything else keeps the interface default. Returns null when no bridge target has that id. | A find by resource id, then by accessibility id, returning the real node. |
| `ReadAlert` | Today's `CurrentAlert()` | null |
| `TryFindActiveDialog` | Today's `TryFindActiveDialogRoot()` | Today's `TryFindActiveDialogRoot()` |

- **`Stepper`** calls `Context.AppElement.TryFindDeclared(id)` and then asks that element
  `SupportsStateReads` and `ReadState`, the same two calls that `Image` and `Picker` already make.
  On Appium the element has no state reads, so the base range reads run, as they do today.
- **`ContentDialog`** calls `Context.AppElement.TryFindActiveDialog()` and
  `Context.AppElement.ReadAlert()`.
- **`SwipeView`** gets the same route for free. Resolving its element through `TryFindDeclared`
  lets its gesture members run on Windows, which closes the gap described in its own remarks.

A bridge-addressed element that answers only four members could be mistaken for a full element.
Its `Visible`, `Text` and bounds must **throw** rather than return defaults, so that it cannot
pass a visibility check by accident.

### 4.5 Selection

**Remove** `SupportsSelectByText` and `SupportsSelectIndex`. `SelectByText` and `SelectIndex`
already perform or throw, and they absorb the choice of route:

| Member | FlaUI | Appium |
|---|---|---|
| `SelectByText(text)` | The verb when declared. Otherwise, when there is ExpandCollapse: open, `Select()` the matching item, close. Otherwise throw, naming both routes. | Tap to open. Find the item whose text matches, tap it. |
| `SelectIndex(i)` | Same as above, by position | Tap to open. Tap the i-th item. **Needs writing**: see below. |
| `SelectedItemText` | Selection pattern when there is a dropdown, otherwise `Text` | `Text` |

**Add** one read, which replaces `GetItemElementsCore`:

```csharp
/// The texts of the selector's items, in order. Performs or throws.
IReadOnlyList<string> ReadItemTexts();
```

| FlaUI | Appium |
|---|---|
| Open the dropdown, read the texts while the items are live, close it. This is today's `WithDropdownOpen`. | Throw `NotSupportedException` until the Android picker dialog has been mapped. That is still better than today's silent `null`. |

`SelectorControlBase` then contains no `Supports*` question at all:

```csharp
protected virtual void SelectByTextCore(IMauiElement element, string? text, int? timeoutMs = null)
{
    if (text == null) return;
    element.SelectByText(text);
}

protected virtual string? GetSelectedTextCore(IMauiElement? element) => element?.SelectedItemText;

protected virtual IReadOnlyList<string>? GetItemTextsCore(IMauiElement? element) => element?.ReadItemTexts();
```

- `SelectFromDropdown` and `WithDropdownOpen` move into `FlaUIMauiElement`.
- `Picker`'s `SupportsStateReads` overrides stay. They are real (see section 3).
- `SupportsDropdown`, `OpenDropdown`, `CloseDropdown` and `ReadDropdownItems` stay, but only
  `Picker`'s flyout members use them.

The Appium item route is exactly what is broken today (the finding in section 3). Moving it does
not fix it by itself. It gives it one place to be written, and on Android that has to be tested
on a device.

### 4.6 Scroll stepping

**Replace** `SupportsScrollContent` plus `bool ScrollContent(...)` with one call that returns
what can actually be known:

```csharp
public enum ScrollStep
{
    Moved,        // the platform confirms the content moved
    NotMoved,     // the platform confirms it did not: the content was at that end
    Unconfirmed   // a swipe was performed; nothing reports whether it moved
}

/// Scrolls one step. Performs or throws.
ScrollStep ScrollContent(int verticalSteps, int horizontalSteps = 0);
```

| FlaUI | Appium |
|---|---|
| Scroll pattern on the element or an ancestor, returning `Moved` or `NotMoved`. Otherwise the bridge swipe verb, returning `Unconfirmed`. Otherwise throw. | The swipe that `ScrollHelper.Swipe` calculates today, with its insets, returning `Unconfirmed`. An element too short to swipe returns `NotMoved`, which is what `false` means to callers today. |

```csharp
// CollectionObjectBase.ScrollToTop
var step = target.ScrollContent(-1);
while (step == ScrollStep.Moved) step = target.ScrollContent(-1);

// ScrollHelper.Step
return element.ScrollContent(forward ? 1 : -1) != ScrollStep.NotMoved;
```

- `ScrollToTop` behaves as today on both backends: FlaUI loops until the content stops, Appium
  takes one step.
- `ScrollHelper.Swipe`, `EdgeInset` and `MinimumSwipeHeight` move into `AppiumMauiElement`.

### 4.7 Shell chrome locators (not in `IMauiElement`)

`TabHost`, `Tab`, `FlyoutHost` and `FlyoutItem` are locators, not operations. They cannot become
element calls, because `CollectionObjectBase` takes them in its constructor. Instead:

```csharp
public sealed record ShellChromeLocators(Locator TabHost, Locator Tab, Locator FlyoutHost, Locator FlyoutItem);

public interface IMauiTestContext
{
    ShellChromeLocators ShellChrome { get; }   // supplied by the backend's driver
}
```

- Each backend fills in its own values: today's Windows branch in FlaUI, today's Android branch
  in Appium.
- `Shell.cs` passes `scope.Context.ShellChrome` instead of `Driver.Platform`.
- `ShellChrome.cs` is deleted, and no `MauiPlatform` is left in the control objects.

This is the one part of the design that goes through the context instead of an element. It is
data for a find, so the rule in section 0 allows it.

### 4.8 `TryFindByScrolling` on the element

```csharp
/// Finds a descendant by scrolling this element until it enters the tree. Null when scrolling does not reach it.
IMauiElement? TryFindByScrolling(Locator locator);
```

| FlaUI | Appium |
|---|---|
| null. Windows keeps off-screen elements in the tree, so there is nothing to scroll for. | Today's `UiScrollable` search, rooted at this element. When this element is the `AppElement`, search the whole screen, which is today's "container is null". |

```csharp
// ViewBase
return (_mauiScope.ScrollingRoot ?? Context.AppElement).TryFindByScrolling(Locator);
```

---

## 5. Summary

### 5.1 `IMauiElement` changes

| Change | Members |
|---|---|
| **Add** | `InvokeToolbarItem(id)`, `OpenFlyout()`, `CloseFlyout()`, `IsFlyoutOpen`, `TryFindDeclared(id)`, `ReadAlert()`, `TryFindActiveDialog()`, `ReadItemTexts()`, `TryFindByScrolling(locator)` |
| **Change** | `ScrollContent` returns `ScrollStep` instead of `bool` |
| **Implement on Appium** (today it keeps the default) | `SelectByText`, `SelectIndex`, `SelectedItemText` |
| **Remove** | `SupportsSelectByText`, `SupportsSelectIndex`, `SupportsScrollContent` |
| **Keep** | `SupportsStateReads`, `SupportsScrollToIndex`, `SupportsInvoke`, `SupportsSelect`, `SupportsGesture`, `SupportsDropdown` (Picker's flyout members only) |

Plus two members on `IMauiTestContext`: `AppElement` and `ShellChrome`.

### 5.2 `IMauiDriver` members that controls stop calling

`InvokeToolbarItem`, `OpenFlyout`, `CloseFlyout`, `IsFlyoutOpen`, `SupportsFlyoutVerbs`,
`CurrentAlert`, `TryFindActiveDialogRoot`, `TryFindByScrollingWithin`, `SupportsStateReads(id)`,
`ReadState(id, …)`, `Platform` (from controls only).

None of these can simply be deleted yet. **UI tests call them directly, 27 times in 6 files:**

- `ToolbarVerbTests`
- `MenuVerbTests`
- `AlertReadTests`
- `GestureVerbTests`
- `GestureAddressabilityTests`
- `GestureBridgeTests`

Move those tests onto the element members first; the driver members can then become internal to
their backend.

`NavigateBack`, `NavigationDepth`, `IsIdle` and `CurrentRoute` are fixture operations, not
control operations, and stay on the driver. `InvokeMenuItem` is not called by any control.

### 5.3 What changes on Windows

Nothing a test would see. Every FlaUI route above is the code that runs today, moved from the
control or the driver into `FlaUIMauiElement`. The error messages change where they are raised,
not what they say.

### 5.4 Deliberately left out

- **Stepper's `Minus`/`Plus`/`RepeatButton` knowledge.** The presses already go through
  `element.Invoke()` on both backends. Moving the part resolution into the backends would take a
  new `Increment`/`Decrement` pair on the element, for one control. Worth doing if a second
  control needs it.
- **`ClickableControlBase.PressCore`**, which calls `SendKeys(Keys.Space)`. It follows the flow,
  and it throws on Windows as documented.

---

## 6. Order of work

Each step leaves the suite green on Windows. Run the smallest tier that can prove the step wrong.

1. **Toolbar (4.2).** Verify with `ToolbarVerbTests`.
2. **`AppElement`, alerts and dialogs (4.1, 4.4 without `TryFindDeclared`).** Verify with
   `Tests.Dialogs`.
3. **Flyout (4.3) and chrome locators (4.7).** Verify with `Tests.Navigation`, the Shell tests.
4. **Scroll stepping (4.6) and `TryFindByScrolling` (4.8).** Verify with `Tests.Collection` and
   the scroll tests.
5. **`TryFindDeclared` (4.4)**, then `Stepper` on top of it. Verify with `Tests.Range`.
   `SwipeView` on Windows becomes possible as a follow-up.
6. **Selection (4.5).** Verify with `Tests.Selection` on Windows. **The Appium half needs an
   Android device**, and the Picker baseline there is already 0/8.
7. **Move the UI tests off the driver members (5.2)**, then make those members internal.

Steps 1 to 5 cannot be checked on Android from here; their Appium members are small and will only
be compiled. Step 6 is the one where Android behaviour is supposed to change.

---

## 7. Stale comments found while reading

- **`ClickableControlBase.ClickCore`** says "`ToolbarButton` clicks". It calls the driver.
- **`FlaUIMauiElement.Invoke`** says "a MAUI `ToolbarItem` is the known case - the control says
  `Click` instead". `Click` throws on Windows without the Tap verb, and the control does not say
  `Click`.
- **`ShellFlyout.Activate`** is documented as "falling back to a click". It only calls
  `Invoke()`.
- **`SelectorControlBase`**'s comments say "Uses FlaUI ExpandCollapse pattern", which puts
  Windows vocabulary into the cross-platform control.

---

## 8. Done 2026-09-15

All of sections 4.1 to 4.8 were implemented in one pass, together with step 7 (moving the tests
off the driver).

### What changed

| Area | Now |
|---|---|
| `IMauiTestContext` | Adds `AppElement` and `ShellChrome`. Both are forwarded from new `IMauiDriver` members of the same names. |
| `IMauiElement`, added | `InvokeToolbarItem`, `OpenFlyout`, `CloseFlyout`, `IsFlyoutOpen`, `ReadAlert`, `TryFindActiveDialog`, `TryFindDeclared`, `ReadItemTexts`, `TryFindByScrolling` |
| `IMauiElement`, changed | `ScrollContent` returns `ScrollStep` (`Moved`, `NotMoved`, `Unconfirmed`) |
| `IMauiElement`, removed | `SupportsSelectIndex`, `SupportsSelectByText`, `SupportsScrollContent` |
| `IMauiDriver`, removed | `InvokeToolbarItem`, `OpenFlyout`, `CloseFlyout`, `IsFlyoutOpen`, `SupportsFlyoutVerbs`, `CurrentAlert`, `TryFindActiveDialogRoot`, `TryFindByScrollingWithin`, `SupportsStateReads(id)`, `ReadState(id, …)`. Each backend still has its own version, now `internal`, and its element calls it. |
| Controls | No `Context.Driver` call and no `MauiPlatform` is left in `Brinell.Maui/Controls` or `Containers`. `ShellChrome.cs` is deleted. `ShellTabs` and `ShellFlyout` take a `ShellChromeLocators` instead of a platform. |
| FlaUI | `FlaUIMauiElement.ForApp` stands for the window and reads `RootElement` afresh each time. New `FlaUIDeclaredElement` answers state reads and gestures by id; every other member throws. The dropdown selection route and `WithDropdownOpen` moved in from `SelectorControlBase`. |
| Appium | `AppiumMauiElement.ForApp` finds a root node only when a member needs one. It gains the swipe geometry from `ScrollHelper`, the Shell opener and back, and a picker route for Android. |

### Where the implementation departs from the design

- **`ReadItemTexts` returns `IReadOnlyList<string>?`, not a list that throws.** Today a selector
  that cannot list its items answers null to `GetItemTexts`/`GetItemCount`, so null keeps that
  behaviour on both backends, the same convention as `Checked` and `RangeValue`.
- **`InvokeToolbarItem` also works on `AppElement`.** `ToolbarVerbTests` raise items by id with
  nothing found first, including an id that does not exist. On FlaUI any element sends the verb.
  On Appium, the app element finds the item by accessibility id and taps it; any other element
  just taps itself.
- **The app-level members throw when called on an ordinary element** (`RequireApp`). That is what
  4.1 said; this note records that it is enforced on both backends, not only by the interface
  default.
- **FlaUI keeps the 40 px minimum height** before it falls back to a swipe verb, so a short element
  with no Scroll pattern still answers `NotMoved` instead of throwing, as `ScrollHelper` did.
- **`SwipeView` was not moved onto `TryFindDeclared`.** That remains the follow-up noted in 4.4.
- **`PerformGesture(id, …)`, `SupportsGesture(id, …)` and `InvokeMenuItem` stay on `IMauiDriver`.**
  No control calls them, and the gesture tests use them.

### Tests

- **Unit tests updated:** `ContentDialogControlTests` now mocks `AppElement.TryFindActiveDialog`.
  `ScrollLookupTests` verifies `TryFindByScrolling` on the app element, or on the scroll view.
- **UI tests updated:**
  - `ToolbarVerbTests`, `AlertReadTests` and `ShellFlyoutVerbTests` call `Context.AppElement`.
  - `SelectionVerbTests.Pickers_OfferTheSemanticRoute` now asserts only `SupportsStateReads`. A
    silent fallback to the dropdown is still caught by the refusal tests: the app refuses with a
    `BrinellException` that gives its reason, and the dropdown route throws a different exception.

### Verified

- **Builds:** the solution, `Brinell.Maui.UITests`, `.UITests.Mobile`, `Brinell.Maui.Uat.Tests` and
  `Brinell.Presenter.Uat.Tests`.
- **Generator:** re-running it on the five edited templates changed only the class doc of
  `SelectorControlBase.gen.cs`.
- **Maui unit tests:** 123 passed, 1 skipped, the same as before.
- **Full Windows UI suite:** **296 tests, 295 passed, 1 skipped (stress), 0 failed**, 3 min 27 s.

### Not verified

- **Android and iOS were not run.** The new Appium members are only compiled:
  - the Android picker route;
  - the app root node (`/hierarchy/*[1]`, `XCUIElementTypeApplication`);
  - `TryFindDeclared` by resource id and then by accessibility id.
- **Windows Shell flyout without the verbs.** The sample Shell declares the verbs, so the chrome
  fallback (find the opener or `LightDismiss`, then Invoke) did not run in the suite.
