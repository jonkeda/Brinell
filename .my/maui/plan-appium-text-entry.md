# Plan: Bring Appium Text Entry Up To The FlaUI Standard

**Date:** 2026-09-01
**Status:** 🟡 **Partly done — Android 9 → 22 of 28.** See §0 for what is built, what was
skipped, and what remains.
**Scope:** `srcnew/Brinell.Maui.Appium/AppiumMauiElement.cs`, and `Entry.SetTextCore` /
`ClearCore` where they assume a Windows-shaped element
**Subject:** `Entry`, `Editor`, `SearchBar` — currently **14 / 28 on Android**, 27 / 28 on
Windows

---

## 0. Progress

| | Windows Text | Android Text | Buttons (both) | Unit |
|---|---|---|---|---|
| Plan written | 27 / 28 | 9 / 28 | 11 / 11 | 84 pass / 6 fail |
| **Now** | **27 / 28** | **22 / 28** | **11 / 11** | **85 pass / 5 fail** |

### Steps

| Step | State |
|---|---|
| 1 — Isolate which call fails | ⚠️ **Reached by another route**, not the probe described |
| 2 — Give the Appium element the FlaUI shape (verify + retry) | 🚫 **Rejected** — see below |
| 3 — Let `SetTextCore` express intent, not mechanism | ✅ **Done, and further than planned** |
| 4 — Verify both platforms | ✅ Done for what changed |
| 5 — iOS | ❌ Not started (no runner) |

### What was actually done

**Step 3 went furthest, because `INestedTextElement` was removed entirely.** The plan only
said "do not port it to Appium". In fact it could go altogether: FlaUI's `Clear()` and
`SendKeys(SetValue)` *already* ran the nested ladder internally, so the control-side
`if (element is INestedTextElement...)` branches were re-doing work the element did anyway.
The one genuine gap was *reading*, so the nested-Edit lookup folded into FlaUI's `Text`
property.

Controls now say `element.Text` / `element.Clear()` / `element.SendKeys(...)` and the element
resolves its own platform's structure — which is exactly step 3's goal, reached by deletion
rather than by adding a `SetValue` contract member. Six control branches and one interface
gone.

**Appium is Android/iOS only**, per the same instruction. Removed: the
`platform = MauiPlatform.Windows` **default parameter** (the origin of the earlier locator
bug), the `OpenQA.Selenium.Appium.Windows` using, and the Windows locator branch — which now
throws pointing at `Brinell.Maui.FlaUI`.

### The cause, found sideways

§3 listed three suspects: focus, `Clear()`, and the trailing `"\t"`. **It was the tab**, and
removing `INestedTextElement` is what exposed it.

`Entry.SetTextCore` ended with `element.SendKeys("\t")` to commit the value. That is *global
keyboard input*, which the Windows interaction policy blocks by default — but the old
nested-text branch returned before reaching it, so it never fired. Remove the branch and
**Windows collapsed to 15 / 28** until the tab went too.

It was never needed: `SetValue` writes through the platform's own value mechanism (UIA
`ValuePattern`, `replaceElementValue` on Android) and requires no commit keystroke. On Android
it was actively harmful — typing a literal tab into the field.

So one line explained a Windows regression *and* a large part of the Android failure.

### Step 2 is rejected: `SetText` stays simple

**Decision: no verify/retry inside text setting, on either driver.** The plan's §2 argued for
porting FlaUI's attempt/verify/retry ladder to Appium. That is not happening, and the ladder
has been removed from FlaUI too.

Three reasons, in order of weight:

1. **It was not what fixed anything.** With the trailing tab removed, Android went 14 → 22
   with no verification at all. The ladder was a theory about the cause; the cause was one
   stray keystroke.
2. **It had already gone dead.** Removing `INestedTextElement` left
   `ClearWithFallback` (a 3-attempt loop), `GetNestedText`, `FindNestedTextBox` and
   `SetTextWithFallback` with **zero callers** — 125 lines. They were deleted rather than
   revived. Machinery nothing calls is not a safety net, it is a liability that looks like one.
