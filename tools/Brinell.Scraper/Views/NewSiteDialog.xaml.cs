using System.Windows;
using Brinell.Scraper.Data;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.Views;

public partial class NewSiteDialog : Window
{
    private readonly SiteInfo? _editingSite;

    public SiteInfo? Result { get; private set; }

    public NewSiteDialog()
    {
        InitializeComponent();
        Title = "New Site";
    }

    public NewSiteDialog(SiteInfo site) : this()
    {
        _editingSite = site;
        Title = "Edit Site";
        SubmitButton.Content = "Save";
        NameBox.Text = site.Name;
        UrlBox.Text = site.StartUrl;
        NamespaceBox.Text = site.Namespace;
        OutputPathBox.Text = site.OutputPath;
        AliasesBox.Text = string.Join(Environment.NewLine, site.UrlAliases);
    }

    private void OnBrowseOutputPath(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select output folder"
        };
        if (dialog.ShowDialog(this) == true)
            OutputPathBox.Text = dialog.FolderName;
    }

    private void OnCreateClick(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var url = UrlBox.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Site name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            NameBox.Focus();
            return;
        }

        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Start URL is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            UrlBox.Focus();
            return;
        }

        if (!url.Contains("://", StringComparison.Ordinal))
            url = "https://" + url;

        var aliases = AliasesBox.Text
            .Split('\n', '\r')
            .Select(a => a.Trim())
            .Where(a => !string.IsNullOrEmpty(a))
            .ToList();

        var db = App.Services.GetService(typeof(CorpusDatabase)) as CorpusDatabase;

        if (_editingSite is not null)
        {
            db!.UpdateSite(_editingSite.Id, name, url, NamespaceBox.Text.Trim(), OutputPathBox.Text.Trim(), aliases);
            Result = new SiteInfo
            {
                Id = _editingSite.Id,
                Name = name,
                StartUrl = url,
                Namespace = NamespaceBox.Text.Trim(),
                OutputPath = OutputPathBox.Text.Trim(),
                UrlAliases = aliases,
                CreatedAt = _editingSite.CreatedAt,
                LastOpenedAt = _editingSite.LastOpenedAt,
                PageCount = _editingSite.PageCount,
                ControlCount = _editingSite.ControlCount
            };
        }
        else
        {
            Result = db!.CreateSite(
                name,
                url,
                NamespaceBox.Text.Trim(),
                OutputPathBox.Text.Trim(),
                aliases);
        }

        DialogResult = true;
    }
}
