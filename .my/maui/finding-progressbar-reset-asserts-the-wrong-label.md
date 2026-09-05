# Finding: `ProgressBar_Reset_ReturnsToInitialState` asserts the wrong label

The one failure in the Windows tier (76 / 77) is not a scrolling, timing or platform problem. It
is a deterministic test defect, and the app is behaving correctly.

## The failure

```
Brinell.Core.Exceptions.AssertionException :
  Expected TextContains to be '50%'. Locator: AutomationId:StatusLabel
  at ProgressBarTests.ProgressBar_Reset_ReturnsToInitialState() ProgressBarTests.cs:line 119
```

[ProgressBarTests.cs:113-124](testsnew/Brinell.Maui.UITests/Tests/Display/ProgressBarTests.cs#L113-L124):

```csharp
page.IncreaseProgressButton.Click()
    .StatusLabel.AssertTextContains("60%")
    .ResetButton.Click()
    .StatusLabel.AssertTextContains("50%");   // <- can never pass
```

## Why it can never pass

`StatusLabel` is bound to `StatusMessage`, and the two commands write it differently
([DisplayViewModel.cs](samples/Brinell.Samples.Maui.App/ViewModels/DisplayViewModel.cs)):

| Command | `StatusMessage` becomes |
|---|---|
| Increase / Decrease | `"✓ Progress: 60% \| Activity: …"` — contains the percentage |
| Reset (line 127) | `"Ready. Interact with controls to test."` — contains no percentage |

So the first assertion passes for the same reason the second one cannot. `Reset()` *does* restore
`ProgressValue = 50`; it just does not say so in the status message, and nothing is wrong with
that.

## Why the test reached for the wrong label

The value it wants is on screen the whole time, in the label above the bar:

```xml
<Label Text="{Binding FormattedProgress, StringFormat='Current Progress: {0}'}" ... />
```

[DisplayView.xaml:96-99](samples/Brinell.Samples.Maui.App/Views/DisplayView.xaml#L96-L99) — **that
label has no `AutomationId`**, so no page object can reach it, and the test settled for the shared
`StatusLabel` instead. A control nobody can name is a control automation has to work around.

The passing Reset tests confirm the convention. `ToggleViewModel.Reset()` writes the same kind of
generic message (`"Ready. Toggle controls to test."`), and `CheckBox_Reset_ClearsState` does not
assert against `StatusLabel` at all — it asserts `CheckBoxStatusLabel`, the label bound to that
control's own state:

```csharp
page.CheckBoxStatusLabel.AssertTextContains("is checked")
    .ResetButton.Click()
    .CheckBoxStatusLabel.AssertTextContains("is unchecked");
```

ProgressBar has no such per-control label it can address, so it is the one that breaks.

## Fix

Two lines, in two places:

1. `AutomationId="ProgressValueLabel"` on the `Current Progress:` label in `DisplayView.xaml`, and
   the matching property on `DisplayTestPage`.
2. Point the assertion at it:
   `.ResetButton.Click().ProgressValueLabel.AssertTextContains("50%")`.

Do **not** "fix" it by making `Reset()` write the percentage into `StatusMessage`. That would
change the app to suit one test, break the symmetry with `ToggleViewModel.Reset()`, and leave the
real gap — an unaddressable label — in place.

## Scope

Deterministic and platform-independent: it fails the same way wherever the Display tier runs. It
is recorded as "the Windows failure" only because Windows is the tier that has been running
Display — an Android Display run would fail it too.

Long treated as a known-pre-existing failure (see the "How to verify" baselines in
`../fixes/cleanup-scroll-and-find-architecture.md`), which is why the tier is quoted as 76 / 77
rather than 77 / 77. Once fixed, that baseline becomes 77 / 77 and should be updated wherever it
is quoted.