3. **Simple is the requirement.** `SetTextCore` is now two lines — clear, then write — and
   each driver decides how. Retry logic inside a setter hides which attempt succeeded and makes
   a partially-applied write indistinguishable from a clean one.

What remains, and is enough: `TrySetTextValue` walks the wrapper and the nested Edit, because
finding the right element is *resolution*, not retry. Waiting is `RunPoll`'s job, one level up
(see [plan-wait-for-readiness.md](plan-wait-for-readiness.md)).

**Revisit only on evidence:** a write that lands silently wrong, demonstrated by a test.
Not on the theory that it might.

**Verified after the deletion** — nothing regressed:

| | Result |
|---|---|
| Windows Text | 27 / 28 |
| Windows Buttons | 11 / 11 |
| Android Text | 22 / 28 |
| Unit suite | 85 pass / 5 pre-existing fail |

### Remaining: 6 failures, all `Clear` / `ResetAll`

`Editor_Clear`, `Editor_LineBreaks`, `Editor_ResetAll`, `Entry_ResetAll`, `SearchBar_Clear`,
`SearchBar_ResetAll`.

They fail with **`Page 'TextTestPage' is not loaded`** — the *page*, not the element. That is
a different failure from the write problems this plan was written for.

**Ruled out:** the soft keyboard. It is up during these tests, but the page root is still
present in the UI tree (checked on the device).

**Not yet established.** Next diagnostic: dump the tree at the moment of failure rather than
after the session ends, and check whether the app is on the Text page at all — a `ResetAll`
button that navigates, or an element tap that dismissed the page, would both present this way.

---

## 1. The measured position

| Platform | Text tests | Notes |
|---|---|---|
| Windows (FlaUI) | 27 / 28 | The one failure, `SearchBar_IsVisible`, predates this work |
| Android (Appium) | **14 / 28** | Was 9 / 28 before the `mobile: setValue` fix below |

Every Android failure is a *write* — `SetText`, `Clear`, `TypeText`, `CharacterCount`. Reads
and existence checks pass.

**Already fixed on the way here:** `mobile: setValue` is an XCUITest name that UiAutomator2
does not have; Android needs `mobile: replaceElementValue`. Calling the iOS name on Android
failed with *"did you mean 'mobile: setUiMode'? Make sure the installed
AndroidUiautomator2Driver is up-to-date"* — a message that points at the driver version rather
than at the wrong API for the platform. That fix moved 5 tests.

**The remaining symptom:** the text reaches the widget, but the bound status label never
updates — `Expected TextContains to be 'Hello Entry'`. Verified by hand that
`adb shell input text` **does** update the label, so the app and its two-way binding are
correct. Something about how Brinell writes the value does not reach MAUI's `TextChanged`.

---

## 2. Why Windows works and Android does not

The two element implementations are not the same *kind* of code, and that is the real finding.

### FlaUI — an attempt/verify ladder

```csharp
public bool ClearWithFallback()
{
    if (TrySetTextValue(string.Empty) && IsEmpty(GetNestedText()))   // ← verifies
        return true;

    for (var attempt = 0; attempt < 3; attempt++)                     // ← retries
    {
        if (IsEmpty(GetNestedText())) return true;
        foreach (var target in GetTextValueTargets())                 // ← candidate ladder
        { ... }
    }
}
```

Three properties, none accidental:

1. **A candidate ladder** — tries the wrapper, then the nested `Edit` descendant.
2. **Verification** — every write is followed by a read to confirm it landed.
3. **Bounded retry** — a write that did not take is attempted again.

### Appium — a one-shot pass-through

```csharp
public void Clear() => _element.Clear();
public void SendKeys(string text, TextInputMethod method) { ... one call ... }
```

No ladder, no verification, no retry. **It reports success by not throwing**, which is exactly
the failure mode this codebase has been removing everywhere else (`ElementClicker.TryClick`,
the `LegacyIAccessible` rung, the selection-item probe). A write that silently does not take is
indistinguishable from one that worked.

### What does *not* need porting

Windows needs the candidate ladder because **MAUI wraps controls on WinUI** — the AutomationId
sits on a wrapper and the real text field is a nested `Edit`. That is what `INestedTextElement`
exists for.

