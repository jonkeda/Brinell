# Backlog recovered from source TODOs

The five `TODO` markers in the source have been removed and recorded here. A marker in the code
is a note to someone who has already left; this file is read.

## 1. `TestName` is hard-coded to `"Test"`

- `srcnew/Brinell.Maui/Controls/Base/ViewBase.tpl.cs`
- `srcnew/Brinell.Html/Controls/ControlBase.cs`

Both read:

```csharp
private string TestName => "Test";
```

Used for logging and diagnostics, so every log line reports the same test name regardless of
which test is running — diagnostics that cannot distinguish one run from another. The value
should come from the test context, which knows it.

`PageName` next to it does resolve properly in `ViewBase` (`Page?.GetType().Name`) but is
hard-coded to `"Unknown"` in the HTML `ControlBase`, so that one has the same problem.

## 2. `Brinell.Html.ControlBase` predates the `RunDoWithElement` model

- `srcnew/Brinell.Html/Controls/ControlBase.cs`, three methods: `RunWithElement(Action<…>)`,
  `RunWithElement<TResult>(Func<…>)`, `RunAssert(…)`

These are the older single-step helpers. `Brinell.Maui` has since split resolution from action —
`RunDoWithElement` resolves a ready element by polling, then performs the action exactly once —
which is what gives the MAUI side its readiness behaviour and its guarantee that a retried
resolution never replays an action.

The HTML side has neither. Bringing it onto the same model would give it the same guarantees, and
is a precondition for sharing more between platforms.

Not urgent: Playwright's own auto-waiting covers much of what the MAUI ladder does by hand, so
the risk here is lower than the same gap would be on MAUI.
