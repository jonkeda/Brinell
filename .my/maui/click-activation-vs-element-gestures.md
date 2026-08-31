# Click Activation vs Element Gestures

**Date:** 2026-08-30  
**Scope:** `ClickableControlBase<TScope>` in `srcnew/Brinell.Maui`

## Short answer

`ClickCore` uses `TryActivateByPattern` because `Click` is a semantic command: activate this control. On Windows, UI Automation patterns can invoke that command without relying on pointer coordinates or a particular visual child. If the element exposes no suitable pattern, as on the Appium mobile path, `ClickCore` falls back to `element.Click()`.

The other operations describe a specific input gesture rather than generic activation. A right-click must be a context click, hover must move a pointer, and long-press must preserve a duration. `Invoke` and `SelectionItem` cannot express those details, so these methods delegate to the matching `IMauiElement` primitive.

## Two abstraction levels

There are two related but different decisions:

1. The control layer decides what the operation means for the MAUI control.
2. The element adapter decides how to perform the selected primitive on its driver.

`ClickableControlBase.ClickCore` owns the first decision. Its activation ladder is:

1. `ISelectionItemPatternElement.SelectItemPattern()` when supported.
2. `IInvokePatternElement.InvokePattern()` when supported.
3. `IMauiElement.Click()` when neither pattern completes the activation.

That ordering is control policy. It is intentionally `protected virtual`, allowing a compound control to activate the child that actually owns the command. `IconCommandButton` and `RoundButton` do this by resolving their native button child before applying the same ladder. `ToggleControlBase` also reuses the ladder, then verifies that checked state changed.

The final `element.Click()` is driver behavior. It is a WebDriver click in `AppiumMauiElement` and a direct FlaUI element click in `FlaUIMauiElement`. Pattern ordering and control-specific activation policy remain exclusively in the control layer.

## Why pattern activation is needed for `Click`

On Windows, a synthetic click can be unreliable when a MAUI control is templated:

- the addressable automation element may be a container rather than the native command-bearing child;
- an overlay or visual child may receive the pointer;
- coordinates and bounds can be valid while the application command is still not dispatched.

UIA SelectionItem and Invoke address the control semantically, so they are preferred when advertised. This also avoids making every control know which platform it is running on. Capability interfaces express the difference: Windows elements expose UIA capabilities, while Appium elements on Android and iOS do not and naturally fall through to `element.Click()`.

The ladder must not catch a supported pattern's exception. An advertised pattern that throws is a real automation failure. The old `ElementClicker.TryClick` swallowed such failures, causing the test to continue and fail later with an unrelated assertion. `TryActivateByPattern` returns `false` only when no pattern succeeds; actual failures remain visible at the operation that caused them.

LegacyIAccessible is excluded deliberately. WinUI Switch can advertise `DoDefaultAction`, report success, and leave its state unchanged. Treating that as successful activation made `Click` silently do nothing. A specialized control may opt into that capability by overriding the ladder when its view genuinely requires it.

## Why the other methods do not use the ladder

| Method | Meaning | Why UIA activation is not substituted |
| --- | --- | --- |
| `RightClickCore` | Open a context action with the secondary button | Invoke has no mouse-button identity. |
| `HoverCore` | Move and hold the pointer over the element | Invoke activates; it does not establish hover state. |
| `LongPressCore` | Hold input for a requested duration | Invoke has no duration or press lifecycle. |
| `PressCore` | Activate through keyboard Space | The keyboard route is the behavior being requested; using Invoke would no longer test it. |

These methods still call `EnsureClickableCore` first, but execution belongs to the platform adapter because pointer and touch mechanics differ. FlaUI uses its pointer driver; Appium uses WebDriver actions or platform mobile gesture commands.

## The `DoubleClickCore` exception

`DoubleClickCore` currently calls `element.Click()` twice. That is not equivalent on every adapter to `element.DoubleClick()`:

```csharp
EnsureClickableCore(element);
element.Click();
element.Click();
```

On FlaUI, each call may take an Invoke or SelectionItem path, producing two semantic activations. On Appium, it produces two ordinary WebDriver clicks. Neither path necessarily preserves the timing and input semantics of the dedicated `IMauiElement.DoubleClick()` primitive, even though that primitive exists and is used by Brinell's HTML, WPF, WinForms, and Native Android control layers.

This behavior predates the activation ladder and should be treated as a separate design decision or defect, not as a reason to apply `TryActivateByPattern` to all gestures. If `DoubleClick` is intended to mean a native double-click gesture, `DoubleClickCore` should delegate to `element.DoubleClick()` and receive focused tests. If it is intended to mean two semantic activations, the public method and documentation should say so explicitly.

## Practical rule

Use `TryActivateByPattern` for an operation whose contract is semantic activation. Use the matching `IMauiElement` method when the contract names a concrete gesture or input modality. Override the control-level method when a compound MAUI view requires a different target or activation policy; keep platform mechanics in the element adapter.

## Relevant sources

- `srcnew/Brinell.Maui/Controls/Base/ClickableControlBase.tpl.cs`
- `srcnew/Brinell.Maui/Controls/Base/ToggleControlBase.tpl.cs`
- `srcnew/Brinell.Maui.Extensions/Controls/Buttons/IconCommandButton.cs`
- `srcnew/Brinell.Maui.Extensions/Controls/Buttons/RoundButton.cs`
- `srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs`
- `srcnew/Brinell.Maui.Appium/AppiumMauiElement.cs`
- `srcnew/Brinell.Core/Interfaces/IElement.cs`
- `testsnew/Brinell.Maui.Tests/Semantic/ClickLadderTests.cs`
