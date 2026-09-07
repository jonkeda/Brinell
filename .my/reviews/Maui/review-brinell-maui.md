# Review: Brinell.Maui

## What was asked

1. Can tests now be written by chaining page objects, control objects and methods?
2. Do they ever still need to find elements themselves?
3. Is it usable?

## How this was measured

The only honest evidence for an API's usability is the code written against it, so this counts
what `Brinell.Maui.UITests` actually does — **211 test methods across 29 files** — rather than what
the API makes possible. Every test method was classified by whether its body reaches past the
fluent surface: a raw xUnit `Assert.*`, a `FindElement`/`TryFindElement` call, or a control object
constructed inline.

Windows only. Android and iOS are not measured here, and question 3 in particular would need
re-asking against Appium.

---

## 1. Yes — and three quarters of the suite already does

**158 of 211 test methods (74%) are pure fluent**: no raw assert, no element lookup, no control
constructed by hand. The whole test is page object → control object → method, chained.

The generated API is built for it. Across the control library:

| Returns | Count | Chains? |
|---|---|---|
| `Assert*` → `TScope` / `TSelf` / parent | 59 | yes |
| Actions (`Click`, `Focus`, `Clear`, …) → `TScope` | 11 | yes |
| `Set*` → `TScope` | 9 | yes |
| `Wait*` → `bool` / `bool?` | 59 | **no** |
| `Is*` → `bool?` | 16 | **no** |
| `Get*` → value | 24 | **no** |

That split is right, not a flaw: a getter has to return the value. What matters is that **every
readable state also has a chainable `Assert*`**, because the generator emits the Get/Wait/Assert
trio together. So a test never has to leave the chain to check something — 274 fluent `.Assert*`
calls against 88 raw `Assert.*` in the suite bears that out.

Where tests do fall back to raw asserts, it splits three ways:

- **Structural checks with no fluent equivalent, and there shouldn't be one.**
  `Assert.Null(Page.Products.TryItem(9999))`, `Assert.Equal(0, first.Index)`. Asking "does row
  9999 exist" is a question about the collection, not an assertion on a control.
- **Deliberate meta-tests of chaining itself.** `Assert.Same(page, afterCount.Parent.Parent)` in
  `ProductCollectionTests` and `NavigationControlTests` exists to prove the fluent return values
  are the objects they claim. Those cannot be written fluently by definition.
- **Avoidable style.** `Assert.Equal("Keyboard", Page.Products.Item(0).Name.GetText())` where
  `.AssertText("Keyboard")` already exists and would chain. This is the only group worth changing.

**Verdict: yes.** The chain is real and it is the default. The 26% that break out are mostly
asking questions the chain is not for.

---

## 2. Almost never — 8 tests out of 211

**8 test methods (3.8%) find an element themselves** — 12 calls, in 3 files:

| File | Find calls | |
|---|---|---|
| `Container/ContainerModuleTests.cs` | 9 | asserting a container scopes its children |
| `Collection/ScrollPatternProbeTests.cs` | 2 | a probe — inspecting the tree is its subject |
| `Text/EntryTests.cs` | 1 | reaching a read-only Entry |

A second, quieter leak: **7 methods construct a control object inline** rather than reading one off
a page object — 4 in `ContainerModuleTests`, 2 in `GridContainerTests`, 1 in
`ProductCollectionTests`:

```csharp
new Label<Pages.ContainerTestPage>(Page, "GridCellTopLeft").AssertText("Top left");
new Button<Pages.ContainerTestPage>(Page, "GridButton").Click();
```

**This is a page-object authoring gap, not a framework gap.** `ContainerTestPage` declares 13
container properties and none of the children those tests need, so the test has nowhere to get
`GridCellTopLeft` from and builds it on the spot. The framework already supports doing it
properly, and `ProductRow` is the worked example:

```csharp
public class ProductRow : ItemContainerBase<ProductCollection, ProductRow>
{
    public CheckBox<ProductRow> Selected => new(this, "ProductSelectedCheckBox");
    public Label<ProductRow>    Name     => new(this, "ProductNameLabel");
}
```

