# Stride Platform Guide

Bruinet supports Stride game/UI testing through `Bruinet.Stride` and
`Bruinet.Stride.Automation`.

## Projects

- `srcnew/Bruinet.Stride`
- `srcnew/Bruinet.Stride.Automation`
- `testsnew/Bruinet.Stride.Automation.Tests`
- `testsnew/Bruinet.Stride.UITests` (the UI tests and the UAT suite)
- `samples/Bruinet.Samples.Stride.App`

## Rules

- Keep automation hooks explicit in the game/app project.
- Prefer named UI elements and state snapshots.
- Keep timing loops bounded and cancellation-aware.
- Store screenshots, logs, and traces under the shared artifact layout.
