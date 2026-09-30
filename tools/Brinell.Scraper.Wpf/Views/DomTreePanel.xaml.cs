using System.Windows.Controls;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.ViewModels;

namespace Brinell.Scraper.Views;

public partial class DomTreePanel : UserControl
{
    public DomTreePanel()
    {
        InitializeComponent();
    }

    public void Initialize(InspectorViewModel vm)
    {
        DataContext = vm;
    }

    private void TreeItem_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is TreeViewItem { DataContext: DomElement element })
        {
            if (DataContext is DomTreeViewModel vm)
                vm.OnElementHover(element);
            e.Handled = true;
        }
    }

    private void TreeItem_MouseLeave(object sender, MouseEventArgs e)
    {
        if (DataContext is DomTreeViewModel vm)
            vm.OnElementUnhover();
    }

    private void TreeView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeView { SelectedItem: DomElement element })
        {
            if (DataContext is DomTreeViewModel vm)
                vm.OnElementClick(element);
        }
    }
}
