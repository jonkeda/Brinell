using Bruinet.Presenter.Services;
using Bruinet.Presenter.ViewModels;
using Bruinet.Presenter.Views;

#if WINDOWS
using Microsoft.UI.Windowing;
using Windows.Graphics;
using WinRT.Interop;
#endif

namespace Bruinet.Presenter;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var settingsService = new PresenterUserSettingsService();
        ApplyPersistedTheme(settingsService);
        ResolveIconFont();

        Window? window = null;
        var viewModel = new PresenterShellViewModel(
            new BddWorkspaceService(),
            new BddExecutionService(),
            new FolderPickerService(),
            settingsService,
            new ScenarioPreviewWindowService(),
            windowService: new PresenterWindowService(() => window));
        window = new Window(new PresenterPage(viewModel))
        {
            Title = "Bruinet Presenter"
        };
        // A narrow bar on its side of the screen; the app under test is placed beside it.
        window.Created += (_, _) => viewModel.ApplyDock();
        window.Destroying += (_, _) => viewModel.Shutdown();
        window.Activated += (_, _) => ShowTitleBarIcon(window);
        return window;
    }

    /// <summary>
    /// Applied before the page is built so the window does not flash light on its way to
    /// dark. The shell view model applies the same value again, idempotently.
    /// </summary>
    private void ApplyPersistedTheme(IPresenterUserSettingsService settingsService)
    {
        UserAppTheme = settingsService.Load().Theme switch
        {
            PresenterTheme.Light => AppTheme.Light,
            PresenterTheme.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    /// <summary>
    /// Falls the icon family back to Segoe MDL2 Assets where Segoe Fluent Icons is absent.
    /// </summary>
    private void ResolveIconFont()
    {
        var fluent = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
            "SegoeIcons.ttf");

        if (!File.Exists(fluent))
        {
            Resources["PresenterIconFont"] = "Segoe MDL2 Assets";
        }
    }

    /// <summary>
    /// Asks the title bar to draw the window icon.
    /// </summary>
    private static void ShowTitleBarIcon(Window window)
    {
#if WINDOWS
        if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window platformWindow)
        {
            return;
        }

        var handle = WindowNative.GetWindowHandle(platformWindow);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        if (appWindow.TitleBar.IconShowOptions != IconShowOptions.ShowIconAndSystemMenu)
        {
            appWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
        }
#endif
    }
}
