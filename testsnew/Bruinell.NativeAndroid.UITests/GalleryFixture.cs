using Bruinell.Core.Composition;
using Bruinell.NativeAndroid.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Bruinell.NativeAndroid.UITests;

/// <summary>
/// One Appium session for the gallery app, shared by the UI tests and the UAT suite. The app is
/// installed fresh (its data and permissions cleared) when the session opens; each test opens its
/// screen by deep link.
/// </summary>
[TestModuleScan(typeof(LoginPage), NamespacePrefix = "Bruinell.NativeAndroid.UITests.Pages")]
public sealed class GalleryFixture : NativeAndroidTestFixtureBase, IAsyncLifetime
{
    /// <summary>The gallery app's package.</summary>
    public const string Package = "com.bruinell.samples.nativeandroid";

    private TestComposition? _composition;

    /// <summary>The pages and the context, for UAT scenarios.</summary>
    public TestComposition Composition
        => _composition ??= TestComposition.ForFixture(this, services => services.AddSingleton(Context));

    /// <inheritdoc />
    protected override NativeAndroidDriverOptions CreateOptions()
    {
        var options = base.CreateOptions();
        options.AppPath ??= GalleryApk();
        options.AppPackage ??= Package;
        options.AppActivity ??= $"{Package}.MainActivity";
        // The permission prompt is under test (DialogsTests): it must not be granted on install.
        options.AutoGrantPermissions = false;
        // The APK just built is the one under test: Appium skips a reinstall of the same version.
        options.AdditionalCapabilities["enforceAppInstall"] = true;
        options.Timeouts = new Bruinell.Core.Configuration.TimeoutSettings { DefaultWait = 10000, PageLoad = 15000, PollingInterval = 100 };
        return options;
    }

    /// <summary>Opens a gallery screen by its deep link (<c>bruinell-native://login</c>).</summary>
    public void Open(string screen) => Context.NavigateTo($"bruinell-native://{screen}");

    /// <summary>"Given I am on the Sign In page".</summary>
    public void NavigateToSignIn() => Open("login");

    /// <summary>"Given I am on the Controls page".</summary>
    public void NavigateToControls() => Open("controls");

    /// <summary>"Given I am on the Invoices page".</summary>
    public void NavigateToInvoices() => Open("invoices");

    /// <summary>"Given I am on the Dialogs page".</summary>
    public void NavigateToDialogs() => Open("dialogs");

    /// <summary>"Given I am on the Navigation page".</summary>
    public void NavigateToNavigation() => Open("navigation");

    Task IAsyncLifetime.InitializeAsync() => InitializeAsync();

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync();

    // The sample's Debug build, beside this repository's samples; APPIUM_APP_PATH overrides it.
    private static string GalleryApk()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var apk = Path.Combine(folder.FullName, "samples", "Bruinell.Samples.NativeAndroid.App", "bin", "Debug", "net10.0-android", $"{Package}-Signed.apk");
            if (File.Exists(apk))
            {
                return apk;
            }
        }

        throw new FileNotFoundException("The gallery APK is not built. Run: dotnet build samples\\Bruinell.Samples.NativeAndroid.App, or set APPIUM_APP_PATH.");
    }
}

/// <summary>The suite's one session: UI test classes and the UAT suite run one after another on it.</summary>
[CollectionDefinition(Name)]
public sealed class GalleryCollection : ICollectionFixture<GalleryFixture>
{
    public const string Name = "Gallery";
}
