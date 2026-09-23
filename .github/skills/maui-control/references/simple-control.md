# Simple control

One element to the test. Parts the platform draws separately are implementation detail.

## Shape

```csharp
namespace Brinell.Maui.Controls.Toggle;

/// <summary>MAUI Switch: an on/off toggle.</summary>
/// <remarks>Windows: TogglePattern. Android: checkable; touch toggles it.</remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Switch<TScope> : Base.ToggleControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    public Switch(IMauiScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public Switch(IMauiScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>Whether the switch is on.</summary>
    protected virtual bool? IsOnCore(IMauiElement? element) => IsCheckedCore(element);

    #endregion
}
```

Every generated member returns `TScope`, so a chain returns to the page:
`page.TestSwitch.TurnOn().StatusLabel.AssertText("On")`.

## Pick the base by capability, lowest that fits

All in `srcnew/Brinell.Maui/Controls/Base/`. Each base already implements its interface.

| Base | Adds | Interface |
| --- | --- | --- |
| `ViewBase` | exists, visible, enabled, attribute, scroll into view | `IElementObject` |
| `FocusableControlBase` | focus | `IFocusableControlObject` |
| `ClickableControlBase` | click, double, right, hover, long press; waits for enabled | `IClickableControlObject` |
| `ToggleControlBase` | toggle, check, set checked, verified | `IToggleControlObject` |
| `RangeControlBase` | value, min, max, step, increment | `IRangeControlObject` |
| `SelectorControlBase` | text options, select | `ISelectorControlObject` |

Read the base's `.tpl.cs` before writing: know which members you inherit.

## Rules

- **Override a base Core method when the platform activates differently** (Toggle's `ClickCore`
  toggles; RadioButton selects). Keep the generated name and its meaning. An override emits
  nothing new: the base already generated the public member.
- **Domain vocabulary is a thin layer.** `IsOnCore => IsCheckedCore`, a `SetOnCore` that calls
  the toggle behaviour, and hand-written aliases `TurnOn() => SetOn(true, timeoutMs)`. Never
  duplicate behaviour under a second name.
- **Parts drawn separately are resolved privately.** Stepper on Windows publishes `{id}Minus`
  and `{id}Plus`, not the Stepper. Override `TryFindElement()` and `FindElement()` so one part
  acts as the proxy element; `FindElement` throws `ElementNotFoundException` naming every
  locator it tried. Parts stay private: if a test needs them by name, it is a component.
- **State the tree cannot show** comes from the bridge:
  `Context.AppElement.TryFindDeclared(id)?.ReadState(property)`, parsed with
  `CultureInfo.InvariantCulture`. A malformed value throws; a missing declaration gives null.
- **Readiness to wait for** (enabled a moment after appearing) goes in
  `EnsureReadyForActionCore`, which the base calls before an action.
- **Absence**: an existence-like read that must answer false for a missing element carries
  `[AbsenceTolerant]` and takes `IMauiElement?`.

## Confirming an effect

```csharp
protected virtual void SetPlayingCore(IMauiElement element, bool? playing, int? timeoutMs = null)
{
    if (playing == null || IsPlayingCore(element) == playing)
    {
        return;                                    // null skips; already there is a no-op
    }

    ClickCore(element, timeoutMs);                 // own Core method, own element

    var confirmation = Confirm(() => IsPlayingCore(element), actual => actual == playing, timeoutMs);
    if (!confirmation.IsConfirmed)                 // never repeats the press
    {
        throw confirmation.Failure(Locator, "the press", lastError => new TimeoutException(
            $"'{Locator.Value}' was pressed and did not {(playing.Value ? "start playing" : "pause")}.",
            lastError));                           // a replaced element: StaleElementException
    }
}
```

## Examples to read

`srcnew/Brinell.Maui/Controls/Toggle/Switch.tpl.cs` (aliases), `srcnew/Brinell.Maui/Controls/Range/Stepper.tpl.cs` (private parts, bridge state),
`srcnew/Brinell.Maui.CommunityToolkit/Controls/Views/Expander.tpl.cs` (bridge verb, confirmed effect),
`srcnew/Brinell.Maui.CommunityToolkit/Controls/Media/MediaPlayPauseButton.tpl.cs` (derived from `Button`, verified
setter).
