using Brinell.Core.Configuration;
using Brinell.Maui.Configuration;
using Brinell.Maui.Enums;
using Brinell.Samples.Todo.Infrastructure;
using Brinell.Samples.Todo.UITests.Containers;
using Brinell.Samples.Todo.UITests.Pages;

namespace Brinell.Samples.Todo.UITests;

/// <summary>
/// What launching the Todo app takes, the same for a one-app fixture (<see cref="TodoAppFixtureBase"/>)
/// and for each app of several (<see cref="TodoMauiDevice"/>): where the build is, the launch
/// values per platform, the Android capabilities, and the way back to the list.
/// </summary>
public static class TodoMauiLaunch
{
    /// <summary>The Todo app's Android package name (<c>ApplicationId</c>).</summary>
    public const string AndroidPackage = "com.brinell.samples.todo";

    /// <summary>The timeouts every Todo UI test uses, as the Brinell MAUI fixture sets them.</summary>
    public static TimeoutSettings Timeouts => new()
    {
        DefaultWait = 5000,
        PageLoad = 10000,
        ElementFind = 3000,
        ElementState = 3000,
        Animation = 300,
        PollingInterval = 100,
    };

    /// <summary>
    /// The settings as the app on <paramref name="platform"/> receives them (<c>MauiDriverOptions.LaunchSettings</c>:
    /// the launched process's environment on Windows, launch-intent extras on Android).
    /// </summary>
    /// <remarks>
    /// On Android the device cannot open files on this machine, so the database path, the
    /// network-state file and the crash log are left out: the app uses its own database, which
    /// the install at session start leaves empty. The test set's local half therefore does not
    /// reach an Android app; its server half does, through the first sync. The backend URL is
    /// rewritten to the emulator's address for this machine (<c>AndroidHost</c>).
    /// </remarks>
    /// <param name="settings">The app's settings.</param>
    /// <param name="runFolder">A folder of the run's own, for the crash log.</param>
    /// <param name="platform">Where the app runs.</param>
    public static Dictionary<string, string> LaunchValues(LaunchSettings settings, string runFolder, MauiPlatform platform)
    {
        var values = new Dictionary<string, string>(settings.ToVariables());

        if (platform == MauiPlatform.Android)
        {
            values.Remove(LaunchSettings.DatabasePathVariable);
            values.Remove(LaunchSettings.NetworkStateFileVariable);
            values[LaunchSettings.ApiBaseUrlVariable] = Brinell.Maui.Appium.AndroidHost.EmulatorUrl(settings.ApiBaseUrl);
        }
        else
        {
            values["BRINELL_APP_CRASH_LOG"] = Path.Combine(runFolder, "crash.log");
        }

        return values;
    }

    /// <summary>The capabilities the Brinell sample fixture uses, for this app's package.</summary>
    /// <param name="options">The driver options of an Android launch.</param>
    public static void ConfigureAndroid(MauiDriverOptions options)
    {
        options.AdditionalCapabilities["autoGrantPermissions"] = true;
        options.AdditionalCapabilities["newCommandTimeout"] = 300;
        options.AdditionalCapabilities["appWaitDuration"] = 60000;
        options.AdditionalCapabilities["adbExecTimeout"] = 60000;

        // MAUI hashes the activity name, so wait on the package and accept whatever it launches.
        options.AdditionalCapabilities["appWaitPackage"] = AndroidPackage;

        // A MAUI app that animates may never report idle; don't pay 10 s per command for it.
        options.AdditionalCapabilities["settings[waitForIdleTimeout]"] = 100;

        // MAUI does not bump versionCode between builds: reinstall, or a run tests the old build.
        // The reinstall also clears the app's data, which is what gives each session an empty database.
        options.AdditionalCapabilities["enforceAppInstall"] = true;
    }

    /// <summary>
    /// The app built by the Todo solution, Debug preferred: the Windows exe, or the signed Android
    /// APK. Found from the test output by walking up to the folder holding
    /// <c>Brinell.Samples.Todo.slnx</c>.
    /// </summary>
    /// <param name="platform">Where the app runs.</param>
    public static string AppPath(MauiPlatform platform)
    {
        var (fileName, framework, buildCommand) = platform switch
        {
            MauiPlatform.Windows => ("Brinell.Samples.Todo.App.exe", "net10.0-windows", "-f net10.0-windows10.0.19041.0"),
            MauiPlatform.Android => ($"{AndroidPackage}-Signed.apk", "net10.0-android", "-f net10.0-android"),
            _ => throw new NotSupportedException($"The Todo UI tests run on Windows and Android, not {platform}."),
        };

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Brinell.Samples.Todo.slnx")))
        {
            root = root.Parent;
        }

        if (root is null)
        {
            throw new DirectoryNotFoundException(
                $"No folder above '{AppContext.BaseDirectory}' holds Brinell.Samples.Todo.slnx.");
        }

        var bin = Path.Combine(root.FullName, "src", "Brinell.Samples.Todo.App", "bin");
        foreach (var configuration in new[] { "Debug", "Release" })
        {
            var folder = Path.Combine(bin, configuration);
            if (!Directory.Exists(folder))
            {
                continue;
            }

            var app = Directory
                .EnumerateFiles(folder, fileName, SearchOption.AllDirectories)
                .FirstOrDefault(path => path.Contains(framework, StringComparison.OrdinalIgnoreCase));

            if (app is not null)
            {
                return app;
            }
        }

        throw new FileNotFoundException(
            $"The Todo app is not built for {platform}. From samples\\Todo: dotnet build "
            + $"src\\Brinell.Samples.Todo.App\\Brinell.Samples.Todo.App.csproj {buildCommand}",
            bin);
    }

    /// <summary>The list, once it has arrived and stopped loading.</summary>
    /// <param name="context">The app.</param>
    public static TodoListPage WaitForTheList(IMauiTestContext context)
    {
        var list = TodoListPage.Arrived(context);
        list.State.WaitShowing(TodoListState.Loading, false, TestConstants.PageTimeoutMs);
        return list;
    }

    /// <summary>
    /// Leaves whatever page or dialog an earlier test ended on, through the pages' own controls.
    /// </summary>
    /// <remarks>
    /// A dialog is answered with its leaving button (Discard for unsaved changes, Cancel for a
    /// delete), an edit page is cancelled, a detail page is left with the back arrow. Four rounds
    /// cover the deepest case: a dirty edit page over a detail page, with its dialog open.
    /// </remarks>
    /// <param name="context">The app.</param>
    public static TodoListPage ReturnToList(IMauiTestContext context)
    {
        var appRoot = new AppRoot(context);

        for (var round = 0; round < 4; round++)
        {
            var dialog = new ContentDialog<AppRoot>(appRoot);
            var list = new TodoListPage(context);

            if (dialog.IsExists())
            {
                var buttons = dialog.GetButtonTexts() ?? [];
                dialog.DialogButton(buttons.Contains("Discard") ? "Discard" : "Cancel").Click();
            }
            else if (list.IsLoaded())
            {
                return list;
            }
            else if (new TodoEditPage(context).IsLoaded())
            {
                new TodoEditPage(context).CancelButton.Click();
            }
            else if (new TodoDetailPage(context).IsLoaded())
            {
                new TodoDetailPage(context).BackButton.Click();
            }

            list.WaitLoaded(true, TestConstants.PageTimeoutMs / 4);
        }

        return TodoListPage.Arrived(context);
    }
}
