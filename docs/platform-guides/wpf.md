# WPF Platform Guide

Brinell automates WPF through FlaUI.

## Projects

- `srcnew/Brinell.Wpf`
- `testsnew/Brinell.Wpf.Tests`
- `testsnew/Brinell.Wpf.UITests` (the UI tests and the UAT suite)
- `samples/Brinell.Samples.Wpf.App`

## Rules

- Prefer Invoke, Value, SelectionItem, RangeValue, and ExpandCollapse patterns.
- Keep raw mouse movement out of normal public test APIs.
- Use page objects for window/page structure.
- Capture screenshots and automation tree dumps for failures.

## Run

See [WPF Run Guide](../run/WPF.md).
