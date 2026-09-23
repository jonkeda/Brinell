using Brinell.Presenter.Services;
using Brinell.Presenter.ViewModels;
using Brinell.Presenter.Views;

#if WINDOWS
using Microsoft.UI.Windowing;
using Windows.Graphics;
using WinRT.Interop;
#endif

namespace Brinell.Presenter;

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

        var viewModel = new PresenterShellViewModel(
            new UatWorkspaceService(),
            new UatExecutionService(),
            new FolderPickerService(),
            settingsService);
        var window = new Window(new PresenterPage(viewModel))
        {
            Title = "Brinell Presenter"
        };
        window.Created += (_, _) => PlacePresenterWindow(window);
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
    /// <remarks>
    /// Fluent ships with Windows 11; Windows 10 - which this app still supports, see
    /// <c>SupportedOSPlatformVersion</c> - has only MDL2. Asking for a font that is not installed
    /// does not fall back on WinUI, it draws blank boxes. Every codepoint in
    /// <c>PresenterIcons</c> was verified present in both families, so swapping the family name
    /// is the whole fix. Checked by file because there is no MAUI API that answers "is this font
    /// installed" on Windows.
    /// </remarks>
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

    private static void PlacePresenterWindow(Window window)
    {
#if WINDOWS
        if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window platformWindow)
        {
            return;
        }

        var handle = WindowNative.GetWindowHandle(platformWindow);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var width = workArea.Width / 4;

        appWindow.MoveAndResize(new RectInt32(workArea.X, workArea.Y, width, workArea.Height));
#endif
    }

    /// <summary>
    /// Asks the title bar to draw the window icon.
    /// </summary>
    /// <remarks>
    /// <c>&lt;MauiIcon&gt;</c> alone gets the icon onto the .exe, the taskbar and Alt-Tab, but not
    /// into the title bar: WinUI 3 defaults <c>AppWindowTitleBar</c> to
    /// <c>HideIconAndSystemMenu</c>, so the window draws its title with a blank space where the
    /// icon belongs. Of the four surfaces this is the only one that has to be asked for.
    /// <para>
    /// On <c>Activated</c> rather than <c>Created</c>: at <c>Created</c> the title bar is not yet
    /// realised and the assignment is silently dropped. Activated fires repeatedly, so this is
    /// written to be idempotent.
    /// </para>
    /// </remarks>
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
