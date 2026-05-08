using System.Windows;
using Brinell.Scraper.Models;
using Brinell.Scraper.ViewModels;
using Brinell.Scraper.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Scraper;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;

    private StartPageViewModel? _startVm;
    private WorkspaceViewModel? _workspaceVm;
    private Action? _workspaceBackHandler;

    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        Loaded += (_, _) => ShowStartPage();
        Closed += (_, _) => DisposeWorkspace();
    }

    private void ShowStartPage()
    {
        DisposeWorkspace();
        DisposeStart();

        var vm = _services.GetRequiredService<StartPageViewModel>();
        vm.SiteSelected += OnSiteSelected;
        vm.SiteOpenWithUrlRequested += OnSiteOpenWithUrl;
        vm.SettingsRequested += OnSettingsRequested;
        _startVm = vm;

        _ = vm.LoadAsync();

        RootContent.Content = new StartPage { DataContext = vm };
    }

    private void OnSiteSelected(SiteCardItem card)
    {
        DisposeStart();

        var vm = _services.GetRequiredService<WorkspaceViewModel>();
        _workspaceVm = vm;
        _workspaceBackHandler = ShowStartPage;
        vm.BackRequested += _workspaceBackHandler;

        _ = vm.LoadAsync(card.Id);

        RootContent.Content = new WorkspacePage { DataContext = vm };
    }

    private void OnSiteOpenWithUrl(long siteId, string url)
    {
        DisposeStart();

        var vm = _services.GetRequiredService<WorkspaceViewModel>();
        _workspaceVm = vm;
        _workspaceBackHandler = ShowStartPage;
        vm.BackRequested += _workspaceBackHandler;

        _ = vm.LoadAsync(siteId, navigateUrl: url);

        RootContent.Content = new WorkspacePage { DataContext = vm };
    }

    private void OnSettingsRequested()
    {
        DisposeStart();

        var vm = _services.GetRequiredService<WorkspaceViewModel>();
        _workspaceVm = vm;
        _workspaceBackHandler = ShowStartPage;
        vm.BackRequested += _workspaceBackHandler;

        vm.LoadStandaloneSettings();
        vm.SelectedTabIndex = 5;

        RootContent.Content = new WorkspacePage { DataContext = vm };
    }

    private void DisposeStart()
    {
        if (_startVm is null) return;

        _startVm.SiteSelected -= OnSiteSelected;
        _startVm.SiteOpenWithUrlRequested -= OnSiteOpenWithUrl;
        _startVm.SettingsRequested -= OnSettingsRequested;
        _startVm = null;
    }

    private void DisposeWorkspace()
    {
        if (_workspaceVm is null) return;

        if (_workspaceBackHandler is not null)
            _workspaceVm.BackRequested -= _workspaceBackHandler;
        _workspaceBackHandler = null;

        // Detach UI before disposing so WebView2 hosted in the previous page
        // is torn down via WorkspacePage.Unloaded.
        if (RootContent.Content is WorkspacePage page)
        {
            page.DataContext = null;
            RootContent.Content = null;
        }

        _workspaceVm.Dispose();
        _workspaceVm = null;
    }
}
