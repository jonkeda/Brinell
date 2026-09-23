# MediaElement Component Plan

Status: implemented 2026-09-18.

## Goal

Model `MediaElement` as a composite component whose WinUI transport parts are typed Brinell ControlObjects rather than raw `IMauiElement` lookups.

## Changes

1. Change `MediaElement` to derive from `ComponentObjectBase<TScope, MediaElement<TScope>>`, preserving its constructors and fluent API.
2. Declare named child ControlObject properties for the published transport buttons, including play/pause, volume/mute, repeat, rewind, and fast-forward. Use the component as their scope so callers can inspect or operate them semantically; MediaElement alone delegates child lookup to its parent because WinUI flattens the transport tree.
3. Add typed child properties for the progress slider and elapsed/remaining labels where current media behavior reads those parts.
4. Rewrite `Play`, `Pause`, `IsPlaying`, progress, and duration behavior through the named child controls; remove the raw `Part` helper where no longer needed. Preserve the fallback that treats the play/pause button as evidence that the flattened Windows media component exists.
5. Update MediaElement UI tests to cover the exposed child controls and existing playback behavior. Regenerate the partial source, build the affected projects, rebuild the sample app, and run only `MediaElementTests` before widening validation.

## Constraints

- Keep transport interaction tree-based; add no bridge verbs, coordinates, physical input, or sleeps.
- Preserve the documented one-media-element-per-scope Windows limitation caused by fixed transport AutomationIds.
- Keep unsupported or absent Android transport parts explicit rather than fabricating platform behavior.
