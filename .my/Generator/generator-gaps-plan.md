# Generator Gaps Plan: Issues Found While Migrating Controls to `.tpl.cs`

**Date:** 2026-08-18
**Scope:** Fix the generator limitations and traps discovered while converting
`ViewBase`, `FocusableControlBase`, and `ClickableControlBase` to the
`.tpl.cs` / `.gen.cs` format under `srcnew/Brinell.Maui/Controls/Base/`.

**Related:** [code-generator-plan.md](code-generator-plan.md) (wrapper generation),
[property-handler-plan.md](property-handler-plan.md) (Is/Wait/Assert generation).
This plan covers the gaps those two left open, discovered in practice.

---

## 1. Problem Statement

The generator works for the three base classes now migrated, but converting real
controls (`ImageButton`, `Entry`, `Label`) hits limits that force members to stay
hand-written. The issues fall into two buckets:

- **Missing generators** — whole categories (`Set*`, non-equality asserts) have no support.
- **Fidelity loss** — generated members differ from what they replace.

Each issue below is confirmed against the current code, not hypothetical.

**Not in scope:** Core methods that don't meet the `protected virtual` + element-first
contract are skipped silently. That is fixed per-control while converting, by bringing
the method to the contract — see the `convert-control` skill. No generator change.

---

## 2. Issues

### Issue 1 — No `Set*` generator

There is no generator for value-writing methods. `ControlObjectGenerator.CreateDefault()`
registers only `IsWaitAssertGenerator` and `ActionGenerator`.

`ViewBase` already provides the runtime helper (`RunSetWithElement<T>`), and the
pattern is uniform in hand-written controls:

```csharp
public TScope SetText(string? text, int? timeoutMs = null)
{
    return RunSetWithElement(text, element => SetTextCore(element, text!, timeoutMs), timeoutMs);
}
```

Blocks converting `Entry` (`SetText`, `Enter`, `Append`), and any control with
settable values (`Slider`, `Stepper`, `Picker`, `DatePicker`, `TimePicker`).

**Design:** new `SetGenerator : IMemberGenerator`, matching
`protected virtual void Set*Core(IMauiElement element, T value, int? timeoutMs = null)`.
Emits `Set{Name}(T value, int? timeoutMs = null)` wrapping `RunSetWithElement`.
The nullable-skip behaviour is already inside `RunSetWithElement` (returns
`ContainingScope` when value is null), so the wrapper stays a one-liner.

Register **before** `ActionGenerator`, which would otherwise claim `Set*Core` as a
plain action and emit the wrong body.

**Acceptance:**
- [x] `SetGenerator` matches `Set*Core` with an element first param and one value param
- [x] Generated wrapper uses `RunSetWithElement` and forwards `timeoutMs` when declared
- [x] Registered ahead of `ActionGenerator` in `CreateDefault()`
- [ ] `Entry.SetText` / `Enter` / `Append` generate identically to the current hand-written versions
      *(deferred — requires converting `Entry.cs`, which is separate work)*
- [x] Unit tests in `Brinell.Generator.Tests` mirroring `ActionGeneratorTests`

---

### Issue 2 — Only equality comparison is supported

`IsWaitAssertGenerator` hardcodes `(actual, expected1) => (actual == expected1)`.
Anything else must stay hand-written:

| Member | Where it lives today |
|---|---|
| `AssertTextContains` | `ControlBase` (all 5 platforms) |
| `AssertTextStartsWith` / `AssertTextEndsWith` | `ControlBase`, `ITextControlObject` |
| `AssertTextEmpty` | `ControlBase`, `ITextControlObject` |
| `WaitTextContains` | `Entry`, `ITextControlObject` |

This is why `IControlObject` had to shed its text members before
`FocusableControlBase` could implement `IFocusableControlObject` — see §4 below.

**Design:** attribute on the Core method declaring which comparisons to emit, e.g.

```csharp
[GenerateComparisons(Comparison.Equals | Comparison.Contains | Comparison.StartsWith)]
protected virtual string? GetTextCore(IMauiElement element)
```

Emits `AssertText`, `AssertTextContains`, `AssertTextStartsWith` from one Core
method. Default stays `Equals` so existing templates are unaffected.

**Acceptance:**
- [x] `Contains`, `StartsWith`, `EndsWith`, `Empty` comparison variants
- [x] Default (no attribute) emits only `Equals` — no change to current output
- [x] Generated `Wait*` variants match (`WaitTextContains`)
- [ ] `ITextControlObject` is fully satisfiable from generated members
      *(deferred — depends on the §4 text-hierarchy decision)*

---

### Issue 3 — Custom assertion messages are lost

Generated `Assert*` always passes `null` for the message
(`IsWaitAssertGenerator.cs`, both `GenerateAssertMethod` and `GenerateGetter`).
Hand-written asserts carry diagnostics:

```csharp
message ?? $"Expected element {(expected.Value ? "to be clickable" : "not to be clickable")}. Locator: {Locator}"
```

Already lost in the migration: `ClickableControlBase.AssertClickable` and
`FocusableControlBase.AssertFocused` both had messages naming the state and locator;
their generated replacements report a bare assertion failure. This is a real
regression in test-failure diagnostics for every migrated control.

**Fix (preferred):** synthesize a default message from the property name and locator
so generated asserts are at least as useful as before:

```csharp
message ?? $"Expected {PropertyName} to be '{expected}'. Locator: {Locator}"
```

**Fix (optional):** `[AssertMessage("...")]` on the Core method for full control.

**Acceptance:**
- [x] Generated `Assert*` passes a synthesized message including property name and `Locator`
- [x] The caller's `message` parameter still takes precedence
- [x] `AssertClickable` / `AssertFocused` failure text is comparable to pre-migration

---

