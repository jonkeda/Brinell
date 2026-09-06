# Redesign: navigation controls as collections of items

## The problem

The navigation controls model a *container* but expose no *items*. Everything an item can do is
reached by passing a locator back into the container:

```csharp
Page.PrimaryToolbar.ClickToolbarItem(Locator.ByAutomationId("ToolbarSaveButton"));
Page.PrimaryToolbar.GoBack(Locator.ByAutomationId("ToolbarBackButton"));
Page.ActionsMenu.Open();
Page.ActionsMenu.ClickMenuItem(Locator.ByAutomationId("PageMenuFileNew"));
Page.TabMenu.Select("Second");
```

Three things follow from that shape, and all three are visible in the current code:

**A locator is not an item.** `ClickToolbarItem(locator)` can only ever click. There is nowhere to
ask whether the item is enabled, what its text is, or whether it exists — because the item is
never an object. `Toolbar` has `GetTitle` for the *toolbar*, and nothing at all for an item.

**Every new question needs a new method on the container.** `ClickToolbarItem`, then `GoBack`
(which is `ClickToolbarItem` with a different name), then `ClickMenuItem`, then `Select`. Asking
"is the Save item enabled?" would mean `IsToolbarItemEnabled(locator)`, and so on for every
property × every container.

**Each container reinvents item lookup.** `Toolbar` and `Menu` both do
`element.FindElement(itemLocator)`. `TabMenu` does something else entirely — it fetches three
parallel lists (`TabMenuView_Caption`, `TabMenuView_Button`, `TabMenuView_Grid`) and matches them
by index, in hand-written code that cannot be reused by anything.

Meanwhile the same problem was already solved elsewhere in the framework.

## The model to copy

`CollectionObjectBase<TParent, TSelf, TItem>` already gives a container a typed, indexable set of
item objects:

```csharp
public TItem this[int index]
public TItem Item(int index)
public IEnumerable<TItem> Items
public int GetItemCount(int? timeoutMs = null)
public TItem? FindItem(Func<TItem, bool> predicate)
public TItem ItemWhere(Func<TItem, bool> predicate)
public TSelf AssertItemCount(int expected, ...)
public TSelf AssertEmpty(bool? expected = true, ...)
```

and `ItemContainerBase<TCollection, TSelf>` gives each item its own scope, so an item exposes
controls the same way a page does:

```csharp
public class ProductRow : ItemContainerBase<ProductCollection, ProductRow>
{
    public CheckBox<ProductRow> Selected => new(this, "ProductSelectedCheckBox");
    public Label<ProductRow> Name => new(this, "ProductNameLabel");
}
```

Navigation containers are collections of items. They should say so.

## The redesign

### Item controls

Introduce a small control per item kind, each an `ItemContainerBase` so it carries its own scope:

| Control | Item of | Members |
|---|---|---|
| `MenuItem` | `Menu` | `Click()`, `GetText()`, `IsEnabled()`, `IsVisible()`, plus `Wait*`/`Assert*` |
| `ToolbarItem` | `Toolbar` | same |
| `TabItem` | `TabMenu` | `Click()`, `GetText()`, `IsSelected()`, plus `Wait*`/`Assert*` |

Every member is a `*Core` method, so the generator emits the `Get`/`Wait`/`Assert` trios rather
than each control hand-writing them. `IsSelectedCore` on `TabItem` is the one that pays for
itself immediately — today nothing can ask which tab is current.

### Containers become collections

```csharp
public partial class Toolbar<TParent> : CollectionObjectBase<TParent, Toolbar<TParent>, ToolbarItem>
public partial class Menu<TParent>    : CollectionObjectBase<TParent, Menu<TParent>, MenuItem>
public partial class TabMenu<TParent> : CollectionObjectBase<TParent, TabMenu<TParent>, TabItem>
```

The item strategy is what each container supplies — `ItemStrategy.ByAutomationId("ToolbarItem")`
for a toolbar, and for `TabMenu` a strategy over `TabMenuView_Button`, which replaces the
three-parallel-list matching with the same mechanism every other collection uses.

### What tests then look like

```csharp
// today                                          // redesigned
Toolbar.ClickToolbarItem(Locator.ById("Save"));   Toolbar["Save"].Click();
Toolbar.GoBack(Locator.ById("Back"));             Toolbar["Back"].Click();
Menu.Open();                                       Menu.Open();
Menu.ClickMenuItem(Locator.ById("New"));           Menu["New"].Click();
TabMenu.Select("Second");                          TabMenu["Second"].Click();
// not expressible today                           Toolbar["Save"].AssertEnabled();
// not expressible today                           TabMenu["Second"].AssertSelected();
// not expressible today                           Toolbar.AssertItemCount(4);
// not expressible today                           foreach (var item in Menu.Items) ...
```

