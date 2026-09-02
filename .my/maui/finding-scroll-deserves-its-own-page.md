# Finding: scrolling needed its own test page

## Why

Scrolling was only ever exercised incidentally — by tests whose Reset button happened to sit
below the fold. A scroll defect therefore surfaced as an unrelated assertion about a status
label, three or four layers from the cause. Every Android diagnosis in
[finding-android-offscreen-elements-leave-the-tree.md](finding-android-offscreen-elements-leave-the-tree.md)
had to work backwards through that.

## What was added

- **`ScrollTestView`** — a page deliberately taller than any screen. The status label is the
  *first* element and the buttons are spread down it, so pressing one and reading the result
  crosses the full height in both directions. Reachable from the hub as `SamplePage.Scroll`.
- **`ScrollTestPage`** page object, with no `IsLoaded` override: the root marker sits on the
  `ScrollView`, a real rendered view on every platform that cannot scroll itself out of view.
- **`Tests/Scroll/ScrollTests.cs`** — seven tests that make the scroll the subject: a control
  above the fold as the control case, clicking below-fold controls at two depths, scrolling down
  and back up, reading text from a below-fold label, both visibility questions on one control,
  and existence without scrolling.

## It earned its place immediately

**Windows: 7 / 7. Android: 0 / 7** — all seven failing before reaching the page at all:

```
ElementNotFoundException : Element not found with locator: AutomationId:Open_Scroll
```

The *hub itself* scrolls on a phone, and the new entry sits below the fold. So the first thing
the page found was not a defect in the page under test but in navigating to it — a gap that no
existing test could have shown, because every other hub entry happens to be above the fold on
this emulator.

## The scroll-to-find has a known limit

`TryFindWithUiScrollable` scrolls `scrollable(true).instance(0)` — the first scrollable container
on the page. On the hub that is the outer `PageHubScroll`, while the `CollectionView` inside it
is what actually scrolls, so the search scrolls a container that cannot move.

Trying each container in turn was implemented and measured: it did **not** fix the hub lookup,
and made every failed lookup roughly ten times slower (5 s → 52 s per test). Reverted. The
matcher does now try content-desc as well as resource-id, and re-resolves by the caller's own
locator after scrolling — that part was measured to help (47 → 50 of 55).

So the limit stands, documented, with a failing test that demonstrates it rather than a comment
claiming it.

## Android status

| Run | Passed |
|---|---|
| Before any of this | 47 / 55 |
| After page-root markers | 46 / 55 |
| After re-resolve-after-scrolling | 50 / 55 |
| Repeat of the same build | 48 / 55 |

The last two rows are the same code. The Text `Clear`/`ResetAll` tests move between passing and
failing run to run, so that family is flaky and not fixed. Failing in **every** run:
`Editor_LineBreaks_ArePreserved`, `Switch_Reset_ClearsState`,
`RadioButton_Reset_ClearsSelection`, `CheckBox_Reset_ClearsState`.

`Editor_LineBreaks` is probably not a scroll problem at all — it asserts `'2 lines'` on a
mid-page control, which points at newline handling through `replaceElementValue`.

Windows is unchanged throughout: **69 / 70**, plus the seven new scroll tests.
