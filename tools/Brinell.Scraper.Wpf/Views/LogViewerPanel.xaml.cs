using System.Collections.Specialized;
using System.Windows.Controls;
using Brinell.Scraper.ViewModels;

namespace Brinell.Scraper.Views;

public partial class LogViewerPanel : UserControl
{
    public LogViewerPanel()
    {
        InitializeComponent();
    }

    public void Initialize(LogViewerViewModel vm)
    {
        DataContext = vm;
        // Auto-scroll on new entries
        vm.RawEntries.CollectionChanged += OnEntriesChanged;
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is LogViewerViewModel { IsAutoScroll: true } && LogList.Items.Count > 0)
        {
            LogList.ScrollIntoView(LogList.Items[^1]);
        }
    }
}