### Issue 4 — Guard methods named `*Core` generate junk public actions

`EnsureEnabledCore` / `EnsureClickableCore` are internal guards, but they end in
`Core`, so as `protected virtual` they match `ActionGenerator` and emit meaningless
public `EnsureEnabled()` / `EnsureClickable()` methods.

Current workaround in `ClickableControlBase.tpl.cs`: declare them `protected`
without `virtual`. This works but **costs subclass overridability** — the old
`ClickableControlBase.EnsureClickableCore` was virtual, so this is a real (if
currently unused) capability regression.

**Fix:** treat `Ensure` as a reserved prefix. Any method starting with `Ensure` is a
guard by convention, so `ActionGenerator.Matches()` excludes it outright — the same
way it already excludes `Is*Core` and `Get*Core` as belonging to another family.

```csharp
// ActionGenerator.Matches()
// Exclude Ensure*Core guards — internal helpers, not actions.
if (methodName.StartsWith("Ensure"))
    return false;
```

No attribute needed, and guards go back to `protected virtual` with overridability
restored. The convention already holds across the codebase: every `Ensure*` method
in the MAUI, WPF, WinForms, Stride, and HTML `ControlBase` classes is a guard.

**Acceptance:**
- [x] `ActionGenerator` skips any method whose name starts with `Ensure`
- [x] `EnsureEnabledCore` / `EnsureClickableCore` restored to `protected virtual`
- [x] No public `Ensure*` members in `ClickableControlBase.gen.cs`
- [x] `convert-control` skill documents `Ensure*` as reserved, replacing the
      "keep guards non-virtual" workaround

---

### Issue 5 — `override` of a base Core method double-generates

`Label.GetTextCore` overrides `ControlBase.GetTextCore`. The wrapper already exists
on the base; the analyzer doesn't check for `override`, so converting `Label` would
emit a duplicate `GetText` on the derived class (CS0111, or a hiding warning).

**Fix:** skip methods carrying `override` — the base class already generated the
wrapper. Cheap and self-contained.

**Acceptance:**
- [x] Core methods with `override` are skipped by all generators
- [ ] Skip is reported as informational, not a warning (it's expected)
      *(not implemented — the generator emits no per-method diagnostics at all;
      would need a reporting channel, which no issue in this plan introduced)*
- [ ] `Label.tpl.cs` converts and builds with no duplicate members
      *(deferred — requires converting `Label.cs`, which is separate work)*

---

### Issue 6 — `ActionGenerator` doc comment describes behaviour it doesn't have

`ActionGenerator.cs` says wrappers delegate "via `RunDoWithElement` and the
clickable guard." No guard is emitted — the body is exactly
`RunDoWithElement(element => { XCore(element); })`.

This misled the `ClickableControlBase` migration and cost debugging time. The guard
now lives inside each Core body, which is the right call, but the comment must match.

**Fix:** correct the comment on the class and on `Generate`. Documentation-only.

**Acceptance:**
- [x] Comments describe the emitted body accurately, with no mention of a guard

---

### Issue 7 — Multiple `Get*Core` overloads collide

Two `Get*Core` methods differing only in parameters both derive the same property
name and emit colliding `Get{Name}` / `Wait{Name}` / `Assert{Name}` members.
Not hit yet, but it's a latent CS0111 waiting for the first control that needs one.

**Fix:** detect duplicate generated names and fail with a clear message rather than
emitting uncompilable code.

**Acceptance:**
- [x] Duplicate generated member names produce a clear error naming both Core methods
- [x] Error fires before writing the `.gen.cs`

---

## 3. Suggested Order

Grouped by value-to-effort, not strict dependency.

**Phase 1 — Small generator fixes (do first)**

1. Issue 4 — exclude `Ensure*` from `ActionGenerator`
2. Issue 5 — skip `override`
3. Issue 6 — fix the misleading comment
4. Issue 7 — detect name collisions

All four are a few lines each and remove traps from the conversion path. Issue 4
also lets the guards in `ClickableControlBase.tpl.cs` go back to `protected virtual`.

**Phase 2 — Restore fidelity (medium)**

5. Issue 3 — assertion messages

**Phase 3 — Widen coverage (larger)**

6. Issue 1 — `SetGenerator`
7. Issue 2 — comparison variants

Phase 3 is what unblocks `Entry` and the text-bearing controls.

---

## 4. Related Open Decision: text in the `ViewBase` hierarchy

Not a generator bug, but it blocks migration and belongs on the record.

`ViewBase` has no text support, unlike the old `ControlBase` (which has
`GetTextCore` plus the `AssertText*` family). To let `FocusableControlBase`
implement `IFocusableControlObject`, the text members were removed from
`IControlObject` (verified safe: nothing consumes the interface as a type, all six
implementations expose the methods as ordinary `public` members, and the UAT
reflection paths resolve against concrete types).

Still undecided: **how text arrives in the new hierarchy.** Options:

- **A.** Add `GetTextCore` to a new `Base/ControlBase.tpl.cs` between `ViewBase` and
  `FocusableControlBase`. Mirrors the old shape; needs Issue 2 for the `AssertText*` family.
- **B.** Put text on an opt-in mixin/interface so non-text controls (`ProgressBar`,
  `ActivityIndicator`) don't inherit meaningless text members.
- **C.** Leave text per-control.

Option A is the smallest step and matches existing structure; B is cleaner long-term.
Decide before converting `Label`, `Entry`, or any other text-bearing control.

---

## 5. Out of Scope

- MSBuild integration (open in [code-generator-plan.md](code-generator-plan.md) §3)
- Migrating the remaining controls — this plan fixes the tooling; conversion is
  driven by the `convert-control` skill in `.claude/skills/convert-control/`
- The old `Controls/*.cs` hierarchy, which stays until every control has moved
