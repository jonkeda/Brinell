using System.Windows.Controls;

namespace Brinell.Scraper.Views;

public partial class DomTreePanel : UserControl
{
    public DomTreePanel()
    {
        InitializeComponent();
    }

    public void Initialize(ViewModels.InspectorViewModel vm)
    {
        DataContext = vm;
    }
}
