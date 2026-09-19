namespace Brinell.Samples.Todo.UITests.Tests;

/// <summary>
/// A test the Android head cannot arrange yet: skipped there, with the reason, instead of failing.
/// </summary>
/// <remarks>
/// <para>
/// Not a way to hide a failure. Each use names the missing arrangement or the known driver gap, and
/// the list is kept in <c>.my/TodoApp/plan.md</c> (phase 7). Today the reasons are:
/// </para>
/// <list type="bullet">
/// <item><see cref="LocalState"/> - the scenario's local half lives in a database file on this
/// machine, which the device cannot open; seeding it on Android means pushing the file into the app's
/// private storage (plan section 6, option A).</item>
/// <item><see cref="NetworkSwitch"/> - the network-state file is on this machine too.</item>
/// <item><see cref="Reinstall"/> - every Appium session reinstalls the app (so each run starts
/// clean), which wipes its data: a restart there is a fresh install.</item>
/// <item><see cref="PickerGap"/> - a MAUI Picker opens a native dialog on Android that the selection
/// controls cannot reach (the Brinell Picker tests are 0/8 there before any change).</item>
/// <item><see cref="ShellTitle"/> - Shell's title on Android has no id or content description, only its
/// position among id-less layouts.</item>
/// </list>
/// <para>
/// The head is read from <c>APPIUM_PLATFORM</c>, the variable that selects it for Brinell.
/// </para>
/// </remarks>
public sealed class WindowsOnlyFactAttribute : FactAttribute
{
    /// <summary>Needs the scenario's local half in the app's database.</summary>
    public const string LocalState = "Needs the scenario's local state seeded into the app's database, which is not possible on a device yet.";

    /// <summary>Needs the network-state file.</summary>
    public const string NetworkSwitch = "Needs the network-state file, which the device cannot read.";

    /// <summary>Needs the app's data to survive a relaunch.</summary>
    public const string Reinstall = "A new Appium session reinstalls the app and clears its data, so a restart is a fresh install.";

    /// <summary>Uses the Picker.</summary>
    public const string PickerGap = "Uses the Picker, whose native dialog the selection controls cannot reach on Android yet.";

    /// <summary>Reads the page title Shell draws.</summary>
    public const string ShellTitle = "Reads the page title, which Shell draws on Android as an id-less TextView in the toolbar that no stable locator can reach.";

    /// <summary>A fact that runs on Windows only.</summary>
    /// <param name="reason">Why it cannot run on a mobile head: one of the constants above.</param>
    public WindowsOnlyFactAttribute(string reason)
    {
        Timeout = TestConstants.DefaultTestTimeoutMs;
        if (IsMobileHead)
        {
            Skip = "Windows only: " + reason;
        }
    }

    /// <summary>Whether this run drives a mobile head.</summary>
    public static bool IsMobileHead =>
        Environment.GetEnvironmentVariable("APPIUM_PLATFORM") is { Length: > 0 } platform
        && !string.Equals(platform, "windows", StringComparison.OrdinalIgnoreCase);
}

/// <summary>A theory that runs on Windows only; see <see cref="WindowsOnlyFactAttribute"/>.</summary>
public sealed class WindowsOnlyTheoryAttribute : TheoryAttribute
{
    /// <summary>A theory that runs on Windows only.</summary>
    /// <param name="reason">Why it cannot run on a mobile head.</param>
    public WindowsOnlyTheoryAttribute(string reason)
    {
        Timeout = TestConstants.DefaultTestTimeoutMs;
        if (WindowsOnlyFactAttribute.IsMobileHead)
        {
            Skip = "Windows only: " + reason;
        }
    }
}