Tests written against `ProductRow` never touch an element. Tests written against
`ContainerTestPage` have to, because the properties were never declared.

Two caveats on the count. The probe tests exist to inspect the tree, so element finding is the
point of them, not a leak. And 2 of the flagged `DatePickerTests` methods are ones I added earlier
today (`Assert.Throws` around a refused date) — that is raw-assert usage, not element finding, and
it is the "no fluent equivalent" category: there is no chainable way to assert that an operation
throws.

**Verdict: effectively yes.** Discount probes and the framework's own meta-tests and the real
figure is a handful of container tests, all fixable by declaring the missing page-object
properties. No test is forced to find an element by a missing capability.

---

## 3. Usable — with one defect that costs real time

### What works

The Get/Wait/Assert trio is generated, so the API is uniform across controls: knowing one control
teaches you the rest. Assertions poll rather than sample, so tests are not littered with waits.
Scope nesting (page → container → item → control) composes without the test tracking anything.
Element lookup is resolved once per assertion and re-read, which is why an Android run is not
dominated by tree traffic.

### What gets in the way

**1. A failing assertion never tells you what it found.** This is the significant one.
`AssertionException` takes `expected` and `actual`, stores both as properties — and passes only
`message` to the base constructor. The `Actual` value is never rendered:

```
AssertionException : Expected TextContains to be 'Thursday, September 17, 2026'.
                     Locator: AutomationId:DateStatusLabel
```

The value it actually read is in the exception object and not in the text anyone sees. Working on
the date controls earlier today, every single failure required writing a throwaway probe to
discover the actual value — information the framework already had in hand. Putting `Actual` into
the message is a few lines in `AssertionException` and would have saved most of that.

**2. Two API families for the same property.** `DatePicker` exposes both:

```
GetDate      WaitDate      AssertDate         (whole-day comparison)
GetDateValue WaitDateValue AssertDateValue    (exact equality)
```

Same for `TimePicker`. The names give no clue which comparison you get, and the difference —
whole days versus exact ticks — is exactly the kind of thing that makes a test pass for the wrong
reason. The comment in the source explains it; the API does not.

**3. `Wait*` returns `bool` or `bool?` depending on the member.** `WaitChecked`, `WaitEnabled`,
`WaitLoaded` return `bool`; `WaitDateValue`, `WaitHeight`, `WaitItemCount` return `bool?`. Nothing
in the name predicts it, and the nullable ones force a caller into `== true` or `!.Value`.

**4. `Is*` returning `bool?` pushes awkwardness into tests.** It produces
`Assert.Equal(true, Page.Products.Item(0).Selected.IsChecked())` — comparing against a boxed
`true` because the value is `bool?`. Tri-state is defensible (`null` = unknown, and
[the focus audit](../../GetAttribute/audit-what-getattribute-can-answer.md) shows why that
distinction matters), but the fluent `AssertChecked(true)` is the better path and is right there.

**5. Container children are not reachable without declaring them.** `ContainerObjectBase` exposes
`FindElement`/`TryFindElement` returning raw `IMauiElement`, and nothing typed. A container knows
its own scope, so a typed child accessor —
`Page.TestGrid.Label("GridCellTopRight").AssertText(...)` — would close the last real source of
element finding in the suite.

### Verdict

**Usable, and in daily use.** 74% of a 211-test suite written entirely in the fluent style is the
answer to whether it works in practice. Nothing in the review needed a workaround for a missing
capability; the friction is in diagnostics and naming, not in what the API can express.

Defect 1 is the one worth fixing first, and it is the cheapest: it is a few lines, and it changes
every future debugging session in the repo.

---

## All five were implemented

Breaking changes were explicitly allowed - nothing depends on the library yet - so items 4 and 5
were taken rather than deferred.

### 1. A failing assertion now says what it found

