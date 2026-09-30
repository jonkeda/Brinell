using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Controls;
using Brinell.Scraper.ViewModels;

namespace Brinell.Scraper.Views.Tabs;

public partial class LogTabView : UserControl
{
    private LogViewerViewModel? _vm;

    public LogTabView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.RawEntries.CollectionChanged -= OnEntriesChanged;

        _vm = e.NewValue as LogViewerViewModel;

        if (_vm is not null)
            _vm.RawEntries.CollectionChanged += OnEntriesChanged;
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.RawEntries.CollectionChanged -= OnEntriesChanged;
            _vm = null;
        }
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add) return;
        if (_vm is null || !_vm.IsAutoScroll) return;
        if (LogGrid.Items.Count == 0) return;

        Dispatcher.BeginInvoke(new System.Action(() =>
        {
            var last = LogGrid.Items[LogGrid.Items.Count - 1];
            if (last is not null)
                LogGrid.ScrollIntoView(last);
        }));
    }
}
