# WinForms Platform Guide

Bruinet automates WinForms through FlaUI.

## Projects

- `srcnew/Bruinet.WinForms`
- `testsnew/Bruinet.WinForms.Tests`
- `testsnew/Bruinet.WinForms.UITests` (the UI tests and the UAT suite)
- `samples/Bruinet.Samples.WinForms.App`

## Rules

- Prefer UI Automation patterns before pointer actions.
- Use stable automation IDs.
- Keep application startup and shutdown in fixtures.
- Capture automation tree diagnostics for lookup failures.

## Run

See [WinForms Run Guide](../run/WinForms.md).
