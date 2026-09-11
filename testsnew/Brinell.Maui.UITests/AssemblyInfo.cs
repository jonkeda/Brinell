using Xunit;

// Step 16. Collections may run in parallel; whether they actually do is decided at runtime.
//
// This used to be `DisableTestParallelization = true`, and the comment beside it named the real
// constraint rather than a runner one: two apps on screen at once compete for the foreground,
// the pointer follows one of them, and keystrokes land wherever focus last went. That is a
// statement about the desktop, and stages 13 to 15 removed the suite's need for it - in
// background mode no test touches the foreground, the pointer or the clipboard, so two apps can
// run side by side without noticing each other.
//
// An attribute cannot express "unless background mode", because it is fixed at compile time and
// the policy is read from the environment. So the gate moved to where it can be asked at
// runtime: DesktopLease, taken for the life of each fixture. With physical input allowed it
// serialises the collections exactly as this attribute used to; with it refused or audited, it
// lets go.
//
// The mobile head keeps the old attribute, in its own copy of this file: two Appium sessions
// share one emulator whatever the input policy says, and that half of the original comment is
// still true.
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = 2)]
