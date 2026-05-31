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
        var viewModel = new PresenterShellViewModel(
            new UatWorkspaceService(),
            new UatExecutionService(),
            new FolderPickerService(),
            new PresenterUserSettingsService());
        var window = new Window(new PresenterPage(viewModel))
        {
            Title = "Brinell Presenter"
        };
        window.Created += (_, _) => PlacePresenterWindow(window);
        return window;
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
}
