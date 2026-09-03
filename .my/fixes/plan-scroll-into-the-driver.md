# Plan: points 2 and 3 — move scrolling into the driver, delete `FindByAndroidUIAutomator`

Companion to `cleanup-scroll-and-find-architecture.md`. Covers points **2** (scrolling mechanics
do not belong in `MauiTestContext`) and **3** (`FindByAndroidUIAutomator` is not generic). Point 3
falls out of point 2, so they are one change, done in one pass, in the order below.

Points 1 (comments) and 4 (popup scopes) are out of scope, except for two doc comments on members
this change touches, which are corrected in place (steps 3 and 6).

## Target shape

One neutral capability on the driver, implemented per platform:

```csharp
// IMauiDriver
/// <summary>
/// Finds an element by scrolling a container until it enters the accessibility tree.
/// </summary>
/// <param name="container">
/// The container to scroll, or null to let the platform pick the scrolling container on screen.
/// </param>
/// <returns>The element once it is on screen and still, or null when scrolling does not reach it.</returns>
IMauiElement? TryFindByScrollingWithin(IMauiElement? container, Locator locator);
```

- `FlaUIMauiDriver` returns `null` — UIA keeps scrolled-off-screen elements in the tree, so
  scrolling reveals nothing a plain lookup missed.
- `AppiumMauiDriver` owns the `UiScrollable` query, the container choice, the re-resolve, and the
  settle wait; it returns `null` on any platform but Android, which is exactly today's behaviour.
- `MauiTestContext` keeps find, find-all, try-find, and the scope-level decision *whether* to
  scroll. It stops knowing that Android exists.

**Why `IMauiDriver` and not `IDriver<TElement>` in Core.** The need this serves is not MAUI's, it
is the backend's — see "Cross-stack notes" at the end — so hoisting is a fair question, and the
answer is *not yet*. `Brinell.Html` has no driver interface at all: `IHtmlTestContext` implements
`IElementScope<IHtmlElement>` directly over the Playwright page. A Core member on
`IDriver<TElement>` would reach WPF, WinForms and NativeAndroid while missing the one other stack
that will genuinely need this, and would write `=> null` into five drivers to serve one real
implementation. Hoist when a **second stack has a real implementation**, not a stub; the signature
survives that move unchanged as `TElement? TryFindByScrollingWithin(TElement? container, Locator)`.

## Steps

### 0. Optional, decide before starting: rename `TryFindElementAfterScroll`

`AfterScroll` names a sequence the caller performed rather than a capability the scope offers, and
`IWpfElementScope` / `IHtmlElementScope` are today exact copies of `IMauiElementScope` minus this
one method — so the name will be inherited three times. `TryFindElementByScrolling` pairs it with
the driver's `TryFindByScrollingWithin`, and the two layers then read as one feature.

Eight call sites, all inside `Brinell.Maui`, all caught by the compiler. **If it is going to
happen, do it as its own commit before step 1** — folding it into the refactor would make a
mechanical rename and a code move indistinguishable in review. Skipping it is defensible; this is
noted only because it is the cheapest moment the rename will ever have. The rest of this plan uses
the current name.

### 1. Add the settle wait to `AppiumMauiElement`

