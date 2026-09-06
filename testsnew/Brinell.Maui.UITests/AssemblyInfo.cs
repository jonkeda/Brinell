using Xunit;

// Two fixtures now launch two different apps. xUnit runs collections in parallel by default,
// which would put both on screen at once: on Windows they compete for the foreground, and on
// Android two Appium sessions share one emulator. UI tests drive one machine, so they run one
// at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
