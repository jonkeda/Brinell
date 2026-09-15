using Xunit;

// Step 16. Collections run in parallel.
//
// This used to be `DisableTestParallelization = true`, and the comment beside it named the real
// constraint rather than a runner one: two apps on screen at once compete for the foreground,
// the pointer follows one of them, and keystrokes land wherever focus last went. That is a
// statement about the desktop, and the MAUI FlaUI driver no longer touches the foreground, the
// pointer, the keyboard or the clipboard at all - so two apps run side by side without noticing
// each other, unconditionally. (For a while this was decided at runtime by a desktop lease that
// serialised collections when physical input was allowed; there is no such mode any more.)
//
// The mobile head keeps the old attribute, in its own copy of this file: two Appium sessions
// share one emulator whatever the driver does, and that half of the original comment is still
// true.
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = 2)]
