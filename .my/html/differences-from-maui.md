# Differences from the MAUI design

The MAUI stack drives WinUI/Android through UIA and Appium: elements are handles that go stale,
readiness signals live on control properties, and every attempt makes a fresh lookup. Playwright
is different: a `Locator` is a query, actions auto-wait, and the DOM has its own idea of what
"exists" and "detached" mean. The design in `design.md` follows MAUI's rules and shapes, and
takes these deltas.

Each item numbers where it appears in `design.md` and cites the MAUI decision it departs from.

## 1. What "stale" means

MAUI: `IUIAutomationElement` and its Appium equivalents are handles that go stale when the app
retires the element. The driver raises a specific exception, which `Live<T>` maps.

Playwright: `ILocator` re-evaluates on every use, so it does not go stale in the MAUI sense. An
`IElementHandle` (which the Html element wraps in some paths) can become detached, and a
`Locator` that matched N > 0 elements can match zero mid-call.

**Design.md decision:** `StaleElementException` is raised when:

1. A Playwright call throws a `PlaywrightException` whose message names element detachment
   (`Element is not attached to the DOM`).
2. A `Locator.Count` that returned `n > 0` for the current step's element index returns 0 on a
   re-read against the same `InstanceKey`.
3. An `EvaluateHandle` result is disposed by the page (Playwright throws
   `JavaScript handle is disposed`).

Anything else stays as its concrete type or is re-raised. No catch-alls (R5).

## 2. `InstanceKey` in Playwright

MAUI: `InstanceKey` is the UIA runtime id, read through `Live` so the read itself doubles as the
alive check (R6).

Playwright: there is no runtime id. Three candidates:

| Option | How | Cost | Robust to |
| --- | --- | --- | --- |
| a. `EvaluateHandle`-based identity | Keep a `JSHandle` per row and compare with `evaluate((a, b) => a === b, other)` | One handle per element kept alive; a page-level cost | Recycling, re-render (new node = new key), navigation (all keys void, `AppUnavailableException`) |
| b. Synthetic attribute | The framework writes `data-brinell-key="<guid>"` on the element the first time it is seen | One `evaluate` call on first access; robust | Not robust to a component that re-renders and does not copy the attribute |
| c. Generation counter | Framework keeps a counter and re-reads the DOM path each attempt | Cheap | Brittle: any DOM shuffle changes it |

**Design.md decision:** **(a)**, backed by (b) for scopes that pin their identity (an item in a
virtualized list). The row scope writes a `data-brinell-key` once (through the sample host's
`data-item-key` attribute if it is present, otherwise its own) and the alive check compares the
handle identity; this way a `<Virtualize>` recycle looks like a `StaleElementException`, not a
false-pass.

Every framework call that reads `InstanceKey` goes through `Live<T>`, and a `TargetClosedError`
raises `AppUnavailableException` from there without any per-call catch.

## 3. Actions do not auto-wait inside a call

MAUI's UIA and Appium drivers do not auto-wait; the design assumes an action is a single
attempt.

Playwright's `Locator.ClickAsync` auto-waits for actionability (visible, enabled, stable,
receives events). Inside a `ControlCall.Run` this is a double budget: the calls layer already
waited for scope readiness and visibility.