A string indexer (`this[string automationId]`) is worth adding alongside the existing integer one:
navigation items are identified by name far more often than by position, and `Toolbar["Save"]`
reads better than `Toolbar.ItemWhere(i => i.AutomationId == "Save")`.

### What this removes

- `ClickToolbarItem`, `ClickMenuItem`, `GoBack`, `Select`, `TrySelect` — all become
  `item.Click()`.
- `TabMenu`'s parallel-list matching (`TrySelectNow`, the three `FindElements` calls, the
  index-alignment loop) — replaced by the shared item strategy.
- The `X`/`TryX` pair on `TabMenu` — `TryItem(index)` and `FindItem(predicate)` already express
  "may not be there" without a second method per operation.

### What stays on the container

`Menu.Open()` and `Menu.IsOpen()` are properties of the menu, not of an item, and stay as they
are. `Toolbar.GetTitle()` likewise.

## Scope: Shell is covered by its own design

`Shell`, `ShellContent`, `Tab` and `FlyoutItem` have no references outside `Controls/Navigation`,
because the app that exercised them was replaced by the page hub. An earlier draft of this
document read that as grounds for deleting them. It is not: they are unreferenced because their
sample went away, not because Shell stopped mattering.

They are redesigned and covered by
**[design-shell-sample-app.md](design-shell-sample-app.md)**, which adds a dedicated Shell sample
app and a test suite on both platforms. That design adopts the collection model described here —
`Shell.Tabs` is a collection of tab items, a flyout is a collection of flyout items — so the two
documents describe one model applied to two families of control.

The ordering between them matters: the toolbar step below settles the open question about item
scope, and the Shell work inherits that answer rather than deciding it again.

## What step 2 settled

**The menu was modelled inside out, and the collection shape exposed it.** `Menu` was bound to
its *trigger*, so `IsOpen()` returned the trigger's own visibility — which is true whether the
menu is open or shut. Nothing caught it because the test asserting `IsOpen()` after `Open()`
passed for the wrong reason. Worse, a test that wanted to click an item had to build a *second*
`Menu` bound to the item host, which is the clearest possible sign the object was the wrong
shape.

A menu is now rooted at the menu as a whole, with its trigger and its item host inside it:

```csharp
new Menu<MyPage>(this, "ActionsMenu",
    triggerLocator: Locator.ByAutomationId("ActionsMenuTrigger"),
    itemsHostLocator: Locator.ByAutomationId("ActionsMenuItems"));

Menu.Open().AssertOpen().AssertItemCount(3);
Menu["New"].Click();
```

`Open` clicks the trigger and `IsOpen` reads the item host, so both are answered by the part that
knows. Both locators are optional and default to the menu's own root, which is the old behaviour
for a menu that is always showing its items. Nothing guesses a naming convention.

**Items sometimes live in a host inside the collection root**, and the menu's trigger is the
proof: it is a button inside the menu, so "the buttons in this menu" would have counted four
items and offered the trigger as one of them. `ItemStrategy.Within(hostLocator, inner)` composes
with any other strategy instead of teaching each one about hosts, and a host that is not there
yields no items — which is the right answer for a closed menu, on both platforms and for whatever
reason it is missing.

**The generator's collision check was a false positive.** It refused `OpenCore` alongside
`IsOpenCore` because both derive the stem "Open" — but the members emitted are `Open()` against
`IsOpen()`/`WaitOpen()`/`AssertOpen()`, which do not collide. (The old `Menu` carried a comment
asserting they would; it was wrong, and cost that control its `WaitOpen`/`AssertOpen`.) Each
generator now declares the names it will actually emit and the check compares those, so a real
clash — `GetOpenCore` with `IsOpenCore`, which both emit `WaitOpen` — still fails, with the
colliding member named. Two tests cover the pair.

**A keyed lookup has to wait; an index does not.** `Menu.Open()["New"]` failed against the real
app: `Open` returns when the click lands, and the items render a frame later, so the lookup read
an empty host. `Item(...)` now polls and `TryItem(...)` still answers about right now — the same
split as `FindElement` against `TryFindElement`, and for the same reason. An index stays immediate
because it names a position in what is materialized now, which is a different question.

