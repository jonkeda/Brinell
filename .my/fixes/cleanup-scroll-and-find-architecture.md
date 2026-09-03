# Cleanup: scrolling, finding, and the comments around them

## What happened

The scroll work was done under measurement pressure — find a cause, change one thing, run, keep
or revert — and each fix was put wherever it was quickest to reach. Nothing stepped back to ask
where it belonged. The result is measurable:

| | before | after |
|---|---|---|
| `MauiTestContext.cs` | 291 lines | **402 lines** |
| comment density, `MauiTestContext.cs` | — | **33%** |
| comment density, `ViewBase.tpl.cs` | — | **36%** |
| comment density, `ToggleControlBase.tpl.cs` | — | **36%** |

## The single mistake underneath all four points

Every item below is the same error: **expressing *where* or *how* to search as a new method on
the driver or the context, instead of as a scope.**

| Symptom | What it really is |
|---|---|
| `MauiTestContext.TryFindElementAfterScroll` | "search this scope, scrolling it" |
| `MauiTestContext.TryFindWithUiScrollable` | Android's way of scrolling a container |
| `IMauiDriver.FindByAndroidUIAutomator` | one platform's query language on a neutral interface |
| `IMauiDriver.FindPopupElement` | "search the popup scope instead of the page scope" |

Brinell already has the concept that answers all of them — the scope: a page, a container, an
item. A scope knows its root; a driver knows how to drive a platform. Neither of those was used,
so the driver and the context grew ad-hoc find variants instead. `research-improving-scroll-to-find.md`
called this out as option A (scope-directed scrolling) before any of it was written; it was not
followed.

---

## 1. Comments

Most of the comments added recently narrate an incident: what broke, what was measured, what was
reverted. That belongs in `.my/`, not in the source. A comment in the code should explain what a
reader cannot deduce from the code itself, and should still be true in a year.

**Cut:**

- Measurements and timings. `"Measured at ~2.7 s a call"`, `"took the Windows suite from 42 s to
  4 m 27 s"`, `"811 lookups for 34 tests"`. These date instantly and are already recorded in
  `.my/scroll/`.
- Post-mortems of fixes. `"This used to be Title.IsExists(), and once IsExists learned to
  scroll..."`, `"the old nested-text branch returned before reaching it"`. The bug is gone; the
  story is not the reader's problem.
- Rejected alternatives, unless a reader would otherwise reintroduce them. `"Trying each
  container in turn was measured and did not help"` is worth one line at most, or a pointer.
- Repetition of the code. `"// Try the next matcher."` above `continue`.

**Keep:**

- Constraints that are invisible in the code: Android drops off-screen elements from the tree;
  WinUI toggles advertise LegacyIAccessible but do not honour it; a `ContentPage` is not a
  rendered view on Android.
- Ordering that looks arbitrary but is not: why the toggle rung comes last in the activation
  ladder.
- A single pointer to the `.my/` document where the reasoning lives.

**Rule of thumb:** if the comment would have to change when the bug is fixed differently, it is a
changelog entry, not a comment.

Target: bring the three files above to something like 15% and lean on `.my/` for the rest.

---

## 2. Scrolling does not belong in `MauiTestContext`

`MauiTestContext` is the platform-neutral root scope. It now contains `TryFindWithUiScrollable`
(a `UiScrollable` query string), `ReResolveAfterScrolling`, and `WaitUntilPositionSettles` — one
platform's scrolling mechanics, its quirks, and a fling-settling workaround, in a class that
should not know Android exists.

**Where each piece belongs:**

| Piece | Home | Why |
|---|---|---|
| Building and running the scroll query | `AppiumMauiDriver` | It is Android's mechanism |
| Waiting for a fling to settle | `AppiumMauiElement` | It is a property of an Android element |
| Deciding *whether* to scroll to find | the scope | The scope owns "where does this element live" |
| Choosing *which* container to scroll | the scope | It is the scope's root — option A |

**Shape to aim for.** The scope asks; the driver performs:

```csharp
// IMauiElementScope — the scope knows its own root and whether it scrolls
IMauiElement? TryFindElementAfterScroll(Locator locator);

// IMauiDriver — one neutral capability, implemented per platform
IMauiElement? TryFindByScrollingWithin(IMauiElement? container, Locator locator);
```

`FlaUIMauiDriver` returns null (UIA keeps off-screen elements, so there is nothing to do).
`AppiumMauiDriver` owns the `UiScrollable` query, the container choice, and the settle wait.
`MauiTestContext` keeps only what a root scope needs: find, find-all, try-find.

Passing the container also closes the known gap in `research-improving-scroll-to-find.md`: today
the query hard-codes `scrollable(true).instance(0)`, so a page whose scrolling container is not
the first one on screen cannot be scrolled at all.

---

## 3. `FindByAndroidUIAutomator` is not generic

```csharp
// IMauiDriver
IReadOnlyList<IMauiElement> FindByAndroidUIAutomator(string uiAutomatorQuery);
```

A neutral driver interface with one platform's query language in the signature. `FlaUIMauiDriver`
implements it by returning an empty list — the tell that it does not belong. It exists only so
`MauiTestContext` could build a `UiScrollable` string, which point 2 removes.

**Fix:** delete it from `IMauiDriver` once scrolling moves into the driver. Nothing else uses it.
If a raw escape hatch is ever wanted, it belongs on `AppiumMauiDriver` as its own public API,
reachable by a caller that has already chosen to be Android-specific — not on the interface every
platform must implement.

---

## 4. The popup finders should be scopes

```csharp
IMauiElement FindPopupElement(Locator locator, int timeoutMs = 5000);
bool TryFindPopupElement(Locator locator, out IMauiElement? element, int timeoutMs = 0);
```

These exist because a WinUI `ContentDialog` renders in a separate top-level window, so the normal
search misses it. But "search somewhere other than the page" is exactly what a scope is for, and
`ContentDialog` is already a control object that could *be* that scope — `ContainerObjectBase`
already resolves and caches a root and scopes lookups to it (`TryGetContainerRoot`).

**Fix:** give `ContentDialog` a scope whose root is the dialog, and let ordinary
`TryFindElement`/`FindElement` run inside it. The driver then needs at most a neutral way to
resolve that root — "the active dialog root" — with FlaUI walking the other top-level windows and
Appium returning the in-tree dialog. The two popup methods disappear from `IMauiDriver`, and
`ContentDialog` stops reaching through `Context.Driver` for every button.

This also removes a duplicated waiting loop: `FindPopupElement` has its own timeout poll, separate
from `RunPoll`.

---

## Order of work

1. **Comments** — independent of the rest, immediate, no behaviour change.
2. **Point 2**, then **point 3** — 3 falls out of 2, and together they empty
   `MauiTestContext` back to a root scope.
3. **Point 4** — the largest, and the only one that changes a public control's structure.

## How to verify

No behaviour should change. Current state to hold:

- Windows, Buttons + Text + Display + Toggle + Scroll: **76 / 77** (the one failure is
  `ProgressBar_Reset_ReturnsToInitialState`, pre-existing).
- Android, Toggle: **15 / 16**, with one rotating `Reset` failure still open — see
  `.my/scroll/finding-toggle-reset-flakiness.md`.
- Android, Scroll: **7 / 7**.

Move that Android `Reset` flakiness before or after, but not during: it rotates between tests
run to run, so it will otherwise look like the refactor caused it.