**Android has no such nesting.** Measured on the device: `TestEntry` and `TestEditor` are both
`class="android.widget.EditText"` carrying the AutomationId directly. So `INestedTextElement`
should stay unimplemented on Appium — porting it would add a search that always finds the
element it started from.

**Port the shape (verify, retry), not the mechanics (nested lookup, ValuePattern).**

---

## 3. What is not yet known

The plan must not guess at this. One experiment already failed:

> Switching Android from `replaceElementValue` to Appium `SendKeys` — on the theory that
> typing would fire the input pipeline the way `adb shell input text` did — **changed nothing**
> (14 / 14 before and after). It was reverted rather than left in on an unproven rationale.

So `adb shell input text` updates the binding and neither Appium route does. The difference is
not yet explained, and three candidates remain untested:

| Candidate | Why plausible |
|---|---|
| **Focus** | `adb input text` types into the *focused* field; the test taps first. `SetTextCore` never focuses — Windows gets focus inside FlaUI's `SendKeys`, Android may not |
| **`element.Clear()`** | Runs before every write. If it fails or steals focus, the write lands somewhere else |
| **The trailing `"\t"`** | `SetTextCore` sends a Tab to commit the value. That is a desktop idiom; on Android it may do nothing, or something unwanted |

---

## 4. Steps

### Step 1 — Isolate which call fails, before changing anything

`SetTextCore` is three operations. Determine which one breaks the chain, by driving the
element through Appium directly in a scratch test:

1. `Clear()` alone → read the widget text. Did it empty?
2. `replaceElementValue` alone, no clear, no tab → read widget text **and** the bound label.
3. Same, but `Click()` the element first → does focus change the outcome?
4. `SendKeys` alone with focus → does the label update?

**Expected to identify one of the §3 candidates.** If none of them explains it, stop and
re-diagnose rather than proceeding — the same rule that saved time on
[plan-wait-for-readiness.md](plan-wait-for-readiness.md).

### Step 2 — Give the Appium element the FlaUI shape

Whatever step 1 finds, the durable fix is the same one Windows already has: **attempt, verify,
fall back.**

```csharp
public bool SetTextWithVerification(string text)
{
    foreach (var attempt in TextWriteStrategies())   // replaceElementValue → focus+type → …
    {
        try { attempt(text); } catch { continue; }
        if (ReadBack() == text) return true;         // ← the part Appium lacks today
    }
    return false;
}
```

The strategy order comes from step 1's evidence, not from this document. The invariant is that
**a write is not reported as succeeding until a read confirms it.**

### Step 3 — Let `SetTextCore` express intent, not mechanism

`Entry.SetTextCore` currently hard-codes a Windows-shaped sequence:

```csharp
element.Clear();
element.SendKeys(text, TextInputMethod.SetValue);
element.SendKeys("\t");        // commit — a desktop idiom
```

The control should say *"make the value this"* and let each element decide how. Add a
`SetValue(string)` to the element contract, implemented per driver, and have `SetTextCore` call
that — with the current three-step sequence kept as the fallback for drivers that do not
implement it.

This is the §3 rule of the parent plan applied to text: **an element never knows what a MAUI
view means; a control never knows how a platform writes a value.**

### Step 4 — Verify, both platforms

1. Android `Tests.Text` → target 28 / 28.
2. Windows `Tests.Text` → must stay 27 / 28. **The `SetValue` contract change touches FlaUI
   too**, so this is a real regression risk, not a formality.
3. Run each twice — a verification-and-retry change must not introduce variance.

### Step 5 — iOS

iOS is **build-only and unverified**; no test has ever run there. `mobile: setValue` is the
correct XCUITest name and is already wired, so iOS is expected to work — but *expected* is not
*verified*, and this plan must not claim otherwise.

When a macOS runner exists: run `Tests.Text` on iOS and treat any failure as its own
investigation. iOS wraps text controls differently again (`XCUIElementTypeTextField` with the
label as a sibling), so the candidate-ladder question may return there even though Android
did not need it.

