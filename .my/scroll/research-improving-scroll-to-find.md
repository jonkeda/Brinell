# Research: improving scroll-to-find

## The problem, stated precisely

Brinell has to answer "give me element X" when X is not on screen. Two very different situations
hide behind that one sentence:

| | Windows (UIA / FlaUI) | Android (UiAutomator2) |
|---|---|---|
| Element scrolled out of view | **In the tree**, `IsOffscreen=true`, zero rect | **Not in the tree at all** |
| Can you find its scrollable ancestor? | Yes — walk `Parent` until `Patterns.Scroll` | **No** — there is no node to walk up from |

That asymmetry is the whole difficulty. On Windows we can ask the element which container holds
it. On Android we cannot, because the question presupposes the answer: the element is missing
*because* the container has not scrolled to it.

So on Android the container has to come from somewhere other than the element.

## What we do today

| Platform | Where | Mechanism |
|---|---|---|
| Windows | `FlaUIMauiElement.ScrollIntoView` | `ScrollItemPattern.ScrollIntoView()`, else walk ancestors for `Patterns.Scroll` and scroll by geometry, else by percent until `!IsOffscreen` |
| Android | `AppiumMauiElement.ScrollIntoView` | `mobile: scrollGesture` over a screen-sized rect, looping while `canScrollMore` |
| Android | `MauiTestContext.TryFindWithUiScrollable` | `new UiScrollable(new UiSelector().scrollable(true).instance(0)).scrollIntoView(<matcher>)` |
| iOS | `AppiumMauiElement.ScrollIntoView` | `mobile: scroll` with `toVisible: true` |

The Windows path is element-directed and structurally right. The Android *find* path guesses:
`instance(0)` is "the first scrollable container on the page", which is an assumption, not a
lookup.

## Evidence

`Tests/Scroll/ScrollTests.cs` — added because scrolling had only ever been tested incidentally —
passes **7/7 on Windows** and fails **0/7 on Android**, all before reaching the page under test:

```
ElementNotFoundException : Element not found with locator: AutomationId:Open_Scroll
```

The hub's own list scrolls, and the new entry sits below the fold. On the hub, `instance(0)` is
the outer `PageHubScroll`, while the `CollectionView` inside it is what actually scrolls.

**But that diagnosis is unproven.** Trying containers `instance(0..2)` in turn was implemented and
measured: it did **not** find the element, and made each failed lookup roughly ten times slower
(5 s to 52 s per test). It was reverted. So either the right container is beyond index 2, or the
item is virtualized and never realized, or the matcher is wrong for hub entries. **Establish this
before choosing a fix.**

## What the platforms actually offer

### Android — UiAutomator2

- **`UiScrollable(...).scrollIntoView(selector)`** — what we use. The outer `UiSelector` chooses
  the container, so the fix for container choice lives here. It also has `setMaxSearchSwipes` and
  `setAsVerticalList`, neither of which we set.
