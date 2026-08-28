# Fix: `WaitExists(false)` / `AssertExists(false)` / `AssertVisible(false)` cannot express absence

**Status:** IMPLEMENTED for `Brinell.Maui`. `Brinell.Html` still has the defect — see §8.
**Found by:** `ProductCollectionTests.Clear_ShowsEmptyState_ResetRestoresSeed`
**Affects:** `Brinell.Maui` (`ViewBase.tpl.cs`, `ControlBase.cs`), `Brinell.Generator` (`IsWaitAssertGenerator`), and the `Brinell.Html` equivalents

---

## 1. Symptom

A control bound to `IsVisible="false"` leaves the Windows automation tree
entirely. Asserting that it is gone is the natural thing to write:

```csharp
Page.Products.EmptyLabel.AssertExists(false);       // throws ElementNotFoundException
Page.Products.EmptyLabel.AssertVisible(false);      // throws ElementNotFoundException
Page.Products.EmptyLabel.WaitExists(false);         // throws ElementNotFoundException
```

All three throw instead of reporting `false`. The test currently works around it:

```csharp
// testsnew/Brinell.Maui.UITests/Tests/Collection/ProductCollectionTests.cs:164
Assert.False(Page.Products.EmptyLabel.IsExists());
```

`IsExists()` is the only member of the trio that answers the question, so the
fluent and polling forms are unusable for absence and every such assertion has
to drop out of the Brinell API into a bare xUnit `Assert`. That loses the
built-in polling, so it is also a latent flake: `IsExists()` samples once, with
no wait for the UI to settle.

## 2. Root cause

Two independent defects combine. Both live in the shared `Run*WithElement`
helpers, not in the generator's emitted text.

### 2.1 The element is resolved before the predicate runs

`srcnew/Brinell.Maui/Controls/Base/ViewBase.tpl.cs:134` and `:231`:

```csharp
protected bool RunWaitWithElement<T>(T? expected, Func<IMauiElement, bool> coreOperation, ...)
{
    if (expected == null) return true;

    return RunPoll(null, () =>
    {
        var element = FindElement();                // throws ElementNotFoundException
        EnsureVisible(element, DefaultTimeoutMs);   // throws TimeoutException
        return coreOperation(element);              // never reached when absent
    }, timeoutMs, caller);
}
```

`FindElement()` throws `ElementNotFoundException` when the element is missing —
which is precisely the state `expected: false` is asking about. The predicate
never runs. `RunPoll` swallows the exception each iteration, retries until
timeout, then rethrows `lastException`, so the caller sees
`ElementNotFoundException` rather than a failed comparison.

`EnsureVisible` compounds it: even if the element resolved, an invisible element
would be scrolled into view and then time out waiting to become visible. Asking
"are you invisible?" must not first demand that the element become visible.

Note the asymmetry that proves the intent. `IsExists()` — hand-written at
`srcnew/Brinell.Maui/Controls/ControlBase.cs:533` — uses the null-tolerant path:

```csharp
public bool IsExists() => IsExistsCore(TryFindElement()) == true;
```

The generated `Is*` members do the same (`ViewBase.gen.cs:26`). Only `Wait*` and
`Assert*` use `FindElement()`. Both `IsExistsCore` and `IsVisibleCore` already
accept `IMauiElement?` and handle null correctly:

```csharp
protected virtual bool? IsExistsCore(IMauiElement? element) => element != null;
protected virtual bool? IsVisibleCore(IMauiElement? element) => element?.Visible;
```

So the Core layer is already null-safe. The helpers simply never give it the
chance.

### 2.2 `expected == null` is used as "no expectation"

The same helpers, plus `RunAssert`, `RunAssertWithElement` and
`RunSetWithElement`, treat `null` as "skip the check". For a `bool?` parameter
that is defensible, but it is silently permissive: `AssertText(null)` passes
unconditionally. This is *not* the cause of the reported bug — `false` is not
`null`, so the guard is passed — but any fix must not convert one silent-pass
into another.

### 2.3 Interface signature is inconsistent

`srcnew/Brinell.Core/Interfaces/IElementObject.cs:36` declares:

```csharp
bool WaitExists(bool? expected, int? timeoutMs = null);
```

`expected` has no default here, while every generated `Wait*` uses
`bool? expected = true`. Worth aligning while in the area.

## 3. Proposed fix

Resolve the element *optionally* and let the Core predicate decide, exactly as
`Is*` already does. Suppress `EnsureVisible` for predicates that are about
presence or visibility.

### 3.1 Add null-tolerant helper overloads

In `srcnew/Brinell.Maui/Controls/Base/ViewBase.tpl.cs`, add siblings that take
`Func<IMauiElement?, ...>` and never throw on absence:

