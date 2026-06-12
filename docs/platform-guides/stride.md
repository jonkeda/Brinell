# Stride Platform Guide

Brinell supports Stride game/UI testing through `Brinell.Stride` and
`Brinell.Automation`.

## Projects

- `srcnew/Brinell.Stride`
- `srcnew/Brinell.Automation`
- `testsnew/Brinell.Stride.Tests`
- `testsnew/Brinell.Stride.UITests`
- `testsnew/Brinell.Stride.Uat.Tests`
- `samples/Brinell.Samples.Stride.App`

## Rules

- Keep automation hooks explicit in the game/app project.
- Prefer named UI elements and state snapshots.
- Keep timing loops bounded and cancellation-aware.
- Store screenshots, logs, and traces under the shared artifact layout.