- **`mobile: scroll`** — takes `elementId` (container), `strategy`, `selector`, `maxSwipes`.
  Reads as exactly what we want, **but**
  [appium#15392](https://github.com/appium/appium/issues/15392) reports it ignores `elementId` and
  scrolls the first available scrollable — the same bug we are trying to avoid. Labelled upstream,
  still open. Verify before relying on it.
- **`mobile: scrollGesture`** — takes either `elementId` **or** an explicit
  `left/top/width/height` rect, plus `direction` and `percent`, and returns `canScrollMore`. This
  one genuinely scrolls a chosen container. We already use it, but only with a screen-sized rect,
  never with a container.

### iOS — XCUITest

- **`mobile: scroll`** with `element` (the container) plus one of `toVisible`, `name`
  (accessibility id) or `predicateString`. Container-directed by design — closest to what we
  want, and untested here since there is no macOS runner.

### Windows — UIA via FlaUI 5.0

- `ScrollItemPattern.ScrollIntoView()` and `ScrollPattern` — in use.
- **`ItemContainerPattern.FindItemByProperty` + `VirtualizedItemPattern.Realize()`** — the UIA
  answer to virtualization: find an item that is *not* in the tree and force the provider to
  create it. FlaUI ships `FlaUI.Core.Tools.ItemRealizer` for this. We use none of it. Not needed
  for our current failures, since Windows keeps off-screen elements, but it is the mechanism if a
  WinUI list ever virtualizes rows away.

## Options

### A. Scope-directed scrolling — recommended

Brinell already holds the missing information. A page object knows its root
(`AutomationId:{Name}`, now on the root `ScrollView`), and `CollectionObjectBase` resolves and
caches a container root. The scope can therefore *name* the container to scroll, instead of the
driver guessing.

Shape: let a scope optionally declare its scroll container, and have the Android find path pass
that container to `mobile: scrollGesture` as `elementId`, or into the `UiScrollable` outer
selector as `resourceId(...)` instead of `instance(0)`.

- Fixes container choice by construction rather than by search.
- Costs an interface addition on the scope and a per-platform plumb-through.
- The same idea works on iOS (`mobile: scroll` `element`) and is a no-op on Windows.
- Risk: a scope that declares nothing still needs today's fallback, so this adds a path rather
  than replacing one.

### B. Try every scrollable container

**Rejected on evidence.** Implemented, measured, reverted: it did not fix the hub, and made every
failed lookup roughly ten times slower. Recorded here so it is not tried again without new
information.

### C. Bounded gesture loop with `canScrollMore`

Scroll the container in steps, re-querying after each, stopping when `canScrollMore` is false.
`AppiumMauiElement.ScrollIntoView` already does this for an element it holds; the *find* path does
not. More predictable than `UiScrollable`, and it fails as "scrolled to the end, still absent"
rather than as an opaque empty result.

- Costs one round trip per step; needs a swipe budget to stay bounded.
- Pairs naturally with A: A picks the container, C drives it.

### D. Set `setMaxSearchSwipes` on the existing query

A one-line change to the current `UiScrollable` query. The cheapest possible experiment, worth
running purely to learn whether the hub failure is a search-budget problem before building
anything larger.

### E. Make the app declare its scroll containers

Give every scrollable container an `AutomationId` — done for three page roots already. Cheap, and
it is what makes option A possible at all. It is also a testability requirement worth stating
openly: a container nobody can name is a container automation has to guess at.

## Recommended sequence

1. **Diagnose the hub first.** Dump the Android tree for the hub and establish why `Open_Scroll`
   is unreachable: wrong container, virtualized item, or wrong matcher. Everything below depends
   on the answer, and option B failing means we do not yet know.
2. **Try D** — one line, and it tells us whether the search budget is the limit.
3. **Build A**, with C as the mechanism underneath it.
4. Leave **Windows** alone. Its path is element-directed and correct; revisit only if a
   virtualized WinUI list appears, at which point the answer is `ItemRealizer`.

## How to verify

`Tests/Scroll/ScrollTests.cs` is the harness — that is what it was built for. It should grow:

- a nested scrollable (a scrollable inside a scrollable), which is the hub's shape and the case
  that breaks `instance(0)`;
- a virtualized list, long enough that items are recycled;
- a horizontal scroller, since every current path assumes vertical.

Each addition should fail before the matching fix and pass after, on both platforms.

## Sources

- [appium-uiautomator2-driver](https://github.com/appium/appium-uiautomator2-driver) — `mobile: scroll` and `mobile: scrollGesture` arguments
- [appium#15392 — `mobile: scroll` always scrolls the first available scrollable](https://github.com/appium/appium/issues/15392)
- [Automating mobile gestures with the UiAutomator2 backend](https://appium.github.io/appium.io/docs/en/writing-running-appium/android/android-mobile-gestures/)
- [iOS XCUITest mobile gestures](https://appium.readthedocs.io/en/latest/en/writing-running-appium/ios/ios-xctest-mobile-gestures/)
- [Working with virtualized items (UIA)](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-workingwithvirtualizeditems)
- [FlaUI `ItemRealizer`](https://github.com/FlaUI/FlaUI/blob/main/src/FlaUI.Core/Tools/ItemRealizer.cs)
