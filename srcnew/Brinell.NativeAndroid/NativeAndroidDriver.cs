namespace Brinell.NativeAndroid;

/// <summary>
/// Brinell driver wrapper for native Android Appium sessions.
/// </summary>
public sealed class NativeAndroidDriver : IDriver<NativeAndroidElement>, IDiagnosticDriver
{
    private readonly AndroidDriver driver;
    private readonly NativeAndroidDriverOptions options;
    private bool disposed;

    public NativeAndroidDriver(AndroidDriver driver, NativeAndroidDriverOptions? options = null)
    {
        this.driver = driver ?? throw new ArgumentNullException(nameof(driver));
        this.options = options ?? new NativeAndroidDriverOptions();
    }

    public AndroidDriver RawDriver => driver;

    public string Context
    {
        get => driver.Context;
        set => driver.Context = value;
    }

    public IReadOnlyCollection<string> Contexts => driver.Contexts;

    public NativeAndroidEvidenceCapture Evidence => new(this);

    public static NativeAndroidDriver Create(NativeAndroidDriverOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var appiumOptions = BuildAppiumOptions(options);
        return new NativeAndroidDriver(new AndroidDriver(options.AppiumServerUri, appiumOptions), options);
    }

    public static NativeAndroidDriver CreateFromEnvironment()
        => Create(NativeAndroidDriverOptions.FromEnvironment());