Worth recording how that was found: the failure looked like broken matching, and the elements
turned out to be exactly right (`id='MenuItemOpen' text='Open'`). One throwaway probe printing
what the tree actually held settled it in a minute, where reasoning about the matcher would have
gone on for some time. Same lesson as the fling hypothesis.

## What step 3 settled

**A tab is a surface, not an element**, and that is what the item model is for. Each
`TabMenuView_Grid` is one tab, holding the button that carries the command and the label that
carries the caption; the tab is rooted there and each member reaches for the part that knows.
The parallel-list matching is gone: three `FindElements` calls paired by position, a fallback
chain of four more lookups, and `TryActivateTabSurface` with its own activation ladder — all of
it replaced by an item strategy and `ClickableItemBase`. The navigation suite got **faster**
(48 s to 35 s), because the old `Select` re-fetched three lists on every poll tick.

**Probing first paid again.** One throwaway test dumped the tab tree before any code was
written, and it changed two decisions: the tab's own element reports an **empty string** on
Windows (so `Tabs["Search"]` needs `MatchesKey` to reach the caption and the button — it would
otherwise have matched nothing), and every tab reports `Selected=False` with no selection
pattern (so the sample cannot cover `IsSelected` at all). Both would have been long debugging
sessions and were instead ten seconds of output.

**`GetCaption` became `GetText`.** The members table above says `TabItem.GetCaption()`. It is
`GetText()`, overridden to read the caption label, then the button, then the tab itself — the
same argument as clicking rather than selecting: one word for one idea across item types beats a
word that reads better on one of them. Where the text lives is the tab's business.

**`IsSelected` is implemented but the sample cannot prove it.** It reads what the platform
exposes — `Selected` (which on Windows already covers the selection pattern and falls back to
the toggle pattern), then the checked state for Android's radio-style bars — on the tab and then
on its button. The demo's bar is plain buttons and exposes neither, so it answers false for the
tab just clicked. Rather than pretend, three things: unit tests over mocked elements that *do*
expose selection cover the ladder, a UI test pins "a plain-button bar reports no selection" so
nobody later "fixes" it by guessing from app state or styling, and real coverage waits for
[design-shell-sample-app.md](design-shell-sample-app.md), where tabs are genuinely selectable.

**A trap worth knowing: the generator ignores `override` Core methods.** `TabItem` overrides
`ClickCore` and `GetTextCore`, which is right — the public members come from
`ClickableItemBase` and dispatch virtually — but it means an attribute like
`[GenerateComparisons]` on an override is silently dead. One was written and removed. The rule
is the existing one (a Core method generates only when `protected virtual`), seen from a new
angle.

## Order of work

1. ~~**`ToolbarItem` + `Toolbar` as a collection.**~~ **Done** — see "What step 1 settled" below.
   8 toolbar tests pass on Windows in 4 s.
2. ~~**`MenuItem` + `Menu`.**~~ **Done** — see "What step 2 settled" below.
3. ~~**`TabItem` + `TabMenu`.**~~ **Done** — see "What step 3 settled" below.
4. **The Shell family** follows in [design-shell-sample-app.md](design-shell-sample-app.md),
   once 1–3 show the model holds.

Each step is independently shippable and independently verifiable.

## How to verify

`Tests/Navigation/NavigationControlTests.cs` is the contract, rewritten step by step to the new
API. The warning that once stood here — three toolbar tests failing intermittently, the failure
rotating between runs — no longer applies: those tests were rewritten in step 1, and the whole
navigation suite has since run **23/23 twice in a row**. Whether the rotation was in the old
locator-passing API or in the app has not been established, so if it reappears in step 3, treat
that as new information rather than as the redesign breaking something.

Both platforms matter: `TabMenu` is the control whose current implementation is most tied to how
one platform lays out its tree, so it is the one most likely to behave differently on Android.

## Decided, and settled

**Decided: an item is clicked, not selected.** `TabItem` exposes `Click()`, not `Select()`.
Selecting a tab *is* clicking it, and one verb across `ToolbarItem`, `MenuItem` and `TabItem` is
worth more than a word that reads better in one place. Selection remains observable —
`IsSelected()` / `AssertSelected()` — so nothing is lost except a second name for one action.

