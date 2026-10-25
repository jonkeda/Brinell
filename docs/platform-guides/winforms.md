# WinForms Platform Guide

Bruinell automates WinForms through FlaUI.

## Projects

- `srcnew/Bruinell.WinForms`
- `testsnew/Bruinell.WinForms.Tests`
- `testsnew/Bruinell.WinForms.UITests` (the UI tests and the UAT suite)
- `samples/Bruinell.Samples.WinForms.App`

## Rules

- Prefer UI Automation patterns before pointer actions.
- Use stable automation IDs.
- Keep application startup and shutdown in fixtures.
- Capture automation tree diagnostics for lookup failures.

## Run

See [WinForms Run Guide](../run/WinForms.md).
