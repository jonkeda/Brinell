# Stride Platform Guide

Bruinell supports Stride game/UI testing through `Bruinell.Stride` and
`Bruinell.Stride.Automation`.

## Projects

- `srcnew/Bruinell.Stride`
- `srcnew/Bruinell.Stride.Automation`
- `testsnew/Bruinell.Stride.Automation.Tests`
- `testsnew/Bruinell.Stride.UITests` (the UI tests and the UAT suite)
- `samples/Bruinell.Samples.Stride.App`

## Rules

- Keep automation hooks explicit in the game/app project.
- Prefer named UI elements and state snapshots.
- Keep timing loops bounded and cancellation-aware.
- Store screenshots, logs, and traces under the shared artifact layout.