`WaitUntilPositionSettles` in [MauiTestContext.cs:250-283](srcnew/Brinell.Maui/Context/MauiTestContext.cs#L250-L283)
is a property of an Android element, not of the root scope. Move it verbatim onto
[AppiumMauiElement.cs](srcnew/Brinell.Maui.Appium/AppiumMauiElement.cs) as an `internal void
WaitUntilPositionSettles()` next to `ScrollIntoView` (line 214) — `internal` because only
`AppiumMauiDriver` calls it and it is not a capability tests should reach for.

Keep the fling remark; it is the invisible constraint that justifies the loop. Add one line to it
recording that the *mechanism* is not Android's even though the *need* is:

```
/// The mechanism is general — two identical rectangles in a row — and depends on nothing but
/// Rect; it lives here because the need does not generalise. UIA scrolling is synchronous, and
/// Playwright already performs this check internally as its "stable" actionability requirement.
/// If a smooth-scrolling Windows surface ever needs it, the home is ElementGeometryExtensions
/// beside HasUsableBounds, and it is a move rather than a rewrite.
```

Nothing else about the method changes.

### 2. Add `TryFindByScrollingWithin` to `AppiumMauiDriver`

Move `TryFindWithUiScrollable` ([MauiTestContext.cs:175-213](srcnew/Brinell.Maui/Context/MauiTestContext.cs#L175-L213))
and `ReResolveAfterScrolling` ([MauiTestContext.cs:224-241](srcnew/Brinell.Maui/Context/MauiTestContext.cs#L224-L241))
into `AppiumMauiDriver`, replacing the `Platform-Specific` region at
[AppiumMauiDriver.cs:214-234](srcnew/Brinell.Maui.Appium/AppiumMauiDriver.cs#L214-L234):

```csharp
public IMauiElement? TryFindByScrollingWithin(IMauiElement? container, Locator locator)
{
    if (_platform != MauiPlatform.Android) return null;          // was MauiTestContext's job

    var scrollable = ScrollableSelector(container);              // new: closes the instance(0) gap
    foreach (var matcher in Matchers(locator.Value))
    {
        try
        {
            var elements = FindByUiAutomator($"new UiScrollable({scrollable}).scrollIntoView({matcher})");
            if (elements.Count > 0) return ReResolveAfterScrolling(locator) ?? elements[0];
        }
        catch { }
    }
    return null;
}
```

- `FindByUiAutomator` is the old `FindByAndroidUIAutomator` body, now **private** — the
  `MobileBy.AndroidUIAutomator` call plus its `AppiumMauiElement` wrapping. It loses the platform
  guard, which the caller now makes once.
- `ScrollableSelector(container)` is the one genuinely new line of behaviour: read the container's
  `resource-id` via `GetAttribute("resource-id")` and emit
  `new UiSelector().resourceIdMatches(".*<id>")`; fall back to
  `new UiSelector().scrollable(true).instance(0)` when the container is null or unnamed. That
  fallback is today's hard-coded string, so a null container reproduces today's behaviour exactly.
- `ReResolveAfterScrolling` ends with `(element as AppiumMauiElement)?.WaitUntilPositionSettles()`.
- Cut from the moved comments: the `instance(0)` limitation remark (the parameter now answers it)
  and `// Try the next matcher.` Keep the resource-id/content-desc remark and the reason for
  re-resolving — both are invisible in the code.

### 3. `FlaUIMauiDriver`

Replace `FindByAndroidUIAutomator` ([FlaUIMauiDriver.cs:898-905](srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L898-L905))
with:

```csharp
/// <inheritdoc />
/// <remarks>
/// UIA keeps scrolled-off-screen elements in the tree with <c>IsOffscreen=true</c>, so scrolling
/// reveals nothing a plain lookup missed. A virtualised list is the exception — there the answer
/// is <c>VirtualizedItemPattern.Realize()</c>, not scrolling — and no list under test virtualises.
/// </remarks>
public IMauiElement? TryFindByScrollingWithin(IMauiElement? container, Locator locator) => null;
```

Two things about that remark, both deliberate. It states the constraint rather than the
conclusion: "there is nothing scrolling can add" would be **false** for a virtualising WinUI list,
and `research-improving-scroll-to-find.md` already names the fix
(`ItemContainerPattern.FindItemByProperty` + `VirtualizedItemPattern.Realize()`, or FlaUI's
`ItemRealizer`). And this is the file `FlaUIWpfDriver` and `FlaUIWinFormsDriver` get copied from
when they need the member, so a wrong reason propagates three times.

Note the difference from the method it replaces: an empty list was a stub standing in for a
capability the platform lacks; `null` is a real answer to a question the platform can be asked.

### 4. `IMauiDriver`

In [IMauiDriver.cs:118-127](srcnew/Brinell.Maui/Interfaces/IMauiDriver.cs#L118-L127), delete the
`Platform-Specific` region and `FindByAndroidUIAutomator` outright — **point 3 done** — and add
`TryFindByScrollingWithin` under a `Scrolling` region with the doc comment from "Target shape".

No escape hatch is added: nothing outside the scroll path ever called it. If a raw UIAutomator
query is wanted later, it is one `private` → `public` on `AppiumMauiDriver`, reachable only by a
caller that has already chosen to be Android-specific.

(`NativeAndroidDriver.FindByAndroidUIAutomator` is a different class that does not implement
`IMauiDriver`. Leave it alone — being Android-specific is its whole job.)

### 5. `MauiTestContext`

- Delete `TryFindWithUiScrollable`, `ReResolveAfterScrolling`, `WaitUntilPositionSettles` — all
  three now live where they belong.
- [TryFindElementAfterScroll:112-130](srcnew/Brinell.Maui/Context/MauiTestContext.cs#L112-L130):
  the tail becomes `return _driver.TryFindByScrollingWithin(null, locator);`, with the
  `_platform == MauiPlatform.Android` test gone. Trim the remark to the constraint that survives:
  the root scope has no container to name, and does not poll because the caller has already
  established a plain lookup finds nothing.
- [FindElement:154-159](srcnew/Brinell.Maui/Context/MauiTestContext.cs#L154-L159): the
  post-timeout Android branch becomes the same one-line call.

Expected: `MauiTestContext.cs` drops from 402 lines to roughly 290 — back to what it was before
the scroll work, and back to being a root scope.

### 6. Scope side — correct one remark, leave the container null

`PageObjectBase` ([line 271](srcnew/Brinell.Maui/Pages/PageObjectBase.cs#L271)) and
`ContainerObjectBase` ([line 163](srcnew/Brinell.Maui/Containers/ContainerObjectBase.cs#L163))
keep the bodies they have. Deliberately:

- A page's root is a `ContentPage`, which is **not** a scrollable node on Android — passing it as
  the container would build a `UiScrollable` over a non-scrollable selector and regress every page
  that scrolls today.
- `ContainerObjectBase.TryFindElementAfterScroll` currently does not scroll at all. Making it
  scroll is a behaviour change, and this change set is verified by "nothing changed".

The parameter is still worth adding now: it is what lets step 7 be a two-line override rather than
another driver method, and it is the shape point 2 prescribes.

One comment does change. [IMauiElementScope.cs:16-22](srcnew/Brinell.Maui/Interfaces/IMauiElementScope.cs#L16-L22)
justifies the method with *"the need is MAUI-specific"*. That is a statement about Android, not
about MAUI, and this is the interface WPF and Html will copy — as written it reads as a reason
**not** to copy the method when Html meets its first virtualised list. Correct it to name the
backend rather than the stack, keeping the pointer to
`.my/scroll/finding-why-android-hides-offscreen-controls.md`.

### 7. Optional, separable: let `ScrollView` name itself

Once 1–6 are green, `ScrollView<TParent, TSelf>`
([ScrollView.tpl.cs](srcnew/Brinell.Maui/Controls/Container/ScrollView.tpl.cs)) can override:

```csharp
public override IMauiElement? TryFindElementAfterScroll(Locator locator)
    => TryFindElement(locator)
       ?? Context.Driver.TryFindByScrollingWithin(TryGetContainerRoot(), locator);
```

This is option A from `research-improving-scroll-to-find.md`, and the first caller that passes a
real container. It **adds** behaviour — a control declared inside a `ScrollView` container object
becomes reachable when it is below the fold and the page's first scrollable is not the right one —
so it needs its own test (a nested scrollable on `ScrollPage`, per that document's "How to verify")
and should be a separate commit. Do it only after 1–6 have been measured clean; a behaviour change
landed alongside a refactor makes both unfalsifiable.

Optional, but it is the piece most worth getting right: **this override is the part other stacks
will copy**, and each already has the object to hang it on — `Wpf.Controls.ScrollView`,
`Html.Controls.Container.ScrollContainerControl`. It also shows the pattern needs no new driver
member on the copying stack: a scope that knows its own root, plus the one neutral capability, is
the whole of it.

## Call sites that do *not* change

`IMauiElementScope.TryFindElementAfterScroll`, `ViewBase.TryFindElementAfterScroll()`,
`ScrollingOnceResolver()`, `IsExists()`, and `ReadinessTests`' mock of
`IMauiTestContext.TryFindElementAfterScroll` all sit above the seam being moved and are untouched
(step 6 edits one of their doc comments, not their behaviour).

No test mocks `FindByAndroidUIAutomator`; the four `Mock<IMauiDriver>` sites
(`FluentChainingTests`, `ContentDialogControlTests`) are loose mocks that set up other members, so
removing it from the interface does not break them.

## Cross-stack notes

MAUI is the frontrunner: WPF, WinForms and Html follow whatever shape it settles on. So the test
for each member is not "does MAUI need it" but "when `IWpfDriver` is copied from `IMauiDriver`
again, should the copier keep this line, drop it, or reinterpret it?"

The problem being solved is *the tree omits what is not rendered*, which is a property of the
automation backend, not of MAUI:

| Backend | Scrolled off-screen | Virtualised away | Needs a real implementation |
|---|---|---|---|
| UiAutomator2 — `Maui.Appium` | **absent from tree** | absent | **today** |
| Windows UIA — `Maui.FlaUI`, `Wpf`, `WinForms` | present, `IsOffscreen=true` | **absent** — needs `VirtualizedItemPattern.Realize()` | when a virtualising list appears |
| DOM via Playwright — `Html` | present in DOM | **absent** — `react-window` and friends | when a virtualised list appears |

So `TryFindByScrollingWithin` is not an Android idea wearing a neutral name: every backend can be
asked it truthfully, and two of the three will eventually answer with real work. That is what
separates it from `FindByAndroidUIAutomator`, whose *signature* named one platform's query
language and which FlaUI could only answer with a lie.

Where each piece lands, and what would move it:

| Piece | Home | Moves to Core when |
|---|---|---|
| `TryFindByScrollingWithin` | `IMauiDriver` | a second stack has a real implementation — but see the caveat above: Html has no driver, so the eventual Core home may be the scope, not `IDriver<TElement>` |
| `WaitUntilPositionSettles` | `AppiumMauiElement` | a smooth-scrolling Windows surface needs it → `ElementGeometryExtensions` |
| `TryFindElementAfterScroll` | `IMauiElementScope` | copied per stack, following `IWpfElementScope` convention |
| Container choice (step 7) | the scope | copied per stack; needs no driver member of its own |

The precedent for all of this is `ElementGeometryExtensions`, hoisted to Core with its reason
written down: *it depends on nothing but `IElement<TSelf>`, and every platform needs it.* Only the
first half of that test passes here today.

## Verification

Unit tests first — `dotnet test testsnew/Brinell.Maui.Tests` and `Brinell.Generator.Tests` — since
they cost seconds and catch the interface change.

Then, per `cleanup-scroll-and-find-architecture.md`, hold the current state exactly:

- Windows, Buttons + Text + Display + Toggle + Scroll: **76 / 77**
  (`ProgressBar_Reset_ReturnsToInitialState` fails before this change too).
- Android, Toggle: **15 / 16** — the rotating `Reset` failure in
  `.my/scroll/finding-toggle-reset-flakiness.md` is still open; do not chase it during this change.
- Android, Scroll: **7 / 7**. This is the tier that actually exercises the moved code — run it
  after step 5 and before step 7, and confirm the APK is freshly installed (`adb install -r`),
  or the run tests yesterday's build and proves nothing.

Steps 1–6 are one commit. Step 0, if taken, is a commit before it; step 7, if taken, is a commit
after it with its own test.