```csharp
/// <summary>
/// Polls a predicate that is meaningful when the element is absent.
/// The element is resolved with TryFindElement and may be null; visibility is
/// not forced, because the predicate may be asking about invisibility.
/// </summary>
protected bool RunWaitWithOptionalElement<T>(T? expected,
    Func<IMauiElement?, bool> coreOperation,
    int? timeoutMs = null, [CallerMemberName] string? caller = null)
{
    if (expected == null) return true;

    return RunPoll(null, () => coreOperation(TryFindElement()), timeoutMs, caller);
}

protected TScope RunAssertWithOptionalElement<T>(T? expected,
    Func<IMauiElement?, T?> getActual, Func<T?, T?, bool> compare,
    string? message = null, int? timeoutMs = null,
    [CallerMemberName] string? caller = null)
{
    if (expected == null) return ContainingScope;

    RunPoll(null, () =>
    {
        var actual = getActual(TryFindElement());
        if (!compare(actual, expected))
        {
            throw new AssertionException(message ?? "Assert exception", expected, actual);
        }
        return true;
    }, timeoutMs, caller);

    return ContainingScope;
}
```

Adding overloads rather than changing `RunWaitWithElement` /
`RunAssertWithElement` in place keeps the existing semantics for value
assertions. `AssertText` *should* fail loudly when the element is missing —
"the label is absent" is not a passing result for a text comparison. Only
presence/visibility predicates want the tolerant path.

Mirror these on `ContainerObjectBase`
(`srcnew/Brinell.Maui/Containers/ContainerObjectBase.cs:390` and `:444`,
returning `TSelf`) using an optional container-root resolution, and on
`srcnew/Brinell.Html/Controls/ControlBase.cs:109` / `:202`.

### 3.2 Mark which Core methods are absence-tolerant

The generator must know which trio gets the tolerant helper. Two options:

**Option A — attribute (preferred).** Add to `Brinell.Core.Interfaces`:

```csharp
/// <summary>
/// Marks an Is*Core query as meaningful when the element is absent, so the
/// generated Wait*/Assert* resolve the element optionally instead of throwing.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AbsenceTolerantAttribute : Attribute;
```

Applied to the two Core methods that need it:

```csharp
[AbsenceTolerant]
protected virtual bool? IsExistsCore(IMauiElement? element) => element != null;

[AbsenceTolerant]
protected virtual bool? IsVisibleCore(IMauiElement? element) => element?.Visible;
```

This follows the precedent already set by `[GenerateComparisons]`, which
`IsWaitAssertGenerator.ExtractComparisons` reads syntactically
(`IsWaitAssertGenerator.cs:128`) without a semantic model. Detection is the same
shape and can reuse that matching logic.

**Option B — infer from the parameter type.** `Matches` already inspects the
first parameter (`IsWaitAssertGenerator.cs:68-71`); a Core method declaring
`IMauiElement?` rather than `IMauiElement` is announcing it tolerates null.
Requires no new attribute, but it is implicit, and several Core methods are
nullable-typed without having thought about absence. Option A is worth the extra
declaration.

### 3.3 Generator changes

In `tools/Brinell.Generator/Generators/IsWaitAssertGenerator.cs`:

1. `Extract` — read the attribute and set a new `MethodInfo.IsAbsenceTolerant`
   flag, alongside the existing `Comparisons` handling.
2. `GenerateState` / `GenerateWaitMethod` / `GenerateAssertMethod` — thread the
   flag through and pick the helper name, exactly as `fluentReturnType` is
   threaded today:

```csharp
var waitHelper   = coreMethod.IsAbsenceTolerant
    ? "RunWaitWithOptionalElement" : "RunWaitWithElement";
var assertHelper = coreMethod.IsAbsenceTolerant
    ? "RunAssertWithOptionalElement" : "RunAssertWithElement";
```

3. The current generated form is `element => IsVisibleCore(element) == expected!.Value`.
   The null-forgiving `!` is safe once the `expected == null` guard has run, but
   it should be reviewed rather than copied blindly into the new path.

`ControlBase.cs`'s hand-written `Exists` region (`:525-560`) is not generated and
must be updated by hand to use the new helpers.

### 3.4 Regenerate

Run `Tools/Scripts/CreateMaui.bat`, which regenerates all 30 templates. Expect a
diff only in the `Visible` and `Exists` regions.

## 4. Expected result

```csharp
Page.Products.EmptyLabel.AssertExists(false);   // passes when absent, polls
Page.Products.EmptyLabel.AssertVisible(false);  // passes when absent or hidden
Page.Products.EmptyLabel.WaitExists(false);     // returns true, no throw
Page.Products.EmptyLabel.AssertText("x");       // still throws when absent (unchanged)
```

## 5. Tests

**Unit** (`testsnew/Brinell.Maui.Tests`) — Moq an `IMauiScope` whose
`TryFindElement` returns `null`:

