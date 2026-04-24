using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Brinell.Scraper.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;

namespace Brinell.Scraper.Views;

public partial class BrowserView : UserControl
{
    private BrowserViewModel? _vm;
    private bool _initialized;

    public BrowserView()
    {
        InitializeComponent();
    }

    public async void Initialize(BrowserViewModel vm)
    {
        _vm = vm;

        if (!_initialized)
        {
            _initialized = true;

            // Step 1.5: Cookie / session persistence — custom user data folder
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Brinell.Scraper", "WebView2Data");
            Directory.CreateDirectory(userDataFolder);

            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder);

            await WebView.EnsureCoreWebView2Async(environment);

            // Step 1.4: Navigation events
            WebView.CoreWebView2.NavigationStarting += OnNavigationStarting;
            WebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            WebView.CoreWebView2.SourceChanged += OnSourceChanged;
            WebView.CoreWebView2.HistoryChanged += OnHistoryChanged;
            WebView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
            WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            // Enable iframe frame tracking for highlight service
            var highlight = App.Services.GetRequiredService<ElementHighlightService>();
            highlight.TrackFrames(WebView.CoreWebView2);
        }

        // Wire ViewModel → WebView2 actions
        _vm.NavigateRequested += OnNavigateRequested;
        _vm.GoBackRequested += () => WebView.CoreWebView2?.GoBack();
        _vm.GoForwardRequested += () => WebView.CoreWebView2?.GoForward();
        _vm.RefreshRequested += () => WebView.CoreWebView2?.Reload();
        _vm.OpenDevToolsRequested += () => WebView.CoreWebView2?.OpenDevToolsWindow();

        // Expose CoreWebView2 to the ViewModel layer
        _vm.GetCoreWebView2 = () => WebView.CoreWebView2;
    }

    private void OnNavigateRequested(string url)
    {
        if (WebView.CoreWebView2 is null) return;

        // Ensure URL has a scheme
        if (!url.Contains("://", StringComparison.Ordinal))
            url = "https://" + url;

        try
        {
            WebView.CoreWebView2.Navigate(url);
        }
        catch (ArgumentException)
        {
            _vm?.OnNavigationCompleted(false, "Invalid URL");
        }
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        _vm?.OnNavigationStarting(e.Uri);
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _vm?.OnNavigationCompleted(e.IsSuccess, e.IsSuccess ? null : e.WebErrorStatus.ToString());
    }

    private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        _vm?.OnSourceChanged(WebView.CoreWebView2.Source);
    }

    private void OnHistoryChanged(object? sender, object e)
    {
        _vm?.OnHistoryChanged(WebView.CoreWebView2.CanGoBack, WebView.CoreWebView2.CanGoForward);
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        // Navigate the current WebView to the requested URL instead of opening a new window
        e.Handled = true;
        WebView.CoreWebView2.Navigate(e.Uri);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.WebMessageAsJson;
            var msg = JsonSerializer.Deserialize<WebViewMessage>(json);
            if (msg?.Type == "elementSelected")
                _vm?.OnElementSelected(msg);
        }
        catch
        {
            // Ignore malformed messages
        }
    }
}
