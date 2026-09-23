using System.Windows;
using System.Windows.Controls;
using Brinell.Samples.Wpf.App.Features.Login.ViewModels;

namespace Brinell.Samples.Wpf.App.Features.Login.Views;

/// <summary>
/// Interaction logic for LoginPage.xaml
/// </summary>
public partial class LoginPage : UserControl
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }
}