    public NativeAndroidElement FindElement(Locator locator, int timeoutMs)
    {
        ArgumentNullException.ThrowIfNull(locator);
        var by = locator.ToAndroidBy();

        if (timeoutMs > 0)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(timeoutMs));
            try
            {
                var element = wait.Until(webDriver => webDriver.FindElement(by));
                return new NativeAndroidElement((AppiumElement)element, this);
            }
            catch (WebDriverTimeoutException)
            {
                throw new ElementNotFoundException(locator, timeoutMs);
            }
        }

        try
        {
            return new NativeAndroidElement((AppiumElement)driver.FindElement(by), this);
        }
        catch (NoSuchElementException)
        {
            throw new ElementNotFoundException(locator, timeoutMs);
        }
    }

    public IReadOnlyList<NativeAndroidElement> FindElements(Locator locator, int timeoutMs = 0)
    {
        ArgumentNullException.ThrowIfNull(locator);
        var by = locator.ToAndroidBy();

        if (timeoutMs > 0)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(timeoutMs));
            try
            {
                wait.Until(_ => driver.FindElements(by).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                return Array.Empty<NativeAndroidElement>();
            }
        }

        return driver.FindElements(by)
            .Select(element => new NativeAndroidElement(element, this))
            .ToList();
    }

    public bool TryFindElement(Locator locator, out NativeAndroidElement? element, int timeoutMs = 2000)
    {
        try
        {
            element = FindElement(locator, timeoutMs);
            return true;
        }
        catch (ElementNotFoundException)
        {
            element = null;
            return false;
        }
    }

    public IReadOnlyList<NativeAndroidElement> FindByAndroidUIAutomator(string uiAutomatorQuery, int timeoutMs = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uiAutomatorQuery);
        var by = MobileBy.AndroidUIAutomator(uiAutomatorQuery);

        if (timeoutMs > 0)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(timeoutMs));
            try
            {
                wait.Until(_ => driver.FindElements(by).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                return Array.Empty<NativeAndroidElement>();
            }
        }

        return driver.FindElements(by)
            .Select(element => new NativeAndroidElement(element, this))
            .ToList();
    }

    public bool TryFindByAndroidUIAutomator(
        string uiAutomatorQuery,
        out IReadOnlyList<NativeAndroidElement> elements,
        int timeoutMs = 0)
    {
        elements = FindByAndroidUIAutomator(uiAutomatorQuery, timeoutMs);
        return elements.Count > 0;
    }

    public void SwitchToNativeApp() => Context = "NATIVE_APP";

    public bool TrySwitchToWebContext(string? contains = null, int timeoutMs = 5000)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeoutMs);

        do
        {
            var webContext = Contexts.FirstOrDefault(context =>
                context.StartsWith("WEBVIEW", StringComparison.OrdinalIgnoreCase)
                && (contains is null || context.Contains(contains, StringComparison.OrdinalIgnoreCase)));

            if (webContext is not null)
            {
                Context = webContext;
                return true;
            }

            Thread.Sleep(100);
        }
        while (DateTimeOffset.UtcNow < deadline);

        return false;
    }

    public void LaunchDeepLink(string uri, string? appPackage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);

        var packageName = appPackage ?? options.AppPackage ?? GetCapability("appPackage");
        if (!string.IsNullOrWhiteSpace(packageName))
        {
            try
            {
                driver.ExecuteScript("mobile: deepLink", new Dictionary<string, object>
                {
                    ["url"] = uri,
                    ["package"] = packageName
                });
                return;
            }
            catch (WebDriverException)
            {
                // Fall back to adb shell. This keeps callback-scheme testing possible
                // when the Appium server does not support mobile: deepLink.
            }
        }

        driver.ExecuteScript("mobile: shell", new Dictionary<string, object>
        {
            ["command"] = "am",
            ["args"] = new[]
            {
                "start",
                "-W",
                "-a",
                "android.intent.action.VIEW",
                "-d",
                uri
            }
        });
    }

    public void ActivateApp(string? appPackage = null)
    {
        var packageName = appPackage ?? options.AppPackage ?? GetCapability("appPackage");
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new InvalidOperationException("Cannot activate app because no app package is configured.");
        }

        driver.ActivateApp(packageName);
    }

    public void TerminateApp(string? appPackage = null)
    {
        var packageName = appPackage ?? options.AppPackage ?? GetCapability("appPackage");
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new InvalidOperationException("Cannot terminate app because no app package is configured.");
        }

        driver.TerminateApp(packageName);
    }

    public void ResetAppState()
    {
        var packageName = options.AppPackage ?? GetCapability("appPackage");
        if (string.IsNullOrWhiteSpace(packageName))
        {
            return;
        }

        driver.TerminateApp(packageName);
        driver.ActivateApp(packageName);
    }

    public object? ExecuteScript(string script, params object[] args)
        => driver.ExecuteScript(script, args);

    public byte[] GetScreenshot() => driver.GetScreenshot().AsByteArray;

    public void SaveScreenshot(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllBytes(path, GetScreenshot());
    }

    public string GetPageSource() => driver.PageSource;

    public string GetAutomationTree() => driver.PageSource;

    public void SavePageSource(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, GetPageSource());
    }

    public void Close() => driver.Close();

    public void Quit() => driver.Quit();

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        driver.Quit();
        disposed = true;
    }

    internal string? GetCapability(string name)
        => driver.Capabilities.GetCapability(name)?.ToString();

    private static AppiumOptions BuildAppiumOptions(NativeAndroidDriverOptions options)
    {
        var appiumOptions = new AppiumOptions
        {
            PlatformName = "Android",
            AutomationName = options.AutomationName,
            DeviceName = options.DeviceName
        };

        if (!string.IsNullOrWhiteSpace(options.PlatformVersion))
        {
            appiumOptions.PlatformVersion = options.PlatformVersion;
        }

        if (!string.IsNullOrWhiteSpace(options.AppPath))
        {
            appiumOptions.App = options.AppPath;
        }

        AddOptionIfPresent(appiumOptions, "appPackage", options.AppPackage);
        AddOptionIfPresent(appiumOptions, "appActivity", options.AppActivity);
        appiumOptions.AddAdditionalAppiumOption("autoGrantPermissions", options.AutoGrantPermissions);
        appiumOptions.AddAdditionalAppiumOption("noReset", options.NoReset);
        appiumOptions.AddAdditionalAppiumOption("fullReset", options.FullReset);

        foreach (var capability in options.AdditionalCapabilities)
        {
            appiumOptions.AddAdditionalAppiumOption(capability.Key, capability.Value);
        }

        return appiumOptions;
    }

    private static void AddOptionIfPresent(AppiumOptions appiumOptions, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            appiumOptions.AddAdditionalAppiumOption(name, value);
        }
    }
}
