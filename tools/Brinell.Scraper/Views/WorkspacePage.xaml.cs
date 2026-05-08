using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;

namespace Brinell.Scraper.Views;

public partial class WorkspacePage : UserControl
{
    public WorkspacePage()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Dispose any WebView2 hosted inside this page so its background
        // process is torn down before the page is dropped.
        var webView = FindWebView2(this);
        webView?.Dispose();
    }

    private static WebView2? FindWebView2(DependencyObject root)
    {
        if (root is WebView2 wv) return wv;

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var found = FindWebView2(child);
            if (found is not null) return found;
        }
        return null;
    }
}
