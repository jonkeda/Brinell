// One app at a time.
//
// This used to be forced: every fixture handed its app a different database and backend through
// the test process's environment, which two fixtures launching at once would have mixed up. The
// settings now travel with each launch (MauiDriverOptions.LaunchSettings), so that reason is gone
// on Windows. Collections still run one after another: the whole suite takes about 30 s, and the
// Android head shares one emulator, where two Appium sessions cannot run side by side. Turning
// parallelism on for Windows is a separate, measured change.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