**Answered: an item needs its own scope, because the collection base only hands out containers.**
`CollectionObjectBase<TParent, TSelf, TItem>` constrains `TItem : ItemContainerBase<TSelf, TItem>`,
so "collection API" and "item is a container" are the same decision — deriving a leaf item from
`ClickableControlBase` was never open, it was excluded by the type system. What that costs is one
class: a leaf item cannot inherit a control base, so `ClickableItemBase<TCollection, TSelf>` holds
`ClickCore`, `GetTextCore` and `IsEnabledCore` for every item type. It cost less than expected —
`ContainerObjectBase` already answers existence and visibility against the item's root, so those
three members are the whole of it, and `MenuItem` and `TabItem` now inherit them for free.

## What step 1 settled

Several things came out different from the design above, each because building it said so.

**An item is addressed by a selector, not by one fixed property.** The design said
`this[string automationId]`, and a first cut keyed on the caption instead, because an element
carries one automation id and keying items on a repeating id would leave `Toolbar["Save"]` unable
to name one. Both were too narrow. The key is now a `Locator` - the vocabulary the framework
already uses for "how to identify a thing":

```csharp
Toolbar["Save"]                                   // automation id, else caption
Toolbar[Locator.ByAutomationId("ToolbarSave")]    // say which
Toolbar[Locator.ByText("Save")]
Toolbar[Locator.ByControlType("Button")]

Toolbar.ItemByAutomationId("ToolbarSave")         // the same, named, for a fixed selector
Toolbar.ItemByText("Save")                        // (plus ItemByName, ItemByControlType
Toolbar.TryItemByText("Save")                     //  and a Try* form of each)
```

The plain string tries **the id across every item first**, and only then captions - an id is what
the app author chose, a caption is what the platform rendered. Ids are compared exactly and
captions leniently (trimmed, case-insensitive), because Android cases button text to suit its
theme and an exact caption match would pass on one platform and fail on the other. `ElementMatch`
holds that comparison; `MatchesKey` stays virtual for a collection whose items are identified by
something the root does not carry.

Comparing ids means reading them back, and only the adapter knows that Windows says
`AutomationId`, Android says `resource-id` (fully qualified, so the package prefix is stripped)
and iOS says `name`. `IMauiElement.AutomationId` is where that is answered now, so generic code
never guesses an attribute name. A control-type key matches the last segment of the platform's
type name, so `ByControlType("Button")` works on both - but type *names* differ (a MAUI Entry is
`Edit` on Windows and `EditText` on Android), and the doc says so rather than pretending
otherwise.

**Items are found by control type, not by a repeating automation id.** A toolbar holds commands
and a command is a button on every platform Brinell drives, so `Toolbar.DefaultItemStrategy` is
`ByLocator(Locator.ByControlType("Button"))`. A bar holding something else passes its own strategy
to the constructor. This needed one adapter line: the Appium locator translation knew only
`entry`, and now maps `button` to `android.widget.Button` / `XCUIElementTypeButton`.

**The generator learned to see a closed self-reference.** `Toolbar<TParent>` derives from
`CollectionObjectBase<TParent, Toolbar<TParent>, ToolbarItem<TParent>>`: it declares one type
parameter and no `TSelf`, so the generator's rule 3 gave `AssertTitle` a `TParent` return where
the base returns `Toolbar<TParent>`, and the generated file did not compile. A concrete class that
passes *itself* to its base is making the same statement `TSelf` makes, so the analyzer now reads
it that way. Three tests cover the rule, including the case it must not fire on — a control
passing its scope, not itself.

**The item constraint is now the interface.** `CollectionObjectBase` constrained `TItem` to
`ItemContainerBase<TSelf, TItem>` - the class - while the interface it implements already
constrained the same parameter to `IMauiItemContainer<TSelf, TItem>`. The base calls nothing on an
item: items are built by the factory and handed straight back, so the class constraint bought
nothing the interface does not. It is now `class, IMauiItemContainer<TSelf, TItem>` on
`CollectionObjectBase`, `CollectionView`, `ListView` and `CarouselView` - `class` because `TItem?`
has to mean null. No call site changed, the guarantee that an item is scoped to its collection is
unchanged, and an item type is now free to reach that any way it likes.

**Shared, not copied.** The activation ladder (SelectionItem, then Invoke, then a pointer click)
existed twice before this step and would have become three times. It now lives once, in
`ActivationHelper` beside `ScrollHelper`, with `ClickableControlBase` and `CollectionObjectBase`
delegating to it. Four other copies remain in `TabMenu` and `Brinell.Maui.Extensions`; steps 2-3
are the moment to fold those in.
