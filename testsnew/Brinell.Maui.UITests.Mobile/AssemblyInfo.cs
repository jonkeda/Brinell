using Xunit;

// The mobile head's own copy, and the reason the shared one is excluded from this project.
//
// The Windows head lets collections run in parallel and decides at runtime whether they really
// should, because there the constraint is the desktop and background mode removes it. Here the
// constraint is the device: two Appium sessions share one emulator, and no input policy changes
// that. So this stays as it always was.
//
// Kept as a separate file rather than a #if in the shared one. The two heads genuinely disagree
// about this, and a conditional would make it look like one decision with an exception rather
// than two projects with different hardware underneath them.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
