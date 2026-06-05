namespace Brinell.NativeAndroid;

public sealed class NativeAndroidTestContext :
    ITestContext<NativeAndroidElement>,
    INativeAndroidScope<NativeAndroidTestContext>
{
    private bool disposed;

    public NativeAndroidTestContext(NativeAndroidDriver driver, NativeAndroidDriverOptions? options = null)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        Timeouts = options?.Timeouts ?? TimeoutSettings.Default;
        Logger = options?.Logger ?? NullTestLogger.Instance;
    }

    public NativeAndroidDriver Driver { get; }

    public TimeoutSettings Timeouts { get; }

    public ITestLogger Logger { get; }

    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.Id;

    public IPageObject? Page => null;

    public NativeAndroidTestContext Context => this;

    public NativeAndroidTestContext Self => this;

    public bool IsReady(int? timeoutMs = null) => true;

    public bool WaitReady(int? timeoutMs = null) => true;

    public NativeAndroidElement? TryFindElement(Locator locator)
        => Driver.TryFindElement(locator, out var element, 0) ? element : null;

    public NativeAndroidElement FindElement(Locator locator)
        => Driver.FindElement(locator, Timeouts.ElementFind);

    public IReadOnlyList<NativeAndroidElement> FindElements(Locator locator)
        => Driver.FindElements(locator);

    public void NavigateTo(string destination)
    {
        Logger.LogNavigation(string.Empty, string.Empty, destination);
        Driver.LaunchDeepLink(destination);
    }

    public void NavigateBack() => Driver.RawDriver.Navigate().Back();

    public void Refresh() => Driver.RawDriver.Navigate().Refresh();

    public byte[] TakeScreenshot() => Driver.GetScreenshot();

    public void SaveScreenshot(string path) => Driver.SaveScreenshot(path);

    public void ResetAppState() => Driver.ResetAppState();

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Driver.Dispose();
        disposed = true;
    }
}
