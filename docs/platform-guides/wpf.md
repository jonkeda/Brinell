# WPF Platform Guide

Bruinet automates WPF through FlaUI.

## Projects

- `srcnew/Bruinet.Wpf`
- `testsnew/Bruinet.Wpf.Tests`
- `testsnew/Bruinet.Wpf.UITests` (the UI tests and the UAT suite)
- `samples/Bruinet.Samples.Wpf.App`

## Rules

- Prefer Invoke, Value, SelectionItem, RangeValue, and ExpandCollapse patterns.
- Keep raw mouse movement out of normal public test APIs.
- Use page objects for window/page structure.
- Capture screenshots and automation tree dumps for failures.

## Run

See [WPF Run Guide](../run/WPF.md).
