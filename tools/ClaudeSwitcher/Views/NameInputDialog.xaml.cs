using System.Windows;

namespace ClaudeSwitcher.Views;

public partial class NameInputDialog : Window
{
    public NameInputDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NameBox.Focus();
    }

    public string ProfileName => NameBox.Text.Trim();

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
            return;
        DialogResult = true;
    }

    public static string? Prompt(Window? owner)
    {
        var dlg = new NameInputDialog { Owner = owner };
        return dlg.ShowDialog() == true ? dlg.ProfileName : null;
    }
}
