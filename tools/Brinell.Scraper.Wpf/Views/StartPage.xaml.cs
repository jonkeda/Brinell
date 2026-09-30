using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Brinell.Scraper.Models;
using Brinell.Scraper.ViewModels;

namespace Brinell.Scraper.Views;

public partial class StartPage : UserControl
{
    public StartPage()
    {
        InitializeComponent();
        VersionLabel.Text = $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"}";
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StartPageViewModel vm) return;

        vm.NewSiteRequested += ShowNewSiteDialog;
        vm.EditSiteRequested += ShowEditSiteDialog;
        vm.DeleteSiteConfirmRequested += ConfirmDelete;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StartPageViewModel vm) return;

        vm.NewSiteRequested -= ShowNewSiteDialog;
        vm.EditSiteRequested -= ShowEditSiteDialog;
        vm.DeleteSiteConfirmRequested -= ConfirmDelete;
    }

    private void ShowNewSiteDialog()
    {
        if (DataContext is not StartPageViewModel vm) return;

        var dialog = new NewSiteDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.Result is { } site)
        {
            vm.AddOrUpdateSite(site);
            var card = vm.Sites.FirstOrDefault(c => c.Id == site.Id);
            if (card is not null)
                vm.RaiseSiteSelected(card);
        }
    }

    private void ShowEditSiteDialog(SiteCardItem card)
    {
        if (DataContext is not StartPageViewModel vm) return;

        var siteInfo = new SiteInfo
        {
            Id = card.Id,
            Name = card.Name,
            StartUrl = card.StartUrl,
            PageCount = card.PageCount,
            ControlCount = card.ControlCount,
            LastOpenedAt = card.LastOpenedAt ?? default
        };

        var dialog = new NewSiteDialog(siteInfo) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.Result is { } updated)
        {
            vm.AddOrUpdateSite(updated);
        }
    }

    private bool ConfirmDelete(SiteCardItem card)
    {
        var result = MessageBox.Show(
            Window.GetWindow(this),
            $"Delete site '{card.Name}' and all its captured pages?\n\nThis cannot be undone.",
            "Delete site",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        return result == MessageBoxResult.Yes;
    }
}
