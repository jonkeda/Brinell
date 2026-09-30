# Stride Platform Guide

Brinell supports Stride game/UI testing through `Brinell.Stride` and
`Brinell.Stride.Automation`.

## Projects

- `srcnew/Brinell.Stride`
- `srcnew/Brinell.Stride.Automation`
- `testsnew/Brinell.Stride.Automation.Tests`
- `testsnew/Brinell.Stride.UITests` (the UI tests and the UAT suite)
- `samples/Brinell.Samples.Stride.App`

## Rules

- Keep automation hooks explicit in the game/app project.
- Prefer named UI elements and state snapshots.
- Keep timing loops bounded and cancellation-aware.
- Store screenshots, logs, and traces under the shared artifact layout.