**Design.md decision (?A5):** Every Playwright call inside a Core method sets `Timeout = 1`
(Playwright's minimum). The action fails at once if not actionable. The failure is either a
`StaleElementException` (detached) or an `ElementNotReadyException` (not clickable) or an
`AppUnavailableException` (target closed); the poller decides whether to retry the phase.

This matters for R0: with Playwright's default timeout of 30 s, a `Click` on a permanently
disabled button waits 30 s inside phase 2, then throws. The test author's `timeoutMs: 2000`
would be silently overrun. Setting `Timeout = 1` ties the wait back to the caller's budget.

## 4. Frames are scopes, not new contexts

MAUI: no analog. Every scope is in the same driver.

Playwright: `IFrame` is a separate document. Today `Brinell.Html` handles it through
`PlaywrightTestContext.ForFrame(urlPattern)`, which returns a whole new context sharing the same
page.

**Design.md decision (?A6):** A frame becomes a `FrameScope : IHtmlElementScope`. Its root is
the frame body; child lookups scope to the frame. A cross-origin frame's root is the frame
element in the outer document, with the lookup delegated to Playwright's `FrameLocator`.

Consequences:

- The `FrameScope` sits in the scope chain and can defer a call while its frame is still
  loading (`ScopeReadiness.NotLoaded`).
- The current `PlaywrightTestContext.ForFrame` and its private `IFrame? _frame` field go away.
- A test that used `ForFrame` and expected an `IHtmlTestContext` back gets a `FrameScope`
  instead. The generator does not change, but the page objects declared on the frame do (their
  base scope goes from `IHtmlTestContext` to `IHtmlElementScope`).

## 5. Item keys in a virtualized list

MAUI: `<CollectionView>` gives back a control with a recycling contract the drivers can read.

Blazor's `<Virtualize>` recycles DOM nodes with no built-in test id. Without an explicit key an
item root looks the same before and after a scroll.

**Design.md decision (7.6):** The Blazor sample project puts `data-item-key="@item.Id"` on every
virtualized row (`sample-app.md` item 4). The `ItemKey` source-of-truth order is:

1. `data-item-key` if present (`Logical`).
2. `data-testid` if present (`AutomationId`).
3. DOM handle identity (`DomNode`).
4. Position, with a `Warning` on first use (`Position`).

An `ItemObjectBase.ProbeReadiness` that finds a different key at the same index returns
`ItemChanged`. A test asking a recycled row to `Click Delete` fails with "row 3 now shows another
item", never presses another item's button, and is R0-compliant.

Tests that only need a fixed list can stay on positions.

## 6. Busy signal for Blazor

MAUI: `Application.MainPage.IsBusy` or a control-provided flag.

Blazor Server: a component-level `_isLoading` field flips during a SignalR round-trip, but is not
observable from outside without a DOM attribute.

**Design.md decision (?A8, 7.2):** A one-line JavaScript hook in the sample host writes
`data-busy="true"` on `<body>` before a Blazor invoke and clears it after
(`Blazor.registerCustomEventType` or `dotnet.invokeMethodAsync` interception; the exact hook is a
sample-app implementation detail, `sample-app.md` item 3). `HtmlPageObjectBase.ProbeReadiness`
reads `data-busy` through the driver's `PageRoot.GetAttribute`. This mirrors what MAUI's page
`IsBusy` gate does.

Alternatives considered:

- **Read Blazor's SignalR client state.** Too invasive; not stable across Blazor versions.
- **Absence of a `<div class="loading">`.** Only works when the app has one; the framework
  cannot assume it.

## 7. `PageLoadException` retirement

MAUI: `PageLoadException` is thrown by Core's `PageObjectBase`; MAUI's step 5 replaced it with
`ScopeNotReadyException`, and 4 tests were updated.

Html: `HtmlPageObjectBase.AssertLoaded` and `AssertTitle` throw `Core.Exceptions.PageLoadException`
today. Step 5 of this plan replaces them with `ScopeNotReadyException`. `Brinell.Html.Tests`,
`Brinell.Html.Uat.Tests`, `Brinell.Blazor.Tests`, `Brinell.Blazor.Uat.Tests` and
`Brinell.Html.UITests` are grep-searched for `PageLoadException` and updated. The exception type
in Core stays as-is: it is used by other stacks (WPF, WinForms, Stride) that have not moved down
yet.

## 8. The `IHtmlAsync*` shim

MAUI: no async facade; every method is sync.

Html: has `IAsyncHtmlElement`, `IHtmlAsyncControlObject<TScope>` and one shim per capability
that wraps `Task.FromResult(sync())`. Not a real async surface; a calling-style adapter.

**Design.md decision (4.6, ?A1):** Kept in step 1, revisited in step 9. If no consumer uses the
async surface beyond internal tests, dropped. A real async Html driver (a `Brinell.Html.Async`
project that keeps Playwright's async model instead of blocking on `.GetAwaiter().GetResult()`)
is a separate project, out of scope here.

## 9. What Html does not need from MAUI

For completeness. These MAUI concepts have no Html analog and do not move over:

- **`ShellChromeLocators`.** No shell chrome; the browser toolbar is not the app.
- **`GestureAutomation` and the bridge verbs.** No custom gestures; Playwright covers what a
  test needs (`Hover`, `Swipe` if ever needed).
- **`BRINELL_ALLOW_POINTER_INPUT`.** No physical input gating.
- **`Platform`** as a first-class enum. Html has one platform (a browser). If more than one
  Playwright browser needs branching, it is an option on the driver, not part of the interface.
- **`MediaElement.Play` bridge verbs.** Media playback in the browser is a plain DOM action.