---

## 5. Why this matters beyond text

**This is the fourth platform-specific API used unconditionally in the Appium driver**, after:

- `ToBy()` defaulting to `MauiPlatform.Windows` — every AutomationId resolved as an
  AccessibilityId on Android
- `SupportsSelectionItemPattern` — true for every Android view, causing double taps
- `SupportsTogglePattern` — same defect, gated on `checkable` now

The pattern is consistent: **the Appium element was written against one platform's semantics
and applied to all of them.** Each instance was invisible until a test ran on a device, and
each presented as something other than what it was.

Worth a review rule rather than four more fixes: *in `Brinell.Maui.Appium`, any driver script
name or attribute name is platform-specific until proven otherwise, and belongs in a `switch`
on `_driver.Platform`.*

**The structural fix is now in** (§0): the `MauiPlatform.Windows` **default parameter** on
`AppiumMauiDriver` is gone, so a caller must state the platform, and the Windows locator branch
throws rather than guessing. Three of the four instances above began as a Windows-shaped
default silently applying to mobile; removing the default makes that class of mistake harder
to write than to notice.

**A fifth, of the same family, surfaced while doing this** — and it ran the other way. The
trailing `SendKeys("\t")` in `Entry.SetTextCore` was a *desktop* idiom sitting in a
**platform-neutral control**, not in a driver. It broke Windows (policy-blocked global
keyboard) and Android (a literal tab in the field) at once. So the rule generalizes: a commit
keystroke, a modifier, a focus dance — any input idiom in a control object is a platform
assumption until proven otherwise, and belongs in the element.

---

## 6. Risks

| Risk | Response |
|---|---|
| Step 2 adds retries that mask a real failure | Verification is a read-back, not a timeout. A write that never lands still fails, and says which strategy was tried |
| The `SetValue` contract change regresses Windows | Step 4 runs Windows explicitly, twice. FlaUI already has the behaviour, so its implementation is a rename not a rewrite |
| Read-back is unreliable on some controls | Then the control cannot be verified and the strategy must say so, rather than reporting a write it cannot confirm |
| Chasing this without evidence | Step 1 is diagnosis-only and gates everything else. One rationale has already failed here and was reverted |

---

## Finding: is the nested-text machinery needed for simple controls?

**Question asked:** do we need `TrySetTextValue` and the nested text wrappers for the simple
controls?

**Answered by measurement, not opinion.** A temporary probe logged the UIA shape of each text
control the Windows suite drives:

| Control | Windows control type | Self writable | Nested edit present |
|---|---|---|---|
| `TestEntry` | `Edit` | ✅ | ❌ |
| `TestEditor` | `Edit` | ✅ | ❌ |
| `TestSearchBar` | **`Group`** | ❌ | ✅ |

**Answer: no for the simple controls, yes for SearchBar.** MAUI maps `Entry` and `Editor`
straight to a WinUI `Edit`, which carries its own writable Value pattern. `SearchBar` becomes an
AutoSuggestBox, which surfaces as a `Group` with **no Value pattern at all** — the real field is
nested inside it. Deleting the descendant lookup would make SearchBar unwritable on Windows.

So the machinery is not generic caution; it earns its place for exactly one control shape. What
*was* removable is the shape it had:

- `GetTextValueTargets()` — deleted. It built a candidate list with the nested edit **first**,
  so every `Entry`/`Editor` write paid a `FindFirstDescendant` that never found anything and
  never mattered.
- `TrySetTextValue` now tries `_element` first and looks for a wrapper **only once the direct
  write is ruled out**. Same behaviour on all three controls, no wasted tree search on the
  common path, and the code now reads as the rule it embodies.

Nothing platform-specific leaked upward: `Entry.SetTextCore` is still `Clear()` +
`SendKeys(SetValue)`, with no branch and no verify/retry.

**Verified:** Windows Text 27/28 — unchanged. The one failure is the pre-existing
`SearchBar_IsVisible_ReturnsTrue` (`Expected Visible to be 'True'`), which is a visibility
probe against the same `Group` shape and is unrelated to writing.
