using System.Windows;
using Bruinet.Samples.Wpf.App.Features.Shell.ViewModels;

namespace Bruinet.Samples.Wpf.App.Features.Shell.Views;

/// <summary>
/// Interaction logic for ShellWindow.xaml
/// </summary>
public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel(App.NavigationService);
    }
}
