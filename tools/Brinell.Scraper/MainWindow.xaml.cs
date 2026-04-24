using System.Windows;
using System.Windows.Input;
using Brinell.Scraper.ViewModels;
using Brinell.Scraper.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Scraper;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private BrowserView? _browserView;
    private SiteSelectionView? _siteSelectionView;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        _vm.SiteSelectorRequested += ShowSiteSelector;
        _vm.BrowserViewRequested += ShowBrowserView;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsLogViewerVisible))
                LogViewerRow.Height = _vm.IsLogViewerVisible
                    ? new GridLength(180)
                    : new GridLength(0);
        };

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var logViewerVm = App.Services.GetRequiredService<LogViewerViewModel>();
        LogViewerPanel.Initialize(logViewerVm);

        ShowSiteSelector();
    }

    private void ShowSiteSelector()
    {
        _siteSelectionView ??= new SiteSelectionView { DataContext = _vm.SiteSelection };
        ContentArea.Content = _siteSelectionView;
    }

    private void ShowBrowserView()
    {
        if (_browserView is null)
        {
            _browserView = new BrowserView();
            _browserView.Initialize(_vm.Browser);
        }
        ContentArea.Content = _browserView;

        // Trigger initial navigation if address is set
        if (!string.IsNullOrWhiteSpace(_vm.Browser.AddressUrl))
            _vm.Browser.NavigateCommand.Execute(null);
    }

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _vm.Browser.NavigateCommand.CanExecute(null))
        {
            _vm.Browser.NavigateCommand.Execute(null);
            e.Handled = true;
        }
    }
}