- `AssertExists(false)` returns the scope, does not throw
- `AssertVisible(false)` returns the scope, does not throw
- `WaitExists(false)` returns `true`
- `AssertExists(true)` still throws for an absent element
- `AssertText("x")` still throws for an absent element (regression guard on 3.1)
- an element that is present but invisible satisfies `AssertVisible(false)`
  without `EnsureVisible` being invoked

**Generator** (`testsnew/Brinell.Generator.Tests`) — mirroring
`FluentReturnTypeTests`:

- `[AbsenceTolerant]` on an `Is*Core` emits `RunWaitWithOptionalElement` /
  `RunAssertWithOptionalElement`
- absent attribute emits the existing helper names
- the flag composes with the resolved fluent return type (`TScope` vs `TSelf`)

**UI** (`testsnew/Brinell.Maui.UITests`) — replace the workaround at
`ProductCollectionTests.cs:164`:

```csharp
Page.Products.EmptyLabel.AssertExists(false);
```

and delete the explanatory comment at `:160-163`.

## 6. Scope and risk

Low. The change is additive at the runtime layer — existing helpers keep their
semantics, and only two Core methods opt into the new path. The regeneration
diff should be confined to the `Visible` and `Exists` regions across the
generated controls.

The one judgement call is 3.1: whether value assertions such as `AssertText`
should also tolerate absence. They should not. "The element is gone" is a
genuine failure for a text comparison, and making it pass silently would be a
worse bug than the one being fixed.

## 8. Implementation record

Implemented as proposed, with one deviation and one deliberate omission.

### What was built

| Piece | Where |
|---|---|
| `[AbsenceTolerant]` attribute (Option A) | `srcnew/Brinell.Core/Interfaces/AbsenceTolerantAttribute.cs` |
| `RunWaitWithOptionalElement` / `RunAssertWithOptionalElement` | `ViewBase.tpl.cs` and `Controls/ControlBase.cs` |
| `MethodInfo.IsAbsenceTolerant` + syntactic attribute detection | `tools/Brinell.Generator/` |
| Helper selection in the emitters | `IsWaitAssertGenerator.GenerateWaitMethod` / `GenerateAssertMethod` |
| 8 generator tests | `testsnew/Brinell.Generator.Tests/Generators/AbsenceToleranceTests.cs` |

`IsVisibleCore` in `ViewBase.tpl.cs` carries the attribute, so the generated `Visible` trio
picks up the tolerant helpers. The regeneration diff was **one file, two lines** — exactly
the `Visible` region, confirming nothing else changed shape.

### Deviation: three hand-written regions, not one

§3.3 said `ControlBase.cs`'s `Exists` region is hand-written and needs updating by hand.
There were **three** such regions, not one:

- `ControlBase.cs` — `Exists` **and** `Visible` (its `IsVisibleCore` is non-virtual and its
  `Wait`/`Assert` members are hand-written, so the attribute does nothing there)
- `ViewBase.tpl.cs` — `Exists`, which uses `IsExistsBase` rather than an `Is*Core` method
  and so is invisible to the generator entirely

All were updated by hand. `AssertExists`/`AssertVisible` also gained the diagnostic message
the generated members already had; they were passing `null`.

### Omission: `ContainerObjectBase` needed no change

§3.1 said to mirror the helpers there. It turned out already correct — it resolves through
`TryGetContainerRoot()` and polls, so `AssertExists(false)` and `AssertVisible(false)`
already reported absence rather than throwing. Left alone.

### Not done: `Brinell.Html`

`srcnew/Brinell.Html/Controls/ControlBase.cs` has the identical defect at `:540` and `:557`.
It was **not** fixed: no Html test or caller uses the negative form today, and there is no
way to verify a change there against a running app the way the MAUI fix was verified. The
fix is mechanical and this document describes it; do it when something needs it.

### Verification

| Check | Result |
|---|---|
| `dotnet build Brinell.sln` | succeeded |
| `Brinell.Generator.Tests` | **104 passed** (96 before, +8 new) |
| Regeneration diff | 1 file, 2 lines — `Visible` region only |
| UI: Navigation + Container + Collection | **46 passed, 2 skipped, 0 failed** |
| `Brinell.Maui.Tests` | 62 passed, 8 failed — **identical to baseline** |

Both workarounds this defect had forced were removed and replaced with the fluent form,
then verified against the real Windows app:

- `NavigationControlTests` — a local `WaitUntilMenuClosed()` polling helper, deleted; two
  call sites now use `WaitExists(false, ...)`, and the closed-state check uses
  `AssertExists(false)`
- `ProductCollectionTests.Clear_ShowsEmptyState_ResetRestoresSeed` — the original report
  site; `Assert.False(...IsExists())` is now `AssertExists(false)`

## 7. Related

- `.my/Containers/sample-app-ui-tests-design.md` §8.2 records this as a known
  limitation of the delivered work.
- Unrelated but adjacent: `SetGenerator` matches only the `Set*` name prefix, so
  `EnterCore` falls through to `ActionGenerator` and loses the null-skip guard.
  Same file family, separate decision.
