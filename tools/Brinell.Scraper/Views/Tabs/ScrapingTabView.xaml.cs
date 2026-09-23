using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Brinell.Scraper.ViewModels.Tabs;

namespace Brinell.Scraper.Views.Tabs;

public partial class ScrapingTabView : UserControl
{
    private bool _browserInitialized;

    public ScrapingTabView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => TryInitializeBrowser();

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => TryInitializeBrowser();

    private void TryInitializeBrowser()
    {
        if (_browserInitialized) return;
        if (DataContext is not ScrapingTabViewModel vm) return;
        if (!IsLoaded) return;

        BrowserHost.Initialize(vm.Browser);
        _browserInitialized = true;
    }

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ScrapingTabViewModel vm) return;
        if (e.Key == Key.Enter && vm.Browser.NavigateCommand.CanExecute(null))
        {
            vm.Browser.NavigateCommand.Execute(null);
            e.Handled = true;
        }
    }
}

