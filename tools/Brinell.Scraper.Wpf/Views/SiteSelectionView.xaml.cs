using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.ViewModels;

namespace Brinell.Scraper.Views;

public partial class SiteSelectionView : UserControl
{
    public SiteSelectionView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SiteSelectionViewModel vm)
        {
            vm.NewSiteRequested = ShowNewSiteDialog;
            vm.EditSiteRequested = ShowEditSiteDialog;
        }
    }

    private void ShowNewSiteDialog()
    {
        if (DataContext is not SiteSelectionViewModel vm) return;

        var dialog = new NewSiteDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.Result is not null)
        {
            vm.AddSite(dialog.Result);
        }
    }

    private void ShowEditSiteDialog(SiteInfo site)
    {
        if (DataContext is not SiteSelectionViewModel vm) return;

        var dialog = new NewSiteDialog(site) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.Result is not null)
        {
            vm.RefreshSite(dialog.Result);
        }
    }

    private void SiteList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SiteSelectionViewModel vm && SiteList.SelectedItem is SiteInfo site)
            vm.SelectSiteCommand.Execute(site);
    }
}