`AssertionException` composes the message from `actual` instead of keeping it as a property no one
reads. One change, and every one of the 74 construction sites that passes `actual` benefits:

```
Expected TextContains to be 'Thursday, September 17, 2026'. Locator: AutomationId:DateStatusLabel
Actual: 'Selected Date: Monday, September 7, 2026'
```

Null, empty string and whitespace render distinctly - `(null)`, `(empty string)`, `'  '` - because
telling those apart is most of what a failure investigation is. Collections render as their first
ten entries.

### 2 and 3. Containers hand back control objects

`ContainerObjectBase` gained `Label(id)`, `Button(id)`, `Entry(id)`, `CheckBox(id)` and a generic
`Child<TControl>(id)`. The container is already a scope, so a child resolves within it - this is the
typed counterpart to `FindElement`, which is where a test stopped chaining and started doing
automation by hand.

`ContainerTestPage` then declares the ten children its tests had been reaching for, so
`Page.GridCellTopRight` exists where `TryFindElement(Locator.ByAutomationId("GridCellTopRight"))`
used to be. `GridContainerTests` and `ProductCollectionTests` lost their inline constructions the
same way.

Two of the remaining leaks turned out not to need new API at all:

- `Grid_DoesNotReachOtherContainersChildren` asserted a negative with `Assert.Null(...FindElement)`.
  `Page.TestGrid.Label("BorderChildLabel").AssertExists(false)` says the same thing and chains.
- `Entry_ReadOnly_IsReported` found an element only to ask whether the platform publishes
  editability. `IsReadOnly()` already returns `null` for exactly that case - the tri-state defended
  in defect 4 answers the question directly, so the lookup was redundant.

### 4. One `Date` family, not two

`GetDateValueCore` became `ReadDate` - a plain overridable helper. The generator emits its trio from
methods ending in `Core`, so dropping the suffix leaves exactly one public family: the hand-written
`GetDate` / `WaitDate` / `AssertDate`, comparing whole days. `TimePicker` the same, keeping its
tolerance comparison.

That kept the *correct* semantics rather than the generated ones. Exact equality would have made
`AssertDate(DateTime.Now)` fail against a picker showing today, which is the sort of thing the
duplicate family was hiding.

### 5. `Wait*` is `bool` everywhere

The generator emitted `bool?` for value waits and `bool` for state waits; `RunWaitWithElement`
returns `bool`, so the nullable could never actually be null. It was noise in 59 signatures.

The four Core interfaces declaring `bool?` changed with it, and so did the WPF, WinForms and
NativeAndroid implementations. Five of those did mean something by `null`: `if (expected is null)
return null` - the nullable-skip convention. MAUI already returned `true` there, so they now match:
nothing was asked, so nothing failed.

## Result

| | Before | After |
|---|---|---|
| Pure fluent tests | 158 / 211 (74%) | **165 / 211 (78%)** |
| Tests finding elements | 8 | **1** |
| Tests constructing controls inline | 7 | **0** |
| `Wait*` returning `bool?` | 59 | **0** |
| Public families for `DatePicker.Date` | 2 | **1** |

The one remaining element lookup is `ScrollPatternProbeTests`, which inspects the automation tree
for a living - finding elements is its subject, not a leak.

The raw-assert count moved only from 51 to 45, and that is the honest ceiling: what is left is the
two categories identified above as legitimate - structural questions about collections
(`Assert.Null(TryItem(9999))`) and the framework's own meta-tests of chaining
(`Assert.Same(page, afterCount.Parent.Parent)`). Neither should be fluent.

## Verification

- Solution builds clean; the one warning is a pre-existing `Application.MainPage` deprecation in
  the sample app.
- Unit, `Brinell.Maui.Tests`: 98 passed, 0 failed, 1 skipped.
- UI, Buttons + Text + Display + Toggle + DateTimes + Container: **115 / 115**, 1 m 6 s.

Worth noting the breadth: item 5 regenerated all 61 control files, so this touched every MAUI
control rather than only the ones the review named.
